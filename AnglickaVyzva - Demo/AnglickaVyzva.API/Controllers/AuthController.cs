using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.DTOs;
using AnglickaVyzva.API.DTOs.Auth;
using AnglickaVyzva.API.Entities;
using AnglickaVyzva.API.Helpers;
using AutoMapper;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RestSharp;

namespace AnglickaVyzva.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseController
    {
        AuthenticationHelper.AuthenticationSettings authenticationSettings = new AuthenticationHelper.AuthenticationSettings(
            "https://anglicka-vyzva-api.azurewebsites.net/api/auth/redirectedByGoogle",
            //"https://localhost:44309/api/auth/redirectedByGoogle",
            "https://anglicka-vyzva-api.azurewebsites.net/api/auth/redirectedByFacebook",
            "https://anglicka-vyzva-api.azurewebsites.net/api/auth/redirectedByApple"
            );

        AuthenticationHelper.AuthenticationSettings authenticationSettings_DEBUG = new AuthenticationHelper.AuthenticationSettings(
            "https://anglicka-vyzva-api-dev.azurewebsites.net/api/auth/redirectedByGoogle",
            //"https://localhost:44309/api/auth/redirectedByGoogle",
            "https://anglicka-vyzva-api-dev.azurewebsites.net/api/auth/redirectedByFacebook",
            "https://anglicka-vyzva-api-dev.azurewebsites.net/api/auth/redirectedByApple"
            );

        private const string externalLoginSuccessHtml = "<h1>Přihlášení proběhlo úspěšně. Nyní můžete tuto stránku zavřít a přepnout se do aplikace.</h1><script>window.close()</script>";
        private const string externalLoginErrorHtml = "<h1>Přihlášení se nepodařilo. Použijte prosím jiný způsob přihášení.</h1>";

        public AuthController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {
        }

        /// <summary>
        /// Pravdepodobne to nejde spustit na Azure -> musi se to delat lokalne
        /// </summary>
        /// <returns></returns>
        [HttpGet("generateAppleClientSecret")]
        public IActionResult GenerateAppleClientSecret()
        {
            #region Vytvareni ClientSecret pro Apple (u googlu a facebooku se to jednoduse zkopiruje z webu. U applu se to musi vytvaret)
            // ---- VYTVARENI JWT tokenu jako Client Secret.

            // Generate a token valid for the maximum 6 months
            var expiresAt = DateTime.UtcNow.Add(TimeSpan.FromSeconds(15777000));

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Audience = "https://appleid.apple.com",
                Expires = expiresAt,
                Issuer = _config["Apple-Team-Id"], // Team ID. Jak ho ziskat: Kliknout na account -> View Membership -> Tady je Team ID
                Subject = new ClaimsIdentity(new[] { new Claim("sub", _config["Apple-Client-Id"]) }), // Id Servisy 
            };


            // Privatni klic .p8, ktery se stahne z Applu pro servisu. Tento klic jde z applu stahnout jenom jednou. .p8 klic je jenom text, ktery jsem nahral na Azure Vault.
            // Z klice jsem odstranil komentare. Pozor na nove radky - zadne nesmi v klici byt
            byte[] privateKeyBlob = Convert.FromBase64String(_config["Apple-Private-Service-Key"]);

            string clientSecret = null; // JWT token, ktery se posila do applu pro overeni tokenu. Google a Facebooku misto toho pouzivaji retezec, ktery je normalne pristupny na webu.


            // Create an ECDSA 256 algorithm to sign the token
            /* TADY BYL USING */
            // Tohle nebude fungovat na Azure, pokud se neudela nasledujici:
            // Do Settings/Configuration přidat hodnot  
            // WEBSITE_LOAD_USER_PROFILE = 1 ----------------------------------------------------------------------------------------
            var privateKey = CngKey.Import(privateKeyBlob, CngKeyBlobFormat.Pkcs8PrivateBlob);


            /* TADY BYL USING */
            var algorithm = new ECDsaCng(privateKey);


            algorithm.HashAlgorithm = CngAlgorithm.Sha256;

            var key = new ECDsaSecurityKey(algorithm) { KeyId = _config["Apple-Key-Id"] }; // V zalozce Certificates, Identifiers & Profiles na developer.apple.com u klice

            // Set the signing key for the token
            tokenDescriptor.SigningCredentials = new SigningCredentials(
                key,
                SecurityAlgorithms.EcdsaSha256Signature);

            // Create the token, which acts as the Client Secret
            var tokenHandler = new JwtSecurityTokenHandler();
            clientSecret = tokenHandler.CreateEncodedJwt(tokenDescriptor);


            ///* TADY KONCILY OBA USINGY */
            #endregion

            return Ok();
        }

        //[HttpGet("pokus")]
        //public IActionResult Pokus()
        //{
        //    Response.Cookies.Append("OHEN", "kouri", new CookieOptions
        //    {
        //        SameSite = SameSiteMode.None,
        //        Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan),
        //        Secure = true,
        //    });

        //    Response.Cookies.Append("DREVO", "roste", new CookieOptions
        //    {
        //        SameSite = SameSiteMode.None,
        //        Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan),
        //        Domain = "anglicka-vyzva-api.azurewebsites.net",
        //        Secure = true,
        //    });

        //    return Ok(new
        //    {
        //        super = "truper",
        //        cookiesCoJsemDostal = Request.Cookies,
        //    });
        //}

        //[HttpGet("pokusApp")]
        //public IActionResult PokusApp()
        //{
        //    Response.Cookies.Append("UKULELE", "zvoni", new CookieOptions
        //    {
        //        SameSite = SameSiteMode.None,
        //        Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan),
        //        Secure = true,
        //    });

        //    Response.Cookies.Append("PISTALKA", "piska", new CookieOptions
        //    {
        //        SameSite = SameSiteMode.None,
        //        Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan),
        //        Secure = true,
        //        HttpOnly = true,
        //    });

        //    Response.Cookies.Append("BUBINEK", "triska", new CookieOptions
        //    {
        //        SameSite = SameSiteMode.None,
        //        Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan),
        //        Secure = true,
        //        HttpOnly = true,
        //        Domain = "anglicka-vyzva-api.azurewebsites.net",
        //    });


        //    return Ok(new
        //    {
        //        cookiesCoJsemDostal = Request.Cookies,
        //    });
        //}

        //[HttpPost("pokusAppPost")]
        //public IActionResult PokusAppPost()
        //{
        //    Response.Cookies.Append("SKOKAN", "skace", new CookieOptions
        //    {
        //        SameSite = SameSiteMode.None,
        //        Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan),
        //        Secure = true,
        //    });

        //    Response.Cookies.Append("MESIC", "sviti", new CookieOptions
        //    {
        //        SameSite = SameSiteMode.None,
        //        Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan),
        //        Secure = true,
        //        HttpOnly = true,
        //    });

        //    Response.Cookies.Append("SLUNCE", "hreje", new CookieOptions
        //    {
        //        SameSite = SameSiteMode.None,
        //        Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan),
        //        Secure = true,
        //        HttpOnly = true,
        //        Domain = "anglicka-vyzva-api.azurewebsites.net",
        //    });


        //    return Ok(new
        //    {
        //        cookiesCoJsemDostal = Request.Cookies,
        //    });
        //}

        public class ChangePassword_Model
        {
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
        }
        [Authorize]
        [HttpPost("changePassword")]
        public async Task<IActionResult> ChangePassword(ChangePassword_Model model)
        {
            var user = await GetLoggedUser();



            if (user.LoginMethod != AuthenticationHelper.LoginMethods.Email)
            {
                LogRepo.Add(new Log(Log.Actions.PasswordChange, Log.DescriptionsPredefined.LoginBadMethod));
                await LogRepo.SaveAll();
                throw new Exception("Uživatel používá jiný typ přihlášení");
            }

            await SharedSemaphores.Semaphore_Auth_MaxLoginRequests.WaitAsync();
            try
            {
                if (AuthenticationHelper.MaxLoginRequestsDictionary_LastReset.AddMinutes(10) < DateTime.Now)
                {
                    AuthenticationHelper.MaxLoginRequestsDictionary.Clear();
                }

                if (!AuthenticationHelper.MaxLoginRequestsDictionary.ContainsKey(user.UserName))
                {
                    AuthenticationHelper.MaxLoginRequestsDictionary.Add(user.UserName, 0);
                }
                AuthenticationHelper.MaxLoginRequestsDictionary[user.UserName]++;

                if (AuthenticationHelper.MaxLoginRequestsDictionary[user.UserName] > 10)
                {
                    LogRepo.Add(new Log(Log.Actions.PasswordChange, Log.DescriptionsPredefined.TooManyAttempts));
                    await LogRepo.SaveAll();
                    throw new Exception("Heslo můžete zkusit maximálně 10x za 10 minut. Počkejte.");
                }
            }
            finally
            {
                SharedSemaphores.Semaphore_Auth_MaxLoginRequests.Release();
            }

            if (string.IsNullOrEmpty(model.CurrentPassword))
            {
                LogRepo.Add(new Log(Log.Actions.PasswordChange, Log.DescriptionsPredefined.PasswordNotProvided));
                await LogRepo.SaveAll();
                throw new Exception("Zadejte současné heslo");
            }
            if (string.IsNullOrEmpty(model.NewPassword))
            {
                LogRepo.Add(new Log(Log.Actions.PasswordChange, Log.DescriptionsPredefined.PasswordNotProvided));
                await LogRepo.SaveAll();
                throw new Exception("Zadejte nové heslo");
            }

            if (!AuthenticationHelper.ComparePassword(model.CurrentPassword, user.Salt, user.PasswordHash))
            {
                LogRepo.Add(new Log(Log.Actions.PasswordChange, Log.DescriptionsPredefined.PasswordWrong));
                await LogRepo.SaveAll();
                throw new Exception("Zadané současné heslo není správné");
            }

            if (!AuthenticationHelper.IsPasswordSecure(model.NewPassword))
            {
                LogRepo.Add(new Log(Log.Actions.PasswordChange, Log.DescriptionsPredefined.PasswordWeak));
                await LogRepo.SaveAll();
                throw new Exception(AuthenticationHelper.GetPasswordSecureErrorMessage(model.NewPassword));
            }

            var newSalt = AuthenticationHelper.GenerateSalt();
            var newPasswordHash = AuthenticationHelper.HashPassword(model.NewPassword, newSalt);

            user.Salt = newSalt;
            user.PasswordHash = newPasswordHash;

            LogRepo.Add(new Log(Log.Actions.PasswordChange, Log.DescriptionsPredefined.Success));

            await UserRepo.SaveAll();

            return Ok();
        }

        public class SendResetVerificationPasswordCode_Model
        {
            public string UserName { get; set; }
        }
        [HttpPost("sendResetPasswordVerificationCode")]
        public async Task<IActionResult> SendResetPasswordVerificationCode(SendResetVerificationPasswordCode_Model model)
        {
            if (!AuthenticationHelper.IsEmailValid(model.UserName))
            {
                LogRepo.Add(new Log(Log.Actions.PasswordResetCodeSend, Log.DescriptionsPredefined.EmailBadFormat));
                await LogRepo.SaveAll();
                throw new Exception("Email není ve správném formátu");
            }

            var user = await UserRepo.All.FirstOrDefaultAsync(x => x.UserName == model.UserName);

            if (user == null)
            {
                LogRepo.Add(new Log(Log.Actions.PasswordResetCodeSend, Log.DescriptionsPredefined.UserNotFound));
                await LogRepo.SaveAll();
                throw new Exception("Uživatel s tímto přihlašovacím emailem neexistuje");
            }

            if (user.LoginMethod != AuthenticationHelper.LoginMethods.Email)
            {
                LogRepo.Add(new Log(Log.Actions.PasswordResetCodeSend, Log.DescriptionsPredefined.LoginBadMethod));
                await LogRepo.SaveAll();
                throw new Exception("Tento účet používá jiný typ přihlášení");
            }

            var code = AuthenticationHelper.RandomNumericString(AuthenticationHelper.codeLength);



            await SharedSemaphores.Semaphore_Auth_ResetPassword.WaitAsync();

            try
            {
                // Zapisu si informace do staticke promenne, abych je mohl pri dalsim dotazu overit (ano, pri restartu serveru jsou zapomenuty)
                if (!AuthenticationHelper.ResetPasswordVerificationCodesDictionary.ContainsKey(user.Id))
                {
                    AuthenticationHelper.ResetPasswordVerificationCodesDictionary.Add(user.Id, null);
                }
                else
                {
                    var tuple = AuthenticationHelper.ResetPasswordVerificationCodesDictionary[user.Id];
                    if (tuple != null)
                    {
                        var seconds = 120;
                        if (tuple.Created.AddSeconds(seconds) > DateTime.Now)
                        {
                            LogRepo.Add(new Log(Log.Actions.PasswordResetCodeSend, Log.DescriptionsPredefined.TooEarly));
                            await LogRepo.SaveAll();
                            throw new Exception($"Ověřovací kód lze znovu odeslat až po {seconds} sekundách.");
                        }
                    }
                }
                AuthenticationHelper.ResetPasswordVerificationCodesDictionary[user.Id] = new AuthenticationHelper.TupleResetPassword(code, user.UserName);
            }
            finally
            {
                SharedSemaphores.Semaphore_Auth_ResetPassword.Release();
            }

            var emailSubject = "Reset hesla";
            var emailBody = EmailHelper.GenerateEmail("Reset hesla", $"" +
                $"Váš kód na reset hesla pro Anglickou Výzvu!:" +
                $"<br />" +
                $"<b>{code}</b>" +
                $"");
            EmailRepo.Add(new Email(user.UserName, emailSubject, emailBody));

            // Pokud ma overeny kontaktni email, poslu mu i na nej overovaci kod (pokud je prihlasovaci a kontaktni email stejny, poslu jenom jednou)
            if (user.IsEmailVerified && AuthenticationHelper.IsEmailValid(user.Email) && user.UserName != user.Email)
            {
                EmailRepo.Add(new Email(user.Email, emailSubject, emailBody));
            }


            LogRepo.Add(new Log(Log.Actions.PasswordResetCodeSend, Log.DescriptionsPredefined.Success));
            await LogRepo.SaveAll();

            return Ok();
        }

        public class CheckResetPasswordVerificationCode_Model
        {
            public string UserName { get; set; }
            public string Code { get; set; }
            public string NewPassword { get; set; }
        }
        [HttpPost("checkResetPasswordVerificationCode")]
        public async Task<IActionResult> CheckResetPasswordVerificationCode(CheckResetPasswordVerificationCode_Model model)
        {
            string errorMessage = "Špatný ověřovací kód";

            if (string.IsNullOrEmpty(model.NewPassword))
            {
                LogRepo.Add(new Log(Log.Actions.PasswordResetCodeCheck, Log.DescriptionsPredefined.PasswordNotProvided, model.UserName));
                await LogRepo.SaveAll();
                throw new Exception("Zadejte nové heslo");
            }

            if (!AuthenticationHelper.IsPasswordSecure(model.NewPassword))
            {
                LogRepo.Add(new Log(Log.Actions.PasswordResetCodeCheck, Log.DescriptionsPredefined.PasswordWeak, model.UserName));
                await LogRepo.SaveAll();
                throw new Exception(AuthenticationHelper.GetPasswordSecureErrorMessage(model.NewPassword));
            }

            var user = await UserRepo.All.FirstOrDefaultAsync(x => x.UserName == model.UserName);

            if (user == null)
            {
                LogRepo.Add(new Log(Log.Actions.PasswordResetCodeCheck, Log.DescriptionsPredefined.UserNotFound, model.UserName));
                await LogRepo.SaveAll();
                throw new Exception(errorMessage);
            }

            await SharedSemaphores.Semaphore_Auth_ResetPassword.WaitAsync();
            try
            {
                AuthenticationHelper.TuplesGarbageCollector(AuthenticationHelper.ResetPasswordVerificationCodesDictionary);

                if (!AuthenticationHelper.ResetPasswordVerificationCodesDictionary.ContainsKey(user.Id))
                {
                    LogRepo.Add(new Log(Log.Actions.PasswordResetCodeCheck, Log.DescriptionsPredefined.VerificationCodeNotFound, model.UserName));
                    await LogRepo.SaveAll();
                    throw new Exception(errorMessage);
                }

                var tuple = AuthenticationHelper.ResetPasswordVerificationCodesDictionary[user.Id];

                var res = AuthenticationHelper.CheckBaseTuple(tuple, model.Code);
                if (!res.IsCorrect)
                {
                    LogRepo.Add(new Log(Log.Actions.PasswordResetCodeCheck, res.LogDescriptionError, model.UserName));
                    await LogRepo.SaveAll();
                    throw new Exception(res.ErrorMessage);
                }

                // Spatny UserName
                if (tuple.UserName != model.UserName)
                {
                    LogRepo.Add(new Log(Log.Actions.PasswordResetCodeCheck, Log.DescriptionsPredefined.UserNotMatch, model.UserName));
                    await LogRepo.SaveAll();
                    throw new Exception(errorMessage);
                }

                // Vsechno probehlo v poradku, mazu pozadavek z dictionary
                AuthenticationHelper.ResetPasswordVerificationCodesDictionary.Remove(user.Id);
            }
            finally
            {
                SharedSemaphores.Semaphore_Auth_ResetPassword.Release();
            }

            var newSalt = AuthenticationHelper.GenerateSalt();
            var newPasswordHash = AuthenticationHelper.HashPassword(model.NewPassword, newSalt);

            user.Salt = newSalt;
            user.PasswordHash = newPasswordHash;

            LogRepo.Add(new Log(Log.Actions.PasswordResetCodeCheck, Log.DescriptionsPredefined.Success, model.UserName));

            await UserRepo.SaveAll();

            return Ok();
        }


        private class HandleExternalLoginRedirect_Model
        {

            public string LoginMethod { get; set; }
            public string State { get; set; }
            /// <summary>
            /// Muze by null. Vklada se do User.Firstname
            /// </summary>
            public string Firstname { get; set; }
            /// <summary>
            /// Muze by null. Vklada se do User.Lastname
            /// </summary>
            public string Lastname { get; set; }
            /// <summary>
            /// Email vsech externich prihlasenich dohromady (Kdyz pouzival google, tak se sem da google..)
            /// </summary>
            public string LoginEmail { get; set; }


            public string GoogleEmail { get; set; }
            public string GoogleUserId { get; set; }
            public string GoogleFirstname { get; set; }
            public string GoogleLastname { get; set; }
            public string GoogleImage { get; set; }
            public string FacebookEmail { get; set; }
            public string FacebookUserId { get; set; }
            public string FacebookFirstname { get; set; }
            public string FacebookLastname { get; set; }
            public string FacebookImage { get; set; }
            public string AppleEmail { get; set; }
            public string AppleUserId { get; set; }

        }
        private async Task HandleExternalLoginRedirect(HandleExternalLoginRedirect_Model model) // Privatni fce. Sdileny kod pro vsechny externi loginy
        {
            AuthenticationHelper.WaitForExternalLogin waitForExternalLogin;

            await SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.WaitAsync();
            try
            {
                waitForExternalLogin = AuthenticationHelper.WaitForExtarnalLoginList.FirstOrDefault(x => x.Guid == model.State);

                if (waitForExternalLogin == null)
                {
                    LogRepo.Add(new Log(Log.Actions.LoginExternalRedirect, Log.DescriptionsPredefined.GuidNotFound, model.GetType().Name));
                    await LogRepo.SaveAll();
                    throw new Exception("Neznámé guid");
                }

                if (waitForExternalLogin.Created.AddMinutes(AuthenticationHelper.ExternalLoginTimeoutMinutes) < DateTime.Now)
                {
                    LogRepo.Add(new Log(Log.Actions.LoginExternalRedirect, Log.DescriptionsPredefined.GuidExpired, model.GetType().Name));
                    await LogRepo.SaveAll();

                    waitForExternalLogin.ReceivedReturnRequest = true;
                    throw new Exception("Platnost guidu vypršela");
                }
            }
            finally
            {
                SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.Release();
            }



            if (waitForExternalLogin.ReceivedReturnRequest)
            {

                LogRepo.Add(new Log(Log.Actions.LoginExternalRedirect, Log.DescriptionsPredefined.ResponseAlreadyArrived, model.GetType().Name));
                await LogRepo.SaveAll();

                throw new Exception("Pro tento guid již přišla odpověď");
            }

            var user = await UserRepo.All.FirstOrDefaultAsync(x => x.UserName == model.LoginEmail || x.GoogleEmail == model.LoginEmail || x.FacebookEmail == model.LoginEmail || x.AppleEmail == model.LoginEmail);
            // Nova registrace bez problemu
            if (user == null)
            {
                if (waitForExternalLogin.IsTemporary)
                {
                    var temporaryUser = await UserRepo.All.FirstAsync(x => x.Id == waitForExternalLogin.TemporaryUserId);

                    temporaryUser.IsTemporary = false;

                    temporaryUser.LoginMethod = model.LoginMethod;
                    temporaryUser.Firstname = model.Firstname;
                    temporaryUser.Lastname = model.Lastname;

                    temporaryUser.GoogleEmail = model.GoogleEmail;
                    temporaryUser.GoogleUserId = model.GoogleUserId;
                    temporaryUser.GoogleFirstname = model.GoogleFirstname;
                    temporaryUser.GoogleLastname = model.GoogleLastname;
                    temporaryUser.FacebookEmail = model.FacebookEmail;
                    temporaryUser.FacebookUserId = model.FacebookUserId;
                    temporaryUser.FacebookFirstname = model.FacebookFirstname;
                    temporaryUser.FacebookLastname = model.FacebookLastname;
                    temporaryUser.FacebookImage = model.FacebookImage;
                    temporaryUser.AppleEmail = model.AppleEmail;
                    temporaryUser.AppleUserId = model.AppleUserId;

                    // Ulozim premenu docasneho uzivatele na normalniho
                    await UserRepo.SaveAll();

                    user = temporaryUser; // Dale se pracuje s User, tak aby mohl byt kod jednotny
                }
                else
                {

                    user = new User
                    {
                        LoginMethod = model.LoginMethod,
                        Firstname = model.Firstname,
                        Lastname = model.Lastname,

                        GoogleEmail = model.GoogleEmail,
                        GoogleUserId = model.GoogleUserId,
                        GoogleFirstname = model.GoogleFirstname,
                        GoogleLastname = model.GoogleLastname,
                        FacebookEmail = model.FacebookEmail,
                        FacebookUserId = model.FacebookUserId,
                        FacebookFirstname = model.FacebookFirstname,
                        FacebookLastname = model.FacebookLastname,
                        FacebookImage = model.FacebookImage,
                        AppleEmail = model.AppleEmail,
                        AppleUserId = model.AppleUserId,

                        // Kontaktni email
                        //Email = new string[] { model.AppleEmail, model.GoogleEmail, model.AppleEmail }.First(x=>x!= null)


                    };
                    UserRepo.Add(user);
                    await UserRepo.SaveAll();
                }

                await SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.WaitAsync();
                try
                {
                    waitForExternalLogin.ReceivedReturnRequest = true;
                    waitForExternalLogin.IsCorrect = true;
                    waitForExternalLogin.UserId = user.Id;
                }
                finally
                {
                    SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.Release();
                }
            }
            else // Nekdo se uz s timto emailem nejak registroval
            {
                string errorDescription = null;

                await SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.WaitAsync();
                try
                {

                    if (user.LoginMethod == model.LoginMethod)
                    {

                        if (user.LoginMethod == AuthenticationHelper.LoginMethods.Google && user.GoogleUserId != model.GoogleUserId)
                        {
                            waitForExternalLogin.ErrorMessage = "Id služby není správné";
                            //waitForExternalLogin.ReceivedReturnRequest = true;
                            waitForExternalLogin.IsCorrect = false;

                            errorDescription = Log.DescriptionsPredefined.IdNotMathch;
                        }
                        else if (user.LoginMethod == AuthenticationHelper.LoginMethods.Facebook && user.FacebookUserId != model.FacebookUserId)
                        {
                            waitForExternalLogin.ErrorMessage = "Id služby není správné";
                            //waitForExternalLogin.ReceivedReturnRequest = true;
                            waitForExternalLogin.IsCorrect = false;

                            errorDescription = Log.DescriptionsPredefined.IdNotMathch;
                        }
                        else if (user.LoginMethod == AuthenticationHelper.LoginMethods.Apple && user.AppleUserId != model.AppleUserId)
                        {
                            waitForExternalLogin.ErrorMessage = "Id služby není správné";
                            //waitForExternalLogin.ReceivedReturnRequest = true;
                            waitForExternalLogin.IsCorrect = false;

                            errorDescription = Log.DescriptionsPredefined.IdNotMathch;
                        }
                        else // Prihlasil jsem se normalne Google/Facebook/Apple Uctem
                        {
                            //waitForExternalLogin.ReceivedReturnRequest = true;
                            waitForExternalLogin.IsCorrect = true;
                            waitForExternalLogin.UserId = user.Id;

                            // Docasny uzivatel se chce prihlasit do normalniho existujiciho uctu -> zkopiruju postup docasneho uzivatele do existujiciho normalniho uctu
                            // - toto muze nastat napriklad kdyz si koupim kurz na pocatici a potom se chci na mobilu, kde mam temporary ucet, prihlasit do nove vytvoreneho uctu, ktery byl vytvoreny pri objednavce
                            if (waitForExternalLogin.IsTemporary)
                            {
                                await UserController.MergeProgressIntoAnotherUser(waitForExternalLogin.TemporaryUserId, user.Id, ProgressRepo, TestProgressRepo, LogRepo);
                            }

                        }
                    }
                    else // Uzivatel pouziva k prihlasovani nejaky jiny login
                    {
                        //waitForExternalLogin.ReceivedReturnRequest = true;
                        waitForExternalLogin.IsCorrect = false;
                        waitForExternalLogin.UsingAnotherProvider = true;

                        var errorMessage = "Použijte jiné přihlášení";
                        switch (user.LoginMethod)
                        {
                            case Entities.User.LoginMethods.Apple:
                                errorMessage = "Použijte přihlášení pomocí Applu.";
                                break;

                            case Entities.User.LoginMethods.Facebook:
                                errorMessage = "Použijte přihlášení pomocí Facebooku.";
                                break;

                            case Entities.User.LoginMethods.Google:
                                errorMessage = "Použijte přihlášení pomocí Googlu.";
                                break;

                            case Entities.User.LoginMethods.Email:
                                errorMessage = "Použijte přihlášení pomocí emailu.";
                                break;

                        }

                        waitForExternalLogin.ErrorMessage = errorMessage;
                        waitForExternalLogin.UsingLoginMethod = user.LoginMethod;

                        errorDescription = Log.DescriptionsPredefined.LoginBadMethod;
                    }
                }
                finally
                {
                    waitForExternalLogin.ReceivedReturnRequest = true;
                    SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.Release();
                }

                if (errorDescription != null)
                {
                    LogRepo.Add(new Log(Log.Actions.LoginExternalRedirect, errorDescription, model.GetType().Name));
                    await LogRepo.SaveAll();
                }
            }
        }

        [HttpGet("generateAutoclosingReturnPage")]
        public IActionResult GenerateAutoclosingReturnPage()
        {
            string str = "";
            if (Request.Query.ContainsKey("reloaded"))
            {
                str = "<script>setTimeout(function (){console.log('closing now'); window.close();}, 0);</script>";
            }
            else
            {
                str = "<!DOCTYPE html><html><head><script>function reloadThisTab() { window.location = \"generateAutoclosingReturnPage?reloaded=true\"; }</script></head><body onload=\"reloadThisTab()\"></body></html>";
            }
            return Content(str, "text/html");
        }

        public class RedirectedByGoogle_Model
        {
            public string state { get; set; }
            public string code { get; set; }
        }
        [HttpGet("redirectedByGoogle")]
        public async Task<IActionResult> RedirectedByGoogle([FromQuery] RedirectedByGoogle_Model model)
        {
            HttpClient httpClient;

            try
            {
                var url = "https://oauth2.googleapis.com/token";

                var client = new RestClient(url);
                var request = new RestRequest(Method.POST);

                var redirectUri = authenticationSettings.Google_Redirect_Uri;
                if(IsOnRemoteDebug())
                {
                    redirectUri = authenticationSettings_DEBUG.Google_Redirect_Uri;
                }

                request.Parameters.Clear();
                request.AddParameter("code", model.code);
                request.AddParameter("client_id", _config["Google-Client-Id"]);
                request.AddParameter("client_secret", _config["Google-Client-Secret"]);
                request.AddParameter("redirect_uri", redirectUri);
                request.AddParameter("grant_type", "authorization_code");
                request.AddParameter("access_type", "offline");

                IRestResponse restResponse = await client.ExecuteAsync(request);

                

                var googleRes = restResponse.Content;
                var ble = JsonConvert.DeserializeObject<dynamic>(googleRes);
                string id_token = (string)ble["id_token"];

                httpClient = new HttpClient();
                //var decodedToken = await httpClient.GetStringAsync("https://www.googleapis.com/oauth2/v3/tokeninfo?id_token=" + id_token);
                var decodedTokenResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/tokeninfo?id_token=" + id_token);
                if(decodedTokenResponse.IsSuccessStatusCode)
                {
                    var decodedToken = await decodedTokenResponse.Content.ReadAsStringAsync();

                    var userInfo = JsonConvert.DeserializeObject<dynamic>(decodedToken);

                    //var handler = new JwtSecurityTokenHandler();
                    //var jwt = handler.ReadJwtToken(decodedToken);
                    //var userId = jwt.Claims.FirstOrDefault((c) => c.Type == "sub").Value;
                    //var email = jwt.Claims.FirstOrDefault((c) => c.Type == "email")?.Value;

                    var userId = userInfo["sub"];
                    string email = userInfo["email"];
                    string firstname = userInfo["given_name"];
                    string lastname = userInfo["family_name"];
                    string picture = userInfo["picture"];


                    string state = model.state;


                    await HandleExternalLoginRedirect(new HandleExternalLoginRedirect_Model
                    {
                        LoginEmail = email,
                        LoginMethod = AuthenticationHelper.LoginMethods.Google,
                        State = state,
                        Firstname = firstname,
                        Lastname = lastname,

                        GoogleEmail = email,
                        GoogleFirstname = firstname,
                        GoogleLastname = lastname,
                        GoogleUserId = userId,
                        GoogleImage = picture,
                    });


                    //return Ok(new
                    //{
                    //    userId,
                    //    email,
                    //    firstname,
                    //    lastname,
                    //    picture,
                    //});

                    LogRepo.Add(new Log(Log.Actions.LoginExternalRedirect, Log.DescriptionsPredefined.Success, model.GetType().Name));
                    await LogRepo.SaveAll();


                    return Content(externalLoginSuccessHtml, "text/html");
                }
                else
                {
                    EmailRepo.Add(new Email("zbyneklazarek@gmail.com", "Chyba Google Loginu - špatný status code", $"status:<br/> {decodedTokenResponse.StatusCode}<br/><br/>Model: {JsonConvert.SerializeObject(model)}<br/><br/>decodedTokenResponse.Json: {JsonConvert.SerializeObject(decodedTokenResponse)}<br /><br />GoogleRes: {googleRes}"));

                    throw new Exception("Nastala chyba při Google přihlášení");
                }


                
            }
            catch (Exception exc)
            {
                EmailRepo.Add(new Email("zbyneklazarek@gmail.com", "Chyba Google Loginu", $"exc:<br/> {exc.ToString()}<br/><br/:Model: {JsonConvert.SerializeObject(model)}"));
                LogRepo.Add(new Log(Log.Actions.LoginExternalRedirect, Log.DescriptionsPredefined.ErrorUndefined, model.GetType().Name + ", " + exc));
                await LogRepo.SaveAll();
                return Content(externalLoginErrorHtml, "text/html");
            }
        }

        public class RedirectedByApple_Model
        {
            public string state { get; set; }
            public string code { get; set; }
        }
        [HttpPost("redirectedByApple")]
        [HttpGet("redirectedByApple")]
        public async Task<IActionResult> RedirectedByApple([FromForm] RedirectedByApple_Model model)
        {
            string appleTokenResponseString = null;
            string userId = null;
            string email = null;

            try
            {

                var authCode = model.code;





                #region Vytvareni ClientSecret pro Apple (u googlu a facebooku se to jednoduse zkopiruje z webu. U applu se to musi vytvaret)
                //// ---- VYTVARENI JWT tokenu jako Client Secret.

                //// Generate a token valid for the maximum 6 months
                //var expiresAt = DateTime.UtcNow.Add(TimeSpan.FromSeconds(15777000));

                //var tokenDescriptor = new SecurityTokenDescriptor()
                //{
                //    Audience = "https://appleid.apple.com",
                //    Expires = expiresAt,
                //    Issuer = _config["Apple-Team-Id"], // Team ID. Jak ho ziskat: Kliknout na account -> View Membership -> Tady je Team ID
                //    Subject = new ClaimsIdentity(new[] { new Claim("sub", _config["Apple-Client-Id"]) }), // Id Servisy 
                //};


                //// Privatni klic .p8, ktery se stahne z Applu pro servisu. Tento klic jde z applu stahnout jenom jednou. .p8 klic je jenom text, ktery jsem nahral na Azure Vault.
                //// Z klice jsem odstranil komentare. Pozor na nove radky - zadne nesmi v klici byt
                //byte[] privateKeyBlob = Convert.FromBase64String(_config["Apple-Private-Service-Key"]);

                //string clientSecret = null; // JWT token, ktery se posila do applu pro overeni tokenu. Google a Facebooku misto toho pouzivaji retezec, ktery je normalne pristupny na webu.


                //// Create an ECDSA 256 algorithm to sign the token
                ///* TADY BYL USING */
                //// Tohle nebude fungovat na Azure, pokud se neudela nasledujici:
                //// Do Settings/Configuration přidat hodnot  
                //// WEBSITE_LOAD_USER_PROFILE = 1 ----------------------------------------------------------------------------------------
                //var privateKey = CngKey.Import(privateKeyBlob, CngKeyBlobFormat.Pkcs8PrivateBlob);


                ///* TADY BYL USING */
                //var algorithm = new ECDsaCng(privateKey);


                //algorithm.HashAlgorithm = CngAlgorithm.Sha256;

                //var key = new ECDsaSecurityKey(algorithm) { KeyId = _config["Apple-Key-Id"] }; // V zalozce Certificates, Identifiers & Profiles na developer.apple.com u klice

                //// Set the signing key for the token
                //tokenDescriptor.SigningCredentials = new SigningCredentials(
                //    key,
                //    SecurityAlgorithms.EcdsaSha256Signature);

                //// Create the token, which acts as the Client Secret
                //clientSecret = tokenHandler.CreateEncodedJwt(tokenDescriptor);


                ///* TADY KONCILY OBA USINGY */
                #endregion

                // U BezSosuedu to jde vygenerovat normalne na produkci, ale v AnglickeVyzve to neslo (mozna FreeTier na azure). Proto jsem si to vygeneroval vedle v a do Vaultu nahral jenom vysledek
                // POZOR - musi se kazdeho pul roku prepocitat, protoze platnost nesmi byt delsi nez pul roku.
                var clientSecret = _config["Apple-Client-Secret"];

                string formData = string.Join("&",
                    new string[]
                    {
                    $"client_id={_config["Apple-Client-Id"]}", // Id Servisy
                    $"code={authCode}",
                    $"client_secret={clientSecret}",
                    $"grant_type=authorization_code",
                    $"scope=name%20email",
                    $"redirect_uri={WebUtility.UrlEncode(authenticationSettings.Apple_Redirect_Uri)}"
                    });


                var exchangeWC = new WebClient();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                exchangeWC.Headers.Add(HttpRequestHeader.UserAgent, "whateveryouwant");
                exchangeWC.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
                appleTokenResponseString = exchangeWC.UploadString("https://appleid.apple.com/auth/token", formData);
                var responseJSON = JObject.Parse(appleTokenResponseString);
                var id_token = responseJSON.GetValue("id_token").ToString();
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(id_token);
                userId = jwt.Claims.FirstOrDefault((c) => c.Type == "sub").Value;
                email = jwt.Claims.FirstOrDefault((c) => c.Type == "email")?.Value;

                if (string.IsNullOrWhiteSpace(email))
                {
                    throw new Exception("Nepodařilo se získat email");
                }

                #region Overeni, ze token prisel od Applu

                // Parse the JWT

                // Get the subject to use for the Name Identifier claim
                //string subject = userToken.Subject;

                // Get the public keys from https://appleid.apple.com/auth/keys
                var httpClient = new HttpClient();
                string keysJson = await httpClient.GetStringAsync("https://appleid.apple.com/auth/keys");

                // Parse the keys
                JsonWebKeySet keySet = JsonWebKeySet.Create(keysJson);

                //Setup the validation parameters
                var parameters = new TokenValidationParameters()
                {
                    ValidAudience = _config["Apple-Service-Id"], // ID servisy
                    ValidIssuer = "https://appleid.apple.com",
                    IssuerSigningKeys = keySet.Keys,
                };

                // Validate the token - ValidateToken(...) throws an exception if it is invalid
                handler.ValidateToken(id_token, parameters, out var _);
                #endregion

                await HandleExternalLoginRedirect(new HandleExternalLoginRedirect_Model
                {
                    LoginEmail = email,
                    LoginMethod = AuthenticationHelper.LoginMethods.Apple,
                    State = model.state,

                    AppleEmail = email,
                    AppleUserId = userId,
                });

                //return Ok(new
                //{
                //    userId,
                //    email,
                //    //bleh = Request.Form["state"]
                //});

                LogRepo.Add(new Log(Log.Actions.LoginExternalRedirect, Log.DescriptionsPredefined.Success, model.GetType().Name));
                await LogRepo.SaveAll();

                return Content(externalLoginSuccessHtml, "text/html");
            }
            catch (Exception exc)
            {
                LogRepo.Add(new Log(Log.Actions.LoginExternalRedirect, Log.DescriptionsPredefined.ErrorUndefined, model.GetType().Name + ", " + exc));
                EmailRepo.Add(new Email("zbyneklazarek@gmail.com", "Chyba Apple Loginu", $"exc:<br/> {exc.ToString()}<br/><br/>Model: {JsonConvert.SerializeObject(model)}<br/><br/>appleTokenResponseString: {appleTokenResponseString}<br/><br/>userId: {userId}, email: {email}"));
                await SaveAll();

                return Content(externalLoginErrorHtml, "text/html");
            }
        }

        public class RedirectedByFacebook_Model
        {
            public string state { get; set; }
            public string code { get; set; }
        }
        [HttpGet("redirectedByFacebook")]
        public async Task<IActionResult> RedirectedByFacebook([FromQuery] RedirectedByFacebook_Model model)
        {
            string firstname = null;
            string lastname = null;
            string email = null;
            string picture = null;

            try
            {
                var code = model.code;

                HttpClient client = new HttpClient();
                var urlAccessToken = "https://" + $"graph.facebook.com/v7.0/oauth/access_token?client_id={_config["Facebook-Client-Id"]}&redirect_uri={authenticationSettings.Facebook_Redirect_Uri}&client_secret={_config["Facebook-App-Secret"]}&code={code}";
                var accessTokenResponse = await client.GetStringAsync(urlAccessToken);
                var accessToken = JsonConvert.DeserializeObject<dynamic>(accessTokenResponse)["access_token"];

                var urlGenerateAppAccessToken = "https://" + $"graph.facebook.com/oauth/access_token?client_id={_config["Facebook-Client-Id"]}&client_secret={_config["Facebook-App-Secret"]}&grant_type=client_credentials";
                var generateAppAccessTokenResponse = await client.GetStringAsync(urlGenerateAppAccessToken);
                var appAccessToken = JsonConvert.DeserializeObject<dynamic>(generateAppAccessTokenResponse)["access_token"];


                // Zde se take zjisti, jake scopes povolil (mozna se to nemusi zjistovat, protoze chceme stejne jenom public veci)
                var urlInspectAccessTokenUrl = $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={appAccessToken}";
                var inspectAccessTokenResponse = await client.GetStringAsync(urlInspectAccessTokenUrl);
                var userId = JsonConvert.DeserializeObject<dynamic>(inspectAccessTokenResponse)["data"]["user_id"];

                var urlUserUserDetail = $"https://graph.facebook.com/v7.0/{userId}?access_token={accessToken}&fields=email,name,first_name,last_name,picture";
                var userDetail = await client.GetStringAsync(urlUserUserDetail);

                var userInfo = JsonConvert.DeserializeObject<dynamic>(userDetail);

                firstname = userInfo["first_name"];
                lastname = userInfo["last_name"];
                email = userInfo["email"];
                picture = null;

                if (string.IsNullOrWhiteSpace(email))
                {
                    throw new Exception("Přihlášení se nepodařilo. Použijte prosím jiný způsob přihlášení.");
                }

                string state = model.state;

                try
                {
                    picture = userInfo["picture"]["data"]["url"];
                }
                catch
                {

                }

                await HandleExternalLoginRedirect(new HandleExternalLoginRedirect_Model
                {
                    LoginEmail = email,
                    LoginMethod = AuthenticationHelper.LoginMethods.Facebook,
                    State = state,

                    FacebookEmail = email,
                    FacebookUserId = userId,
                    FacebookFirstname = firstname,
                    FacebookLastname = lastname,
                    FacebookImage = picture,
                });

                LogRepo.Add(new Log(Log.Actions.LoginExternalRedirect, Log.DescriptionsPredefined.Success, model.GetType().Name));
                await LogRepo.SaveAll();

                //return Ok(
                //    new
                //    {
                //        zdar = "klabos",
                //        queryString = Request.QueryString,
                //        userDetail = userDetail
                //    });
                return Content(externalLoginSuccessHtml, "text/html");
            }
            catch (Exception exc)
            {
                EmailRepo.Add(new Email("zbyneklazarek@gmail.com", "Chyba Facebook Loginu", $"firstName: {firstname}, lastName: {lastname}, email: {email}, code: {model.code}, state: {model.state}<br/>exc:<br/> {exc.ToString()}"));
                LogRepo.Add(new Log(Log.Actions.LoginExternalRedirect, Log.DescriptionsPredefined.ErrorUndefined, model.GetType().Name + ", " + exc));
                await SaveAll();
                return Content(externalLoginErrorHtml, "text/html");
            }
        }

        [HttpGet("redirectMeToExternalLogin/{provider}")]
        public async Task<IActionResult> RedirectMeToExternalLogin(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new Exception("není vyplněn poskytovatel přihlášení");
            }

            string url = null;

            var guid = AuthenticationHelper.CreateCryptographicallySecureGuid().ToString();

            if (provider == "google")
            {
                // Dokumentace: https://developers.google.com/identity/protocols/oauth2/web-server#httprest

                var apiUrl = "https://accounts.google.com/o/oauth2/v2/auth";
                //var scope = "https%3A//www.googleapis.com/auth/drive.metadata.readonly";
                //var scope = "httpss%3A//www.googleapis.com/auth/userinfo.email";
                //var scope = "https://www.googleapis.com/auth/plus.login";
                var scope = "https://www.googleapis.com/auth/userinfo.email";
                var redirectUrl = authenticationSettings.Google_Redirect_Uri;
                if(IsOnRemoteDebug())
                {
                    redirectUrl = authenticationSettings_DEBUG.Google_Redirect_Uri;
                }

                var clientId = _config["Google-Client-Id"];
                url = $"{apiUrl}?scope={scope}&access_type=offline&include_granted_scopes=true&response_type=code&state={guid}&redirect_uri={redirectUrl}&client_id={clientId}&prompt=select_account";
            }
            else if (provider == "facebook")
            {
                // https://developers.facebook.com/docs/facebook-login/manually-build-a-login-flow/
                url = "https://" + $"www.facebook.com/v7.0/dialog/oauth?client_id={_config["Facebook-Client-Id"]}&redirect_uri={authenticationSettings.Facebook_Redirect_Uri}&state={guid}&granted_scopes=id,email,first_name,last_name,picture&scope=email";
                // POZOR - musel jsem pridat na konec scope=email, protoze jinatk to NEVRACELO EMAIL. Je mozne, ze granted scopes je v dotazu spatne a nema tam byt. Moc to z dokumentace (odkaz o dva radky vyse nad dotazem) nechapu
                // MEGA POZOR !!!!!!!!!!!!!!!! Aplikace musi mit Povoleny Permission na Email -> developer.facebook.com -> vybra aplikaci -> vlevo App Review -> Permissions and Features
            }
            else if (provider == "apple")
            {
                // https://developer.apple.com/documentation/sign_in_with_apple/generate_and_validate_tokens // Dokumentace k REST HTTP dotazum - dobre

                // https://medium.com/identity-beyond-borders/how-to-configure-sign-in-with-apple-77c61e336003 -- Navod, podle ktereho to jde zprovoznit Sign In with google na Apple portalu
                // https://blog.martincostello.com/sign-in-with-apple-prototype-for-aspnet-core/ -- Navod, podle ktereho se overuji Apple tokeny

                url = "https://" + $"appleid.apple.com/auth/authorize?response_type=code&scope=name%20email&response_mode=form_post&redirect_uri={authenticationSettings.Apple_Redirect_Uri}&client_id={_config["Apple-Service-Id"]}&state={guid}";
            }

            if (url == null)
            {
                throw new Exception("neznámý poskytovatel přihlášení");
            }



            var waitForExternalLogin = new AuthenticationHelper.WaitForExternalLogin
            {
                Created = DateTime.Now,
                Guid = guid,
                Provider = provider,
            };



            // -- DOCASNY UZIVATEL -- //
            var loggedTemporaryUser = await GetLoggedUser();
            if (loggedTemporaryUser?.IsTemporary != null)
            {
                waitForExternalLogin.IsTemporary = true;
                waitForExternalLogin.TemporaryUserId = loggedTemporaryUser.Id;
            }
            // END -- DOCASNY UZIVATEL -- //


            await SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.WaitAsync();

            try
            {
                if (AuthenticationHelper.WaitForExtarnalLoginList.Any(x => x.Guid == waitForExternalLogin.Guid))
                {
                    throw new Exception("Požadavek s tímto guid již existuje"); // Tohle asi nikdy nenastane. Generator by se musel trefit do stejneho guidy -> nemozne
                }

                AuthenticationHelper.WaitForExtarnalLoginList.Add(waitForExternalLogin);
            }
            finally
            {
                SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.Release();
            }

            return Ok(new
            {
                url,
                guid
            });

        }

        /// <summary>
        /// Aplikace se pravidelne dotazuje, jestli uz prisla odpoved od externiho poskytovatele prihlaseni.
        /// </summary>
        public class CheckWaitForExternalLogin_Model
        {
            public string Guid { get; set; }
        }
        [HttpPost("checkWaitForExternalLogin")]
        public async Task<IActionResult> CheckWaitForExternalLogin(CheckWaitForExternalLogin_Model model)
        {
            if (string.IsNullOrWhiteSpace(model.Guid))
            {
                LogRepo.Add(new Log(Log.Actions.LoginExternalWaitCheck, Log.DescriptionsPredefined.GuidNotProvided));
                await LogRepo.SaveAll();
                throw new Exception("Guid nesmý být prázdný");
            }

            AuthenticationHelper.WaitForExternalLogin waitForExternalLogin;

            await SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.WaitAsync();
            try
            {
                waitForExternalLogin = AuthenticationHelper.WaitForExtarnalLoginList.FirstOrDefault(x => x.Guid == model.Guid);
            }
            finally
            {
                SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.Release();
            }

            if (waitForExternalLogin == null)
            {
                LogRepo.Add(new Log(Log.Actions.LoginExternalWaitCheck, Log.DescriptionsPredefined.GuidNotFound));
                await LogRepo.SaveAll();
                throw new Exception("Tento guid neexistuje");
            }

            object response = null;

            string errorDescription = null;
            string errorValues = null;

            if (!waitForExternalLogin.ReceivedReturnRequest)
            {
                return Ok(new
                {
                    receivedReturnRequest = false
                });
            }
            else
            {


                if (!waitForExternalLogin.IsCorrect)
                {
                    // Pouziva jiny druh prihlaseni
                    if (waitForExternalLogin.UsingAnotherProvider)
                    {
                        response = new
                        {
                            isCorrect = false,
                            usingAnotherProvider = true,
                            waitForExternalLogin.UsingLoginMethod,
                            errorMessage = waitForExternalLogin.ErrorMessage,
                            receivedReturnRequest = true
                        };

                        errorDescription = Log.DescriptionsPredefined.LoginBadMethod;
                    }
                    else
                    {
                        response = new
                        {
                            isCorrect = false,
                            errorMessage = waitForExternalLogin.ErrorMessage,
                            receivedReturnRequest = true,
                        };

                        errorDescription = Log.DescriptionsPredefined.ErrorUndefined;
                        errorValues = waitForExternalLogin.ErrorMessage;
                    }
                }
                else // IsCorrect = true (jeste musim zkontrolovat, jestli nevyprsel cas)
                {
                    if (waitForExternalLogin.Created.AddMinutes(AuthenticationHelper.ExternalLoginTimeoutMinutes) < DateTime.Now)
                    {
                        response = new
                        {
                            isCorrect = false,
                            guidExpired = true,
                            errorMessage = "Platnost guid vypršela",
                            receivedReturnRequest = true
                        };

                        errorDescription = Log.DescriptionsPredefined.GuidExpired;
                    }
                    else
                    {
                        var user = await UserRepo.All.FirstOrDefaultAsync(x => x.Id == waitForExternalLogin.UserId);


                        var userDto = _mapper.Map<UserDto>(user);
                        var token = AuthenticationHelper.CreateToken(_config["TokenPrivateKey"], user.Id.ToString(), user.GetLoginEmail());

                        //Response.Cookies.Append("authorization", token, new CookieOptions { HttpOnly = true, Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan), SameSite = SameSiteMode.None, Secure = true });

                        response = new
                        {
                            isCorrect = true,
                            receivedReturnRequest = true,
                            token = token, // Mobil pouzije tento token, web pouzije cookie
                            user = userDto
                        };
                    }
                }
            }



            // Dotaz probehl v poradku, smazu cekacku ze seznamu, protoze uz nikdy nebude potreba
            await SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.WaitAsync();
            try
            {
                AuthenticationHelper.WaitForExtarnalLoginList.Remove(waitForExternalLogin);
            }
            finally
            {
                SharedSemaphores.Semaphore_Auth_WaitForExternalLogin.Release();
            }

            if (errorDescription != null)
            {
                LogRepo.Add(new Log(Log.Actions.LoginExternalWaitCheck, errorDescription, errorValues));
                await LogRepo.SaveAll();
            }
            else
            {
                LogRepo.Add(new Log(Log.Actions.LoginExternalWaitCheck, Log.DescriptionsPredefined.Success));
                await LogRepo.SaveAll();
            }

            return Ok(response);
        }

        [HttpPost("fakeLogin")]
        public async Task<IActionResult> FakeLogin(FakeLoginDto fakeLoginDto)
        {
            var now = DateTime.Now;

            if (now.Year != 2022 || now.Month != 3 || now.Day != 4)
            {
                throw new Exception("Nene");
            }

            var email = fakeLoginDto.Email;

            // Prvne se zkusim prihlasim pres UserName -> spravny postup
            var userFromRepo = await UserRepo.All.FirstOrDefaultAsync(x => x.UserName == email || x.FacebookEmail == email || x.GoogleEmail == email);
            if (userFromRepo == null)
            {
                userFromRepo = await UserRepo.All.FirstOrDefaultAsync(x => x.Email == email);
            }

            // Kdyz nenajdu nikoho s UserName, tak vytahnu prvniho podle KONTAKTNIHO emailu
            if (userFromRepo == null)
            {
                LogRepo.Add(new Log(Log.Actions.LoginFake, Log.DescriptionsPredefined.UserNotFound, email));
                await LogRepo.SaveAll();

                throw new Exception("Tento email neexistuje");
            }

            var userDto = _mapper.Map<UserDto>(userFromRepo);
            var token = AuthenticationHelper.CreateToken(_config["TokenPrivateKey"], userFromRepo.Id.ToString(), userFromRepo.GetLoginEmail());

            //Response.Cookies.Append("authorization", token, new CookieOptions { HttpOnly = true, Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan), SameSite = SameSiteMode.None, Secure = true });
            //AuthenticationHelper.AddAuthorizationCookie(Response, Request, token);

            LogRepo.Add(new Log(Log.Actions.LoginFake, Log.DescriptionsPredefined.Success, email));
            await LogRepo.SaveAll();

            return Ok(new
            {
                token = token, // Mobil pouzije tento token, web pouzije cookie
                user = userDto
            });
        }

        // Smaze notifikacni token, aby uz nechodil na zarizeni (prave jsem se z nej odhlasil
        public class LogOut_Model
        {
            public string NotificationToken { get; set; }
        }
        [HttpPost("logOut")]
        public async Task<IActionResult> LogOut(LogOut_Model model)
        {
            if (!string.IsNullOrWhiteSpace(model.NotificationToken))
            {
                var tokenDb = await NotificationTokenRepo.All.FirstOrDefaultAsync(x => x.Token == model.NotificationToken);
                if (tokenDb != null)
                {
                    tokenDb.EntityState = BaseEntity.EntityStates.Deleted;
                    await NotificationTokenRepo.SaveAll();
                }
            }

            //Response.Cookies.Delete("authorization");

            LogRepo.Add(new Log(Log.Actions.Logout, Log.DescriptionsPredefined.Success));
            await LogRepo.SaveAll();

            return Ok();
        }

        [Authorize]
        [HttpPost("logOutAllDevices")]
        public async Task<IActionResult> LogOutAllDevices()
        {
            var user = await UserRepo.All.FirstAsync(x => x.Id == UserId);
            user.LogOutAllDevicesDate = DateTime.Now;

            // Smazu vsechny notifikacni tokeny -> nebudou mu chodit notifikace na zadne zarizeni
            var allNotificationTokens = await NotificationTokenRepo.All.Where(x => x.UserId == user.Id).ToListAsync();
            allNotificationTokens.ForEach(x => x.EntityState = BaseEntity.EntityStates.Deleted);

            await NotificationTokenRepo.SaveAll();

            await AuthenticationHelper.AddLogOutFromDevice(user.Id, (DateTime)user.LogOutAllDevicesDate);

            //Response.Cookies.Delete("authorization");

            LogRepo.Add(new Log(Log.Actions.LogoutAllDevices, Log.DescriptionsPredefined.Success, user.UserName));
            await LogRepo.SaveAll();

            return Ok();
        }

        public class RegisterNotificationToken_Model
        {
            public int DeviceType { get; set; }
            public string Token { get; set; }
        }
        [Authorize]
        [HttpPost("registerNotificationToken")]
        public async Task<IActionResult> RegisterNotificationToken(RegisterNotificationToken_Model model)
        {
            var tokenDb = await NotificationTokenRepo.All.FirstOrDefaultAsync(x => x.Token == model.Token);

            if (tokenDb != null)
            {
                // Prihlasil jsem se pod jinym uctem na zarizeni, ktere jiz drive dostalo token -> stary token musim smazat, jinak by mi ted chodili oznameni i pro uzivatele, ktery neni prihlasny
                if (tokenDb.UserId != UserId)
                {
                    tokenDb.EntityState = BaseEntity.EntityStates.Deleted;
                }
            }


            // Tento token jeste neni zaregistrovany -> zaregistruji ho
            if (tokenDb == null || tokenDb.UserId != UserId)
            {
                NotificationTokenRepo.Add(new NotificationToken
                {
                    UserId = UserId,
                    DeviceType = model.DeviceType,
                    Token = model.Token,
                });


            }

            LogRepo.Add(new Log(Log.Actions.NotificationTokenRegistration, Log.DescriptionsPredefined.Success, UserId.ToString()));

            await NotificationTokenRepo.SaveAll();
            return Ok();
        }

        [HttpPost("registerTemporaryUser")]
        public async Task<IActionResult> RegisterTemporaryUser()
        {
            var user = new User
            {
                IsTemporary = true,
                TemporaryUserName = Guid.NewGuid().ToString().Replace("-", "")
            };

            UserRepo.Add(user);
            await UserRepo.SaveAll();

            var userDto = _mapper.Map<UserDto>(user);
            var token = AuthenticationHelper.CreateToken(_config["TokenPrivateKey"], user.Id.ToString(), user.TemporaryUserName);

            //Response.Cookies.Append("authorization", token, new CookieOptions { HttpOnly = true, Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan), SameSite = SameSiteMode.None, Secure = true });

            return Ok(new
            {
                token = token, // Mobil pouzije tento token, web pouzije cookie
                user = userDto
            });
        }

        public class RegisterByEmail_Model
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public string PasswordConfirm { get; set; }
            //public string Firstname { get; set; }
            //public string Lastname { get; set; }
            //public string Phone { get; set; }
        }
        [HttpPost("registerByEmail")]
        public async Task<IActionResult> RegisterByEmail(RegisterByEmail_Model model)
        {
            if (!AuthenticationHelper.IsPasswordSecure(model.Password))
            {
                LogRepo.Add(new Log(Log.Actions.RegisterByEmail, Log.DescriptionsPredefined.PasswordWeak));
                await LogRepo.SaveAll();
                throw new Exception(AuthenticationHelper.GetPasswordSecureErrorMessage(model.Password));
            }

            var email = model.Email.Trim().ToLower();

            var user = await UserRepo.All.FirstOrDefaultAsync(x => x.UserName == email || x.GoogleEmail == email || x.FacebookEmail == email || x.AppleEmail == email);

            // Nekdo uz nejakym zpusobem tento email pouziva
            if (user != null)
            {
                if (user.LoginMethod == AuthenticationHelper.LoginMethods.Email)
                {
                    LogRepo.Add(new Log(Log.Actions.RegisterByEmail, Log.DescriptionsPredefined.EmailAlreadyInUse, email));
                    await LogRepo.SaveAll();

                    throw new Exception("Tento email je již obsazený");
                }
                else // Email se pouziva k externimu loginu
                {
                    LogRepo.Add(new Log(Log.Actions.RegisterByEmail, Log.DescriptionsPredefined.LoginBadMethod, email)); // Tady by spise melo byt EmailAlreadyInUse, ale to bych potom nevedel, jaka chyba nastala, protoze uz to pouzivam vyse
                    await LogRepo.SaveAll();

                    throw new Exception("Tento email se používá k jinému typu přihlášení");
                }
            }

            string salt = AuthenticationHelper.GenerateSalt();


            var loggedTemporaryUser = await GetLoggedUser();
            // Docasny uzivatel se registruje
            if (loggedTemporaryUser?.IsTemporary == true)
            {
                loggedTemporaryUser.IsTemporary = false;

                loggedTemporaryUser.UserName = email;
                loggedTemporaryUser.Email = email;
                loggedTemporaryUser.Salt = salt;
                loggedTemporaryUser.PasswordHash = AuthenticationHelper.HashPassword(model.Password, salt);
                loggedTemporaryUser.LoginMethod = AuthenticationHelper.LoginMethods.Email;


                LogRepo.Add(new Log(Log.Actions.RegisterByEmail_Temporary, Log.DescriptionsPredefined.Success, email));
                await LogRepo.SaveAll(); // Ulozim LOG + zmenu Docasneho uzivatele na normalniho


                user = loggedTemporaryUser; // Z user se tvori Token a cookie


            }
            else // Normalni registrace 
            {
                user = new User
                {
                    UserName = email, // Prihlasovaci email
                    Email = email, // Kontaktni email
                    Salt = salt,
                    PasswordHash = AuthenticationHelper.HashPassword(model.Password, salt),
                    //Firstname = model.Firstname,
                    //Lastname = model.Lastname,
                    //Phone = AuthenticationHelper.GetPhoneCorrectFormat(model.Phone),
                    LoginMethod = AuthenticationHelper.LoginMethods.Email,
                };

                UserRepo.Add(user);
                await UserRepo.SaveAll();
            }

            var userDto = _mapper.Map<UserDto>(user);

            LogRepo.Add(new Log(Log.Actions.RegisterByEmail, Log.DescriptionsPredefined.Success, email));
            await LogRepo.SaveAll();

            var token = AuthenticationHelper.CreateToken(_config["TokenPrivateKey"], user.Id.ToString(), user.UserName);

            //Response.Cookies.Append("authorization", token, new CookieOptions { HttpOnly = true, Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan), SameSite = SameSiteMode.None, Secure = true });

            return Ok(new
            {
                token = token, // Mobil pouzije tento token, web pouzije cookie
                user = userDto
            });
        }


        public class LoginByEmail_Model
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
        [HttpPost("loginByEmail")]
        public async Task<IActionResult> LoginByEmail(LoginByEmail_Model model)
        {


            var user = await UserRepo.All.FirstOrDefaultAsync(x => x.UserName == model.Email);
            if (user == null)
            {
                LogRepo.Add(new Log(Log.Actions.LoginByEmail, Log.DescriptionsPredefined.UserNotFound, model.Email));
                await LogRepo.SaveAll();
                throw new Exception("Uživatel s tímto emailem neexistuje");
            }

            if (user.LoginMethod != AuthenticationHelper.LoginMethods.Email)
            {
                LogRepo.Add(new Log(Log.Actions.LoginByEmail, Log.DescriptionsPredefined.LoginBadMethod, model.Email));
                await LogRepo.SaveAll();
                throw new Exception("Tento email se používá k jinému typu přihlášení");
            }

            if (string.IsNullOrEmpty(user.Salt) || string.IsNullOrEmpty(user.PasswordHash))
            {
                LogRepo.Add(new Log(Log.Actions.LoginByEmail, Log.DescriptionsPredefined.ErrorUndefined, model.Email + ", resetujte si heslo"));
                await LogRepo.SaveAll();
                throw new Exception("Resetujte si heslo");
            }

            await SharedSemaphores.Semaphore_Auth_MaxLoginRequests.WaitAsync();
            try
            {
                if (AuthenticationHelper.MaxLoginRequestsDictionary_LastReset.AddMinutes(10) < DateTime.Now)
                {
                    AuthenticationHelper.MaxLoginRequestsDictionary.Clear();
                }

                if (!AuthenticationHelper.MaxLoginRequestsDictionary.ContainsKey(user.UserName))
                {
                    AuthenticationHelper.MaxLoginRequestsDictionary.Add(user.UserName, 0);
                }
                AuthenticationHelper.MaxLoginRequestsDictionary[user.UserName]++;

                if (AuthenticationHelper.MaxLoginRequestsDictionary[user.UserName] > 10)
                {
                    LogRepo.Add(new Log(Log.Actions.LoginByEmail, Log.DescriptionsPredefined.TooManyAttempts));
                    await LogRepo.SaveAll();
                    throw new Exception("Heslo můžete zkusit maximálně 10x za 10 minut. Počkejte.");
                }
            }
            finally
            {
                SharedSemaphores.Semaphore_Auth_MaxLoginRequests.Release();
            }

            if (!AuthenticationHelper.ComparePassword(model.Password, user.Salt, user.PasswordHash))
            {
                LogRepo.Add(new Log(Log.Actions.LoginByEmail, Log.DescriptionsPredefined.PasswordWrong, model.Email));
                await LogRepo.SaveAll();
                throw new Exception("Heslo není správné");
            }

            LogRepo.Add(new Log(Log.Actions.LoginByEmail, Log.DescriptionsPredefined.Success, model.Email));

            // -- DOCASNY UZIVATEL -- //
            var loggedUser = await GetLoggedUser();
            if (loggedUser?.IsTemporary == true)
            {
                // Docasny uzivatel se chce prihlasit do normalniho existujiciho uctu -> zkopiruju postup docasneho uzivatele do existujiciho normalniho uctu
                // - toto muze nastat napriklad kdyz si koupim kurz na pocatici a potom se chci na mobilu, kde mam temporary ucet, prihlasit do nove vytvoreneho uctu, ktery byl vytvoreny pri objednavce
                await UserController.MergeProgressIntoAnotherUser(loggedUser.Id, user.Id, ProgressRepo, TestProgressRepo, LogRepo);
            }
            // -- DOCASNY UZIVATEL -- //

            var token = AuthenticationHelper.CreateToken(_config["TokenPrivateKey"], user.Id.ToString(), user.UserName);

            //Response.Cookies.Append("authorization", token, new CookieOptions { HttpOnly = true, Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan), SameSite = SameSiteMode.None, Secure = true });
            //Response.Cookies.Append("ROSNICKA", "kvaka", new CookieOptions
            //{
            //    SameSite = SameSiteMode.None,
            //    Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan),
            //    Secure = true,
            //    HttpOnly = true,
            //});

            await SaveAll();

            var userDto = _mapper.Map<UserDto>(user);

            return Ok(new
            {
                token = token, // Mobil pouzije tento token, web pouzije cookie
                user = userDto
            });
        }

        [HttpGet("getLoginMethodByEmail/{email}")]
        public async Task<IActionResult> GetLoginMethodByEmail(string email)
        {
            var user = await UserRepo.All.FirstOrDefaultAsync(x => x.UserName == email || x.GoogleEmail == email || x.FacebookEmail == email || x.AppleEmail == email);

            var method = "no-user";
            if (user != null)
            {
                method = user.LoginMethod;
            }

            if (string.IsNullOrWhiteSpace(method))
            {
                throw new Exception("Uživatel nemá zvolenou žádnou přihlašovací metodu");
            }

            return Ok(new
            {
                method
            });
        }

    }
}