using AnglickaVyzva.API.Controllers;
using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static AnglickaVyzva.API.Controllers.OrderController;

namespace AnglickaVyzva.API.Helpers
{
    public class OrderAndChallengeHelper
    {
        static SemaphoreSlim semaphoreEndlessRunner = new SemaphoreSlim(1, 1);

        public static SemaphoreSlim semaphore_giftCode = new SemaphoreSlim(1, 1);
        static Random random = new Random();

        public static readonly string comGateSecret = "xxx";

        public const decimal monthPrice_Decimal = 298;
        public const decimal monthPriceHalf_Decimal = 149;
        public const string monthPrice_Bank = "29800";
        public const string monthPriceHalf_Bank = "14900";
        public const string monthPrice_String = "298";
        public const string monthPriceHalf_String = "149";

        public const decimal yearPrice_Decimal = 1490;
        public const string yearPrice_Bank = "149000";
        public const string yearPrice_String = "1 490";

        /// <summary>
        /// Automaticky prodluzuje vyzvy pri rocnim predplatnem. Vytvari objednavky pro vyprsene mesicni predplatne
        /// </summary>
        /// <param name="connectionString"></param>
        public static void RunEndlessOrderAndChallengeChecker(string connectionString)
        {
            Task.Run(async () =>
            {
                try
                {
                    await semaphoreEndlessRunner.WaitAsync();


                    DateTime? lastRunDate = null;

                    while (true)
                    {
                        try
                        {
                            // Prave zacal novy den -> vsechno to zkontroluju a nastavim
                            if (lastRunDate == null || DateTime.Now.Day != ((DateTime)lastRunDate).Day)
                            {
                                #region Vytvareni repozitaru

                                var dbContext = new DataContext(new DbContextOptionsBuilder<DataContext>().UseSqlServer(connectionString).Options);

                                var personalChallengeRepo = new EFPersonalChallengeRepo(
                                        dbContext,
                                        SaveParameters.Factory_ForCrone()
                                    );

                                var progressRepo = new EFProgressRepo(
                                        dbContext,
                                        SaveParameters.Factory_ForCrone()
                                    );

                                var userRepo = new EFUserRepo(
                                        dbContext,
                                        SaveParameters.Factory_ForCrone()
                                    );

                                var orderRepo = new EFOrderRepo(
                                        dbContext,
                                        SaveParameters.Factory_ForCrone()
                                    );

                                var testProgressRepo = new EFTestProgressRepo(
                                        dbContext,
                                        SaveParameters.Factory_ForCrone()
                                    );

                                var chestPointsUsesRepo = new EFChestPointsUsesRepo(
                                        dbContext,
                                        SaveParameters.Factory_ForCrone()
                                    );

                                var promoCodeRepo = new EFPromoCodeRepo(
                                        dbContext,
                                        SaveParameters.Factory_ForCrone()
                                    );

                                var emailRepo = new EFEmailRepo(
                                        dbContext,
                                        SaveParameters.Factory_ForCrone()
                                    );

                                var topicPointsRepo = new EFTopicPointsRepo(
                                        dbContext,
                                        SaveParameters.Factory_ForCrone()
                                    );

                                #endregion

                                var now = DateTime.Now;

                                // Rocni predplatne -> pokud ma zaplaceno, jednoduse mu vytvorim novou vyzvu.
                                //                     pokud nema zaplaceno, zobrazi se mu hlaska ze mu kurz vyprsel, at si ho koupi
                                var yearUsers = await userRepo.All.Where(x =>
                                    x.IsPremium &&
                                    x.PrepaidUntil > now &&
                                    (
                                        x.SubscriptionType == "year" ||
                                        x.SubscriptionType == "gift"
                                    )
                                    ).ToListAsync();

                                foreach (var user in yearUsers)
                                {
                                    // Pokud se jim nova vyzva nevytvori, protoze uz nemaji predplaceno, nic se nedeje.
                                    await PersonalChallengeController.GetOrCreateActivePersonalChallenge(personalChallengeRepo, userRepo, user.Id);
                                }
                                // END Rocni predplatne


                                // ----




                                // Mesicni predplatne -> jenom vytvorim platby. Nova vyzva se vytvori pri procesu zpracovani odpovedi od ComGate.
                                //                     - pokud se nepovede automaticky strhnout penize z karty, tak se to zkusi dalsi den znovu, a znovu, a znovu...
                                var monthUsers = await userRepo.All
                                    .Include(x => x.ActiveOrder)
                                    .Where(x =>
                                        x.IsPremium &&
                                        x.DoNotRenewSubscription != true && // Nezrusil predplatne
                                        x.SubscriptionType == "month" &&
                                        x.PrepaidUntil < now && // Vyprselo predplatne
                                        x.PendingRenewOrderId == null && // Neceka na zaplaceni platby (treba ze vcera)
                                        x.RenewOrderError == null && // Pri vytvareni nove mesicni platby nevznikla chyba
                                        x.ActiveOrderId != null // ODFILTRUJI UZIVATELE, kteri to maji z aktivacniho kodu (napr to nekde vyhrali)
                                    )
                                    .ToListAsync();


                                foreach (var user in monthUsers)
                                {
                                    try
                                    {
                                        string initRecurringId;
                                        if (user.ActiveOrder.IsInitRecurring == true) // Toto je iniciacni platbla -> beru jeji TransId
                                        {
                                            initRecurringId = user.ActiveOrder.ComGateTransId;
                                        }
                                        else // Tato platba uz byla vytvorena na zaklade predchozi iniciacni platby -> vezmu ulozene TransId, ktere ovsem neni jeji vlastni TransId
                                        {
                                            initRecurringId = user.ActiveOrder.InitRecurringId;
                                        }

                                        var model = new CreateOrder_Model
                                        {
                                            Name = user.ActiveOrder.ClientName,
                                            Street = user.ActiveOrder.Street,
                                            City = user.ActiveOrder.City,
                                            PostalCode = user.ActiveOrder.PostalCode,
                                            Phone = user.ActiveOrder.Phone,
                                            EmailForOrder = user.ActiveOrder.EmailForOrder,
                                            EmailForApp = user.ActiveOrder.EmailForApp,
                                            IsMonthSubscription = user.ActiveOrder.IsMonthSubscription,
                                            IsYearSubscription = user.ActiveOrder.IsYearSubscription,
                                            IsGift = user.ActiveOrder.IsGift,
                                            ICO = user.ActiveOrder.ICO,
                                            DIC = user.ActiveOrder.DIC,

                                            IsAutomaticalyRenewing = true,
                                            InitRecuringId = initRecurringId,
                                        };

                                        var res = await CreateOrder_Shared(model, dbContext, userRepo, personalChallengeRepo, progressRepo, testProgressRepo, chestPointsUsesRepo, promoCodeRepo, orderRepo, emailRepo, topicPointsRepo);

                                        if (res.Ok == false)
                                        {
                                            emailRepo.Add(new Email("podpora@nexti.cz", "Anglická výzva chyba - Automatické obnovení předplatného [neošetřená chyba]", res.ErrorMessage));
                                            await emailRepo.SaveAll();
                                        }
                                    }
                                    catch (Exception exc)
                                    {
                                        emailRepo.Add(new Email("podpora@nexti.cz", "Anglická výzva chyba - Automatické obnovení předplatného [EXC]", exc.Message));
                                        emailRepo.Add(new Email("zbyneklazarek@gmail.com", "Anglická výzva chyba - Automatické obnovení předplatného [EXC]", exc.Message));
                                        await emailRepo.SaveAll();
                                    }
                                }

                                // END Mesicni predplatne


                                // Probehlo vsechno bez chyby - dneska padla
                                lastRunDate = DateTime.Now;
                            }
                        }
                        catch (Exception exc)
                        {
                            try
                            {
                                var context = new DataContext(new DbContextOptionsBuilder<DataContext>().UseSqlServer(connectionString).Options);

                                var emailRepo = new EFEmailRepo(context, SaveParameters.Factory_ForCrone());
                                emailRepo.Add(new Email("zbyneklazarek@gmail.com", "Chyba pri automatickem obnovovani noveho mesice", $"Chyba: <br /> {exc.ToString()}"));
                                await emailRepo.SaveAll();
                            }
                            catch
                            {

                            }
                            Debug.WriteLine("OrderAndChallenge ERROR: ", exc.ToString());
                        }

                        await Task.Delay(5000);
                    }
                }
                finally
                {
                    semaphoreEndlessRunner.Release();
                }
            });
        }



