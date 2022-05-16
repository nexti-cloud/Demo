using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.DTOs.User;
using AnglickaVyzva.API.Entities;
using AnglickaVyzva.API.Helpers;
using AnglickaVyzva.API.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AnglickaVyzva.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : BaseController
    {
        static SemaphoreSlim semaphore_giftOrActivationCode = new SemaphoreSlim(1, 1);

        public UserController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {

        }

        //[HttpPost("fakeBuyMonth")]
        //public async Task<IActionResult> FakeBuyMonth()
        //{
        //    var user = await GetLoggedUser();

        //    if(user.PrepaidUntil > DateTime.Now)
        //    {
        //        throw new Exception("Už má koupeno");
        //    }

        //    user.PrepaidUntil = DateTime.Now.Date.AddMonths(1).AddMilliseconds(-1);
        //    await SaveAll();

        //    return Ok();
        //}

        public class ChangeUnconfirmedLoginEmail_Model { public string NewLoginEmail { get; set; } }
        [HttpPost("changeUnconfirmedLoginEmail")]
        public async Task<IActionResult> ChangeUnconfirmedLoginEmail(ChangeUnconfirmedLoginEmail_Model model)
        {
            var user = await GetLoggedUser();

            if(user.LoginMethod != Entities.User.LoginMethods.Email)
            {
                return BadRequest("Změnit email lze pouze u přihlášení pomocí emailu");
            }

            if(user.IsUserNameVerified)
            {
                return BadRequest("U účtu s ověřeným přihlašovacím emailem již nelze email změnit");
            }

            var email = model.NewLoginEmail.Trim().ToLower();

            var checkUser = await UserRepo.All.FirstOrDefaultAsync(x => x.UserName == email || x.GoogleEmail == email || x.FacebookEmail == email || x.AppleEmail == email);

            // Nekdo uz nejakym zpusobem tento email pouziva
            if (checkUser != null)
            {
                if (checkUser.LoginMethod == AuthenticationHelper.LoginMethods.Email)
                {
                    LogRepo.Add(new Log(Log.Actions.RegisterByEmail, Log.DescriptionsPredefined.EmailAlreadyInUse, $"UserId: {user.Id}, newLoginEmail: {email}"));
                    await LogRepo.SaveAll();

                    return BadRequest("Tento email je již obsazený");
                }
            }

            user.UserName = email;
            user.Email = email;

            await SaveAll();

            return Ok();
        }

        public class UseGiftCode_Or_ActivationCode_Model { public string Code { get; set; } }
        [HttpPost("useGiftCodeOrActivationCode")]
        public async Task<IActionResult> UseGiftCode_Or_ActivationCode(UseGiftCode_Or_ActivationCode_Model model)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                return BadRequest("Aktivační kód není zadaný");
            }

            var code = model.Code.ToUpper();

            try
            {
                await semaphore_giftOrActivationCode.WaitAsync();

                var user = await GetLoggedUser();
                if (user.PrepaidUntil > DateTime.Now)
                {
                    return BadRequest("Máte stále aktivní Premium předplatné. Nové předplatné lza aktivovat až po vypršení současného.");
                }

                var order = await OrderRepo.All.FirstOrDefaultAsync(x => x.GiftCode == code);
                var activationCode = await ActivationCodeRepo.All.FirstOrDefaultAsync(x => x.Code == code);


                if (order == null && activationCode == null)
                {
                    return BadRequest("Neznámý aktivační kód.");
                }

                if (order?.GiftActivatedDate != null || (activationCode != null && activationCode.IsActivated))
                {
                    return BadRequest("Aktivační kód je již použitý");
                }


                user.IsPremium = true;
                user.DoNotRenewSubscription = false;

                // Koupeny darkovy poukaz
                if (order != null)
                {
                    user.PrepaidUntil = DateTime.Now.Date.AddYears(1).AddMilliseconds(-1);

                    user.SubscriptionType = "gift";
                    user.ActiveOrderId = order.Id;

                    order.GiftActivatedDate = DateTime.Now;
                    order.GiftActivatedByEmail = user.GetLoginEmail();
                }
                else // Aktivacni kod (treba ho nekde vyhral)
                {
                    if(activationCode.ExpirationDate < DateTime.Now)
                    {
                        return BadRequest("Platnost aktivačního kódu vypršela");
                    }

                    if(activationCode.IsForMonth)
                    {
                        user.PrepaidUntil = DateTime.Now.Date.AddMonths(1).AddMilliseconds(-1);
                        user.SubscriptionType = "month";
                    }
                    else
                    {
                        user.PrepaidUntil = DateTime.Now.Date.AddYears(1).AddMilliseconds(-1);
                        user.SubscriptionType = "year";
                    }

                    user.ActiveOrderId = null;

                    activationCode.IsActivated = true;
                    activationCode.ActivationDate = DateTime.Now;
                    activationCode.ActivatedForUserName = user.GetLoginEmail();
                }

                using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                {                   
                    await SaveAll();

                    // Ukoncim starou trial vyzvu a vytvorim novou
                    await OrderController.EndOrDeleteTrialPersonalChallenge_And_CreateNewPaidPersonalChallenge_Shared(user.Id, PersonalChallengeRepo, UserRepo);

                    await SaveAll(); // Tohle je mozna zbytecne

                    await transaction.CommitAsync();
                }

                return Ok();
            }
            finally
            {
                semaphore_giftOrActivationCode.Release();
            }

        }

        [HttpGet("checkUserCompleteRegistration")]
        public async Task<IActionResult> CheckUserCompleteRegistration()
        {
            var user = await GetLoggedUser();

            return Ok(new
            {
                isCompletelyRegistered = user.IsCompletelyRegistered(),
                completelyRegisteredErrorMessage = user.GetCompletelyRegisteredError()
            });
        }


        [HttpGet("getUserAccountDetail")]
        public async Task<IActionResult> GetUserAccountDetail()
        {
            var user = await UserRepo.All.FirstAsync(x => x.Id == UserId);

            var userDto = _mapper.Map<User_ForAccountDetailDto>(user);

            return Ok(new
            {
                user = userDto
            });
        }

        public class SaveUserAccountDetail_Model
        {
            public string Firstname { get; set; }
            public string Lastname { get; set; }
        }
        [HttpPost("saveUserAccountDetail")]
        public async Task<IActionResult> SaveUserAccountDetail(SaveUserAccountDetail_Model model)
        {
            if (string.IsNullOrWhiteSpace(model.Firstname))
            {
                throw new Exception("Jméno nesmí být prázdné");
            }

            if (string.IsNullOrWhiteSpace(model.Lastname))
            {
                throw new Exception("Příjmení nesmí být prázdné");
            }

            var userFromDb = await UserRepo.All.FirstAsync(x => x.Id == UserId);
            userFromDb.Firstname = model.Firstname;
            userFromDb.Lastname = model.Lastname;

            //// Telefon se lisi -> uz neni overeny
            //if (userFromDb.Phone != userDto.Phone)
            //{
            //    userFromDb.IsPhoneVerified = false;
            //}
            //userFromDb.Phone = userDto.Phone;

            //userFromDb.Email = userDto.Email;

            await UserRepo.SaveAll();

            return Ok();
        }

        // Overeni prihlasovaciho emailu
        [HttpPost("sendUserNameVerificationCode")]
        public async Task<IActionResult> SendUserNameVerificationCode()
        {
            var user = await UserRepo.All.FirstAsync(x => x.Id == UserId);
            if (user.LoginMethod != AuthenticationHelper.LoginMethods.Email)
            {
                LogRepo.Add(new Log(Log.Actions.VerificationCodeSend_UserName, Log.DescriptionsPredefined.LoginBadMethod));
                await LogRepo.SaveAll();

                throw new Exception("Uživatel používá jiný typ přihlášení");
            }

            if (user.IsUserNameVerified)
            {
                LogRepo.Add(new Log(Log.Actions.VerificationCodeSend_UserName, Log.DescriptionsPredefined.AlreadyVerified));
                await LogRepo.SaveAll();

                throw new Exception("Přihlašovací email je již ověřen");
            }

            var code = AuthenticationHelper.RandomNumericString(AuthenticationHelper.codeLength);

            

            

            try
            {
                await SharedSemaphores.Semaphore_UserAccount_UserName.WaitAsync();

                // Zapisu si informace do staticke promenne, abych je mohl pri dalsim dotazu overit (ano, pri restartu serveru jsou zapomenuty)
                if (!AuthenticationHelper.UserNameVerificationCodesDictionary.ContainsKey(UserId))
                {
                    AuthenticationHelper.UserNameVerificationCodesDictionary.Add(UserId, null);
                }
                else
                {
                    var tuple = AuthenticationHelper.UserNameVerificationCodesDictionary[UserId];
                    if (tuple != null)
                    {
                        var seconds = 10;
                        if (tuple.Created.AddSeconds(seconds) > DateTime.Now)
                        {
                            LogRepo.Add(new Log(Log.Actions.VerificationCodeSend_UserName, Log.DescriptionsPredefined.TooEarly));
                            await LogRepo.SaveAll();
                            throw new Exception($"Nový ověřovací kód lze znovu odeslat až po {seconds} sekundách.");
                        }
                    }
                }
                var emailHtml = EmailHelper.GenerateEmail(
                        "Ověření přihlašovacího e-mailu",
                        $"Váš kód pro ověření přihlašovacího e-mailu pro Anglickou výzvu! je:" +
                        $"<br />" +
                        $"<b>{code}</b>" +
                        $"<br />" +
                        $"<br />" +
                        $"Tento kód zadejte v uživatelském profilu do kolonky ověřit kód."
                        );

                EmailRepo.Add(new Email(user.UserName, "Ověření přihlašovacího emailu", emailHtml));

                AuthenticationHelper.UserNameVerificationCodesDictionary[UserId] = new AuthenticationHelper.TupleUserName(code, UserId);

                LogRepo.Add(new Log(Log.Actions.VerificationCodeSend_UserName, Log.DescriptionsPredefined.Success));
                await LogRepo.SaveAll();
            }
            catch(Exception exc)
            {
                throw exc;
            }
            finally
            {
                SharedSemaphores.Semaphore_UserAccount_UserName.Release();
            }

            

            return Ok();
        }

        // Overeni kontaktniho emailu - ne prihlasovaciho
        public class SendEmailVerificationCode_Model
        {
            public string Email { get; set; }
        }
        [HttpPost("sendEmailVerificationCode")]
        public async Task<IActionResult> SendEmailVerificationCode(SendEmailVerificationCode_Model model)
        {
            if (!AuthenticationHelper.IsEmailValid(model.Email))
            {
                LogRepo.Add(new Log(Log.Actions.VerificationCodeSend_Email, Log.DescriptionsPredefined.EmailBadFormat, model.Email));
                await LogRepo.SaveAll();
                throw new Exception("Email není ve správném formátu");
            }

            var user = await UserRepo.All.FirstAsync(x => x.Id == UserId);

            if (user.Email == model.Email && user.IsEmailVerified)
            {
                LogRepo.Add(new Log(Log.Actions.VerificationCodeSend_Email, Log.DescriptionsPredefined.AlreadyVerified, model.Email));
                await LogRepo.SaveAll();
                throw new Exception("Tento kontaktní email je již ověřen");
            }

            var code = AuthenticationHelper.RandomNumericString(AuthenticationHelper.codeLength);



            

            try
            {
                await SharedSemaphores.Semaphore_UserAccount_Email.WaitAsync();

                // Zapisu si informace do staticke promenne, abych je mohl pri dalsim dotazu overit (ano, pri restartu serveru jsou zapomenuty)
                if (!AuthenticationHelper.EmailVerificationCodesDictionary.ContainsKey(UserId))
                {
                    AuthenticationHelper.EmailVerificationCodesDictionary.Add(UserId, null);
                }
                else
                {
                    var tuple = AuthenticationHelper.EmailVerificationCodesDictionary[UserId];
                    if (tuple != null)
                    {
                        var seconds = 120;
                        if (tuple.Created.AddSeconds(seconds) > DateTime.Now)
                        {
                            LogRepo.Add(new Log(Log.Actions.VerificationCodeSend_Email, Log.DescriptionsPredefined.TooEarly));
                            await LogRepo.SaveAll();
                            throw new Exception($"Ověřovací kód lze znovu odeslat až po {seconds} sekundách.");
                        }
                    }
                }
                var emailHtml = EmailHelper.GenerateEmail("Ověření kontaktního emailu", $"" +
                $"Váš kód pro ověření kontaktního emailu pro Anglickou výzvu! je:" +
                $"<br />" +
                $"<b>{code}</b></h3>");

                EmailRepo.Add(new Email(model.Email, "Ověření kontaktního emailu", emailHtml));

                AuthenticationHelper.EmailVerificationCodesDictionary[UserId] = new AuthenticationHelper.TupleEmail(code, UserId, model.Email);

                LogRepo.Add(new Log(Log.Actions.VerificationCodeSend_Email, Log.DescriptionsPredefined.Success, model.Email));
                await LogRepo.SaveAll();
            }
            catch (Exception exc)
            {
                throw exc;
            }
            finally
            {
                SharedSemaphores.Semaphore_UserAccount_Email.Release();
            }

            

            

            return Ok();
        }

        public class CheckUserNameVerificationCode_Model
        {
            public string Code { get; set; }
        }
        [HttpPost("checkUserNameVerificationCode")]
        public async Task<IActionResult> CheckUserNameVerificationCode(CheckUserNameVerificationCode_Model model)
        {
            var user = await UserRepo.All.FirstAsync(x => x.Id == UserId);

            if (user.LoginMethod != AuthenticationHelper.LoginMethods.Email)
            {
                LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_UserName, Log.DescriptionsPredefined.LoginBadMethod));
                await LogRepo.SaveAll();
                throw new Exception("Uživatel používá jiný typ přihlášení");
            }

            if (user.IsUserNameVerified)
            {
                LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_UserName, Log.DescriptionsPredefined.AlreadyVerified));
                await LogRepo.SaveAll();
                throw new Exception("Přihlašovací email je již ověřen");
            }

            await SharedSemaphores.Semaphore_UserAccount_UserName.WaitAsync();
            try
            {
                AuthenticationHelper.TuplesGarbageCollector(AuthenticationHelper.UserNameVerificationCodesDictionary);
                if (!AuthenticationHelper.UserNameVerificationCodesDictionary.ContainsKey(UserId))
                {
                    LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_UserName, Log.DescriptionsPredefined.VerificationCodeNotFound));
                    await LogRepo.SaveAll();
                    throw new Exception("Špatný ověřovací kód");
                }

                var tuple = AuthenticationHelper.UserNameVerificationCodesDictionary[UserId];

                var res = AuthenticationHelper.CheckBaseTuple(tuple, model.Code);
                if (!res.IsCorrect)
                {
                    LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_UserName, res.LogDescriptionError));
                    await LogRepo.SaveAll();
                    throw new Exception(res.ErrorMessage);
                }

                // Pta se jiny uzivatel, nez pro koho byl kod vygenerovan
                if (tuple.UserId != UserId)
                {
                    LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_UserName, Log.DescriptionsPredefined.UserNotMatch));
                    await LogRepo.SaveAll();
                    throw new Exception("Špatný ověřovací kód");
                }

                // Vsechno probehlo v poradku, mazu pozadavek z dictionary
                AuthenticationHelper.UserNameVerificationCodesDictionary.Remove(UserId);
            }
            finally
            {
                SharedSemaphores.Semaphore_UserAccount_UserName.Release();
            }

            user.IsUserNameVerified = true;

            // Pokud je kontaktni email stejny jako prihlasovaci, je automaticky taky overeny
            if (user.UserName == user.Email)
            {
                user.IsEmailVerified = true;
            }

            LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_UserName, Log.DescriptionsPredefined.Success));

            await UserRepo.SaveAll();

            return Ok();
        }

        public class CheckEmailVerificationCode_Model
        {
            public string Email { get; set; }
            public string Code { get; set; }
        }
        [HttpPost("checkEmailVerificationCode")]
        public async Task<IActionResult> CheckEmailVerificationCode(CheckEmailVerificationCode_Model model)
        {
            if (!AuthenticationHelper.IsEmailValid(model.Email))
            {
                LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_Email, Log.DescriptionsPredefined.EmailBadFormat, model.Email));
                await LogRepo.SaveAll();
                throw new Exception("Email není ve správném formátu");
            }

            var user = await UserRepo.All.FirstAsync(x => x.Id == UserId);

            if (user.Email == model.Email && user.IsEmailVerified)
            {
                LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_Email, Log.DescriptionsPredefined.AlreadyVerified, model.Email));
                await LogRepo.SaveAll();
                throw new Exception("Přihlašovací email je již ověřen");
            }

            await SharedSemaphores.Semaphore_UserAccount_Email.WaitAsync();
            try
            {
                AuthenticationHelper.TuplesGarbageCollector(AuthenticationHelper.EmailVerificationCodesDictionary);

                if (!AuthenticationHelper.EmailVerificationCodesDictionary.ContainsKey(UserId))
                {
                    LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_Email, Log.DescriptionsPredefined.VerificationCodeNotFound, model.Email));
                    await LogRepo.SaveAll();
                    throw new Exception("Špatný ověřovací kód");
                }

                var tuple = AuthenticationHelper.EmailVerificationCodesDictionary[UserId];

                var res = AuthenticationHelper.CheckBaseTuple(tuple, model.Code);
                if (!res.IsCorrect)
                {
                    LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_Email, res.LogDescriptionError, model.Email));
                    await LogRepo.SaveAll();
                    throw new Exception(res.ErrorMessage);
                }

                // Spatny email
                if (tuple.Email != model.Email)
                {
                    LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_Email, Log.DescriptionsPredefined.EmailNotFound, model.Email));
                    await LogRepo.SaveAll();
                    throw new Exception("Špatný ověřovací kód");
                }

                // Pta se jiny uzivatel, nez pro koho byl kod vygenerovan
                if (tuple.UserId != UserId)
                {
                    LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_Email, Log.DescriptionsPredefined.UserNotMatch, model.Email));
                    await LogRepo.SaveAll();
                    throw new Exception("Špatný ověřovací kód");
                }

                // Vsechno probehlo v poradku, mazu pozadavek z dictionary
                AuthenticationHelper.EmailVerificationCodesDictionary.Remove(UserId);
            }
            finally
            {
                SharedSemaphores.Semaphore_UserAccount_Email.Release();
            }

            user.Email = model.Email;
            user.IsEmailVerified = true;


            LogRepo.Add(new Log(Log.Actions.VerificationCodeCheck_Email, Log.DescriptionsPredefined.Success, model.Email));

            await UserRepo.SaveAll();

            return Ok();
        }


        [HttpPost("cancelSubscription")]
        public async Task<IActionResult> CancelSubscription()
        {
            var user = await GetLoggedUser();

            if (user.IsPremium == false)
            {
                return BadRequest("Uživatel nemá aktivovaný Premium účet");
            }

            if (user.DoNotRenewSubscription == true)
            {
                return BadRequest("Uživatel má již obnovu předplatného zrušenou");
            }

            EmailHelper.SendLogEmail(ServerEmailCredentials, "Zrušení měsíčního předplatného", $"Uživatel: {user.GetLoginEmail()}");

            user.DoNotRenewSubscription = true;
            await SaveAll();

            return Ok();
        }

        /// <summary>
        /// Znovu obnovy predplatne
        /// </summary>
        /// <returns></returns>
        [HttpPost("enableSubscription")]
        public async Task<IActionResult> EnableSubscription()
        {
            var user = await GetLoggedUser();

            if (user.IsPremium == false)
            {
                return BadRequest("Uživatel nemá aktivovaný Premium účet");
            }

            if (user.DoNotRenewSubscription == false)
            {
                return BadRequest("Uživatel nemá zrušené předplatné");
            }

            EmailHelper.SendLogEmail(ServerEmailCredentials, "Obnovení měsíčního předplatného", $"Uživatel: {user.GetLoginEmail()}");

            user.DoNotRenewSubscription = false;
            await SaveAll();

            return Ok();
        }

        public static async Task MergeProgressIntoAnotherUser(int fromThisUserId, int intoThisUserId, EFProgressRepo progressRepo, EFTestProgressRepo testProgressRepo, EFLogRepo logRepo)
        {
            if(fromThisUserId == intoThisUserId)
            {
                throw new Exception($"Id uživatelů jsou stejná [{fromThisUserId},{intoThisUserId}]");
            }

            if (fromThisUserId == 0)
            {
                throw new Exception("Id fromThisUserId je 0");
            }

            if (intoThisUserId == 0)
            {
                throw new Exception("Id intoThisUserId je 0");
            }

            var allFromUserProgresses = await progressRepo.All.Where(x => x.UserId == fromThisUserId).ToListAsync();
            var allIntoUserProgresses = await progressRepo.All.Where(x => x.UserId == intoThisUserId).ToListAsync();

            var allFromUserTestProgressesPassed = await testProgressRepo.All.Where(x => x.UserId == fromThisUserId && x.Percentage >= Test.PercentageThreshold).ToListAsync();
            var allIntoUserTestProgressesPassed = await testProgressRepo.All.Where(x => x.UserId == intoThisUserId && x.Percentage >= Test.PercentageThreshold).ToListAsync();

            logRepo.Add(new Log(Log.Actions.LoginByEmail, Log.DescriptionsPredefined.MergeProgress, $"fromUserId: {fromThisUserId}, toUserId: {intoThisUserId}"));

            foreach (var fromProgress in allFromUserProgresses)
            {
                // Nastavajici uzivatel tento postup nema -> vytvorim ho
                if(!allIntoUserProgresses.Any(x=>
                    x.LessonOrder == fromProgress.LessonOrder &&
                    x.SectionOrder == fromProgress.SectionOrder &&
                    x.ExerciseOrder == fromProgress.ExerciseOrder &&
                    x.ExerciseSuborder == fromProgress.ExerciseSuborder
                ))
                {
                    progressRepo.Add(new Progress
                    {
                        UserId = intoThisUserId,
                        Created = fromProgress.Created,
                        LessonOrder = fromProgress.LessonOrder,
                        SectionOrder = fromProgress.SectionOrder,
                        ExerciseOrder = fromProgress.ExerciseOrder,
                        ExerciseSuborder = fromProgress.ExerciseSuborder,
                        Points = fromProgress.Points,
                    });
                }
            }

            foreach(var fromTest in allFromUserTestProgressesPassed)
            {
                // Nastavajici uzivatel tento test nema splneny -> vytvorim ho
                if(!allIntoUserTestProgressesPassed.Any(x=>
                    x.LessonOrder == fromTest.LessonOrder &&
                    x.TestOrder ==fromTest.TestOrder 
                ))
                {
                    testProgressRepo.Add(new TestProgress
                    {
                        UserId = intoThisUserId,
                        Created = fromTest.Created,
                        LessonOrder = fromTest.LessonOrder,
                        TestOrder = fromTest.TestOrder,
                        Percentage = fromTest.Percentage,
                        Points = fromTest.Points,
                    });
                }
            }

            await progressRepo.SaveAll();
        }
    }
}
