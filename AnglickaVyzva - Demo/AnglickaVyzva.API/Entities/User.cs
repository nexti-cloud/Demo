using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class User : BaseEntity
    {
        public override string ToString()
        {
            return $"{UserName} ({(IsPremium ? "premium" : "trial")})";
        }

        /// <summary>
        /// Email pro prihlaseni pomoci Emailu a Hesla. Nikde jinde se nepouziva
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Pouze pro metodu prihlaseni pomoci Emailu a hesla
        /// </summary>
        public bool IsUserNameVerified { get; set; }

        public string Firstname { get; set; }

        public string Lastname { get; set; }

        public string Note { get; set; }

        /// <summary>
        /// Kontaktni email - V tomto projektu se nepouziva. Je to CopyPaste ze Sousedu. V Sousedech se uzivateli overuje jeho kontaktni email. Tady to neni naprogramovane.
        /// Pri registraci pomoci Emailu a Hesla se do nej automaticky zkopiruje hodnota z UserName
        /// </summary>
        public string Email { get; set; }

        public bool IsEmailVerified { get; set; }

        public bool IsPhoneVerified { get; set; }

        public string LoginMethod { get; set; }

        public string GoogleUserId { get; set; }
        public string GoogleEmail { get; set; }
        public string GoogleFirstname { get; set; }
        public string GoogleLastname { get; set; }
        public string GoogleImage { get; set; }

        public string FacebookUserId { get; set; }
        public string FacebookEmail { get; set; }
        public string FacebookFirstname { get; set; }
        public string FacebookLastname { get; set; }
        public string FacebookImage { get; set; }

        public string AppleUserId { get; set; }
        public string AppleEmail { get; set; }

        public string PasswordHash { get; set; }
        public string Salt { get; set; }


        public DateTime? LogOutAllDevicesDate { get; set; }

        public bool IsPremium { get; set; }
        public int SparePoints { get; set; }

        public bool IsTemporary { get; set; }
        public string TemporaryUserName { get; set; }

        /// <summary>
        /// Do kdy ma zaplaceno?
        /// </summary>
        public DateTime? PrepaidUntil { get; set; }
        public string SubscriptionType { get; set; }

        public IEnumerable<Progress> Progresses { get; set; }
        public IEnumerable<TestProgress> TestProgresses { get; set; }
        /// <summary>
        /// Kdy a kolik pouzil bodu z truhlicky
        /// </summary>
        public IEnumerable<ChestPointsUse> ChestPointUses { get; set; }

        public bool? DoNotRenewSubscription { get; set; }

        /// <summary>
        /// Na zaklade jake platby ma ted preplatne?
        /// </summary>
        [ForeignKey("ActiveOrder")]
        public int? ActiveOrderId { get; set; }
        public Order ActiveOrder { get; set; }

        /// <summary>
        /// Pro mesicni predplatitele - po pulnoci ihned po vyprseni predplatneho vytvorim
        /// </summary>
        public int? PendingRenewOrderId { get; set; }
        /// <summary>
        /// Jaka chyba se vyskytla pri vytvareni nove mesicni platby
        /// </summary>
        public string RenewOrderError { get; set; }

        /// <summary>
        /// Ziska email, pod kterym se uzivatel prihlasuje - zkusi vsechny metody - Google, Facebook, Apple, Email a heslo
        /// </summary>
        /// <returns></returns>
        public string GetLoginEmail()
        {
            string loginEmail = null;
            switch (LoginMethod)
            {
                case LoginMethods.Google:
                    loginEmail = GoogleEmail;
                    break;

                case LoginMethods.Facebook:
                    loginEmail = FacebookEmail;
                    break;

                case LoginMethods.Apple:
                    loginEmail = AppleEmail;
                    break;

                case LoginMethods.Email:
                    loginEmail = UserName;
                    break;
            }

            return loginEmail;
        }

        public bool IsCompletelyRegistered()
        {
            if (GetCompletelyRegisteredError() == null)
            {
                return true;
            }

            return false;
        }

        public string GetCompletelyRegisteredError()
        {
            if (string.IsNullOrWhiteSpace(LoginMethod))
            {
                return "Není vybraná žádná přihlašovací metoda.";
            }

            if (LoginMethod == LoginMethods.Email)
            {
                if (!IsUserNameVerified)
                {
                    return "Přihlašovací email není ověřený.";
                }
            }

            //if (!IsPhoneVerified)
            //{
            //    return "Telefonní číslo není ověřené.";
            //}

            if (!IsEmailVerified)
            {
                return "Kontaktní email není ověřený. Prosím, vyplňte jej a nechte si zaslat ověřovací kód.";
            }

            //if (string.IsNullOrWhiteSpace(Firstname))
            //{
            //    return "Jméno není vyplněné.";
            //}

            //if (string.IsNullOrWhiteSpace(Lastname))
            //{
            //    return "Příjmení není vyplněné";
            //}

            return null;
        }

        [NotMapped]
        public class LoginMethods
        {
            public const string Email = "email";
            public const string Google = "google";
            public const string Facebook = "facebook";
            public const string Apple = "apple";
        }
    }
}