        public class CreateOrder_Result
        {
            public bool Ok { get; set; }
            public string Redirect { get; set; }
            public string ErrorMessage { get; set; }

            public CreateOrder_Result()
            {
            }

            public CreateOrder_Result(string errorMessage)
            {
                Ok = false;
                ErrorMessage = errorMessage;
            }
        }
        public class CreateOrder_Model
        {
            public string Name { get; set; }
            public string Street { get; set; }
            public string City { get; set; }
            public string PostalCode { get; set; }
            public string Phone { get; set; }
            public string EmailForOrder { get; set; }
            public string EmailForApp { get; set; }

            public string ICO { get; set; }
            public string DIC { get; set; }

            public string Password { get; set; }

            public bool? IsMonthSubscription { get; set; }
            public bool? IsYearSubscription { get; set; }
            public bool? IsGift { get; set; }

            public string PromoCode { get; set; }

            public bool IsAutomaticalyRenewing { get; set; }
            public string InitRecuringId { get; set; }
        }
        public static async Task<CreateOrder_Result> CreateOrder_Shared(CreateOrder_Model model, DataContext _dbContext, EFUserRepo userRepo, EFPersonalChallengeRepo personalChallengeRepo, EFProgressRepo progressRepo, EFTestProgressRepo testProgressRepo, EFChestPointsUsesRepo chestPointsUsesRepo, EFPromoCodeRepo promoCodeRepo, EFOrderRepo orderRepo, EFEmailRepo emailRepo, EFTopicPointsRepo topicPointsRepo)
        {
            if (model.IsMonthSubscription == null && model.IsYearSubscription == null && model.IsGift == null)
            {
                return new CreateOrder_Result("Není zadán typ služby");
            }

            var typeCount = 0;
            if (model.IsMonthSubscription == true) typeCount++;
            if (model.IsYearSubscription == true) typeCount++;
            if (model.IsGift == true) typeCount++;
            if (typeCount > 1)
            {
                return new CreateOrder_Result("Je zadáno více typů služby");
            }

            if (string.IsNullOrWhiteSpace(model.EmailForOrder))
            {
                return new CreateOrder_Result("Není zadán E-mail pro objednávku");
            }

            if (string.IsNullOrWhiteSpace(model.Name)) return new CreateOrder_Result("Není zadáno jméno");
            if (string.IsNullOrWhiteSpace(model.Street)) return new CreateOrder_Result("Není zadaná ulice a číslo popisné");
            if (string.IsNullOrWhiteSpace(model.City)) return new CreateOrder_Result("Není zadáno město");
            if (string.IsNullOrWhiteSpace(model.PostalCode)) return new CreateOrder_Result("Není zadáno PSČ");
            if (string.IsNullOrWhiteSpace(model.Phone)) return new CreateOrder_Result("Není zadán telefon");


            var order = new Order
            {
                ClientName = model.Name,
                Street = model.Street,
                City = model.City,
                PostalCode = model.PostalCode,
                Phone = model.Phone,
                EmailForOrder = model.EmailForOrder?.Trim(),
                EmailForApp = model.EmailForApp?.Trim(),
                IsMonthSubscription = model.IsMonthSubscription,
                IsYearSubscription = model.IsYearSubscription,
                IsGift = model.IsGift,
                ICO = model.ICO,
                DIC = model.DIC,
                IsAutomaticRenew = model.IsAutomaticalyRenewing,
            };

            bool isPreviousChallengeCompleted = false;
            CheckEmail_Result checkEmailRes = null;

            if (model.IsGift != true)
            {
                checkEmailRes = await OrderController.CheckEmail_Shared(model.EmailForApp, userRepo, personalChallengeRepo, progressRepo, testProgressRepo, chestPointsUsesRepo, topicPointsRepo);

                if (checkEmailRes.EmailNotProvided)
                {
                    return new CreateOrder_Result("Není zadán E-mail pro přihlášení do aplikace");
                }

                if (checkEmailRes.IsStillPrepaid)
                {
                    return new CreateOrder_Result($"Účet s E-mailem {model.EmailForApp} má stále aktivní Premium předplatné. Nové předplatné lze zakoupit až po vypršení současného.");
                }

                isPreviousChallengeCompleted = checkEmailRes.PreviousChallengeCompleted;

                // Musi se vytvorit novy uzivatel -> vytvori se pri prijeti platby
                if (checkEmailRes.DoesNotExist)
                {
                    if (string.IsNullOrEmpty(model.Password))
                    {
                        return new CreateOrder_Result("Není zadané heslo pro nový účet");
                    }

                    var passwordErrorMessage = AuthenticationHelper.GetPasswordSecureErrorMessage(model.Password);
                    if (passwordErrorMessage != null)
                    {
                        return new CreateOrder_Result(passwordErrorMessage);
                    }

                    order.PasswordSalt = AuthenticationHelper.GenerateSalt();
                    order.PasswordHash = AuthenticationHelper.HashPassword(model.Password, order.PasswordSalt);
                }

                if (model.IsMonthSubscription == true)
                {
                    if (model.IsAutomaticalyRenewing == false)
                    {
                        order.IsInitRecurring = true; // Jsem rucne vytvorena objednavka z formulare -> jsem prvni v rade, moje ID bude nasledne pouzivano jako zaklad pro opakovane platby
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(model.InitRecuringId))
                        {
                            throw new Exception("Init recuring ID je prázdné");
                        }
                        order.InitRecurringId = model.InitRecuringId; // Na tuto platbu chci navazat, aby se platba provedla na pozadi bez interakce uzivatele
                    }
                }


            }


            PromoCode promoCode = null;
            if (!string.IsNullOrWhiteSpace(model.PromoCode))
            {
                var promoCodeUpper = model.PromoCode.ToUpper();
                promoCode = await promoCodeRepo.All.FirstOrDefaultAsync(x => x.Code == promoCodeUpper);

                if (promoCode == null)
                {
                    return new CreateOrder_Result("Neznámý slevový kód");
                }

                if (promoCode.IsActivated)
                {
                    return new CreateOrder_Result("Slevový kód je již použitý");
                }

                if (promoCode.ExpirationDate < DateTime.Now)
                {
                    return new CreateOrder_Result("Platnost slevového kódu vypršela");
                }

                if (promoCode.IsForMonth == false && order.IsMonthSubscription == true)
                {
                    return new CreateOrder_Result("Tento slevový kód nelze použít pro měsíční předplatné");
                }

                if (promoCode.IsForYear == false && order.IsYearSubscription == true)
                {
                    return new CreateOrder_Result("Tento slevový kód nelze použít pro roční předplatné");
                }

                if (promoCode.IsForGift == false && order.IsGift == true)
                {
                    return new CreateOrder_Result("Tento slevový kód nelze použít pro dárek");
                }


                promoCode.IsActivated = true;
                promoCode.ActivatedForUserName = order.EmailForApp;
                promoCode.ActivationDate = DateTime.Now;

                order.PercentageSale = promoCode.PercentageSale;
            }


            // GIFT CODE
            if (model.IsGift == true)
            {
                const string chars = "ABCDEFGHJKLMNPRSTUVWXYZ23456789";

                string giftCode = null;

                var first = new string(Enumerable.Repeat(chars, 3)
                                                        .Select(s => s[random.Next(s.Length)]).ToArray());
                var second = new string(Enumerable.Repeat(chars, 3)
                                                        .Select(s => s[random.Next(s.Length)]).ToArray());
                var third = new string(Enumerable.Repeat(chars, 3)
                                                        .Select(s => s[random.Next(s.Length)]).ToArray());
                var fourth = new string(Enumerable.Repeat(chars, 3)
                                                        .Select(s => s[random.Next(s.Length)]).ToArray());

                giftCode = $"{first}-{second}-{third}-{fourth}";


                order.GiftCode = giftCode;
            }
            // END GIFT CODE


            using (var httpClient = new HttpClient())
            {
                List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();
                parameters.Add(new KeyValuePair<string, string>("merchant", "455678"));

                parameters.Add(new KeyValuePair<string, string>("curr", "CZK"));

                if (order.IsMonthSubscription == true)
                {
                    var priceBankFormat = monthPrice_Bank;
                    if (promoCode != null)
                    {
                        var price = CalculatePriceAfterSale(promoCode.PercentageSale, true, false, false, isPreviousChallengeCompleted);
                        priceBankFormat = price.ToString() + "00";
                    }
                    else // Neni promo kod, ale musim zkontrolovat, jestli mu nemam dat jenom pulku ceny za splnenou predchozi vyzvu
                    {
                        if (isPreviousChallengeCompleted)
                        {
                            priceBankFormat = monthPriceHalf_Bank;
                        }
                    }

                    parameters.Add(new KeyValuePair<string, string>("price", priceBankFormat));
                    parameters.Add(new KeyValuePair<string, string>("label", "mesicni"));
                    parameters.Add(new KeyValuePair<string, string>("method", "CARD_CZ_CS"));

                    // Rucne zadana platba z formulare -> je prvni
                    if (!model.IsAutomaticalyRenewing)
                    {
                        parameters.Add(new KeyValuePair<string, string>("initRecurring", "true")); ///////////// ZAPNOUT INIT RECURRING
                    }
                    else // Opakovana platba
                    {
                        parameters.Add(new KeyValuePair<string, string>("initRecurringId", model.InitRecuringId)); ///////////// OPAKOVANA PLATBA
                    }
                }
                else if (order.IsYearSubscription == true)
                {

                    var priceBankFormat = yearPrice_Bank;
                    if (promoCode != null)
                    {
                        var price = CalculatePriceAfterSale(promoCode.PercentageSale, false, true, false, false);
                        priceBankFormat = price.ToString() + "00";
                    }

                    parameters.Add(new KeyValuePair<string, string>("price", priceBankFormat));
                    parameters.Add(new KeyValuePair<string, string>("label", "rocni"));
                    parameters.Add(new KeyValuePair<string, string>("method", "ALL"));
                }
                else if (order.IsGift == true)
                {
                    var priceBankFormat = yearPrice_Bank;
                    if (promoCode != null)
                    {
                        var price = CalculatePriceAfterSale(promoCode.PercentageSale, false, true, false, false);
                        priceBankFormat = price.ToString() + "00";
                    }

                    parameters.Add(new KeyValuePair<string, string>("price", priceBankFormat));
                    parameters.Add(new KeyValuePair<string, string>("label", "darek"));
                    parameters.Add(new KeyValuePair<string, string>("method", "ALL"));
                }

                // ZACINAM TRANSAKCI UZ TADY, ABYCH VEDEL IDcko Zakazku
                using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                {
                    orderRepo.Add(order);
                    await orderRepo.SaveAll(); // Dostanu ID nove vytravene zakazky (Order.Id), ktere potrebuju poslat do ComGate

                    parameters.Add(new KeyValuePair<string, string>("refId", order.Id.ToString()));
                    parameters.Add(new KeyValuePair<string, string>("prepareOnly", "true"));
                    parameters.Add(new KeyValuePair<string, string>("secret", comGateSecret));

                    parameters.Add(new KeyValuePair<string, string>("email", order.EmailForOrder));

                    var isDebug = Environment.GetEnvironmentVariable("AV_IsDebug");
                    if (!string.IsNullOrWhiteSpace(isDebug))
                    {
                        //parameters.Add(new KeyValuePair<string, string>("test", "true"));
                    }



                    var content = new FormUrlEncodedContent(parameters);

                    //httpClient.BaseAddress = new Uri("https://payments.comgate.cz/v1.0/create");
                    httpClient.BaseAddress = new Uri("https://payments.comgate.cz/v1.0/");

                    string methodUrl;

                    if (model.IsAutomaticalyRenewing)
                    {
                        methodUrl = "recurring";
                    }
                    else
                    {
                        methodUrl = "create";
                    }

                    var response = await httpClient.PostAsync(methodUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var parsedResponse = HttpUtility.ParseQueryString(responseContent);
                        var code = parsedResponse["code"];
                        var message = parsedResponse["message"];
                        var transId = parsedResponse["transId"];
                        var redirect = parsedResponse["redirect"];

                        if (code != "0")
                        {
                            throw new Exception($"Nastala chyba při zakládání objednávky. [Code: {code}, Message: {message}, TransId: {transId}]");
                        }

                        order.ComGateTransId = transId;
                        order.ComGateRedirectUrl = redirect;

                        // Odeslu email
                        var emailBody = EmailHelper.GenerateEmail(
                            "Objednávka přijata",
                            $"Děkujeme za Vaši objednávku." +
                            $"<br />" +
                            $"Platbu můžete zkontrolovat <a href='https://payments.comgate.cz/{order.ComGateTransId}'>zde</a>." +
                            $"<br />" +
                            $"<br />" +
                            $"Pokud potřebujete s něčím poradit, odpovězte na tento e-mail." +
                            $"<br />" +
                            $"<br />" +
                            $"Přejeme Vám mnoho úspěchů v našem kurzu. ❤"
                            );
                        var email = new Email(order.EmailForOrder, "Objednávka přijata", emailBody);
                        emailRepo.Add(email);


                        //orderRepo.Add(order);

                        await userRepo.SaveAll(); // Je to vsechno na jednom contextu, je jedno nad jakym repozitarem zavolam SaveAll


                        // Ulozim k uzivateli, na zaplaceni jake objednavky ceka
                        if (checkEmailRes != null && checkEmailRes.User != null)
                        {
                            checkEmailRes.User.PendingRenewOrderId = order.Id;
                        }


                        // Do Promo kodu ulozim k jake objednavce byl prirazen
                        if (promoCode != null)
                        {
                            promoCode.ActivatedByOrderId = order.Id;
                        }
                        await userRepo.SaveAll(); // Je to vsechno na jednom contextu, je jedno nad jakym repozitarem zavolam SaveAll

                        await transaction.CommitAsync();


                        //var order = new Order
                        //{
                        //    ClientName = model.Name,
                        //    Street = model.Street,
                        //    City = model.City,
                        //    PostalCode = model.PostalCode,
                        //    Phone = model.Phone,
                        //    EmailForOrder = model.EmailForOrder,
                        //    EmailForApp = model.EmailForApp,
                        //    IsMonthSubscription = model.IsMonthSubscription,
                        //    IsYearSubscription = model.IsYearSubscription,
                        //    IsGift = model.IsGift,
                        //};

                        return new CreateOrder_Result { Ok = true, Redirect = redirect };
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        throw new Exception("Nastala neučekávaná chyba při zakládání objednávky.");
                    }
                }
            }
        }



        public static decimal CalculatePriceAfterSale(decimal percentageSale, bool isMonth, bool isYear, bool isGift, bool isMontPriceHalf)
        {
            if (percentageSale > 100 && percentageSale <= 0)
            {
                throw new Exception("Sleva musí být větší než 0 a menší než 100%");
            }

            var ratio = 1 - (percentageSale / 100);

            if (isMonth)
            {
                if (isMontPriceHalf)
                {
                    return Math.Floor(monthPriceHalf_Decimal * ratio);
                }
                else
                {
                    return Math.Floor(monthPrice_Decimal * ratio);
                }
            }
            else if (isYear || isGift)
            {
                return Math.Floor(yearPrice_Decimal * ratio);
            }
            else
            {
                throw new Exception("Do výpočtu ceny nebyl předán typ předplatného");
            }
        }

    }
}
