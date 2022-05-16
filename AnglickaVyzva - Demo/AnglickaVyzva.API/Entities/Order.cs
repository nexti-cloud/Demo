using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class Order: BaseEntity
    {
        public string ClientName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Phone { get; set; }
        public string EmailForOrder { get; set; }
        public string EmailForApp { get; set; }

        public string ICO { get; set; }
        public string DIC { get; set; }

        public bool? IsMonthSubscription { get; set; }
        public bool? IsYearSubscription { get; set; }
        public bool? IsGift { get; set; }

        /// <summary>
        /// Je tato objednavka prvni v serii opakujicich se objednavek?
        /// </summary>
        public bool? IsInitRecurring { get; set; }
        /// <summary>
        /// Na zaklade jake inicializacni platby je tato platba vytvorena?
        /// </summary>
        public string InitRecurringId { get; set; }
        /// <summary>
        /// Byla objednavka vytvorena automaticky po konci mesicniho obdobi?
        /// </summary>
        public bool? IsAutomaticRenew { get; set; }

        /// <summary>
        /// Heslo k nove vytvarenemu uctu. (Pokud se plati kurz pro uzivatele, ktery jeste neni registrovany)
        /// </summary>
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }

        /// <summary>
        /// IDcko platby v ComGate svete
        /// </summary>
        public string ComGateTransId { get; set; }
        public string ComGateRedirectUrl { get; set; }
        public string ComGateStatus { get; set; }
        public string ComGateEmail { get; set; }
        public string ComGateFee { get; set; }
        public string ComGateVs { get; set; }
        public string ComGateMethod { get; set; }
        public string ComGateAccount { get; set; }
        public string ComGatePayerName { get; set; }
        public string ComGatePayerAcc { get; set; }
        public string ComGatePayerId { get; set; }
        public string ComGatePhone { get; set; }

        public string GiftCode { get; set; }
        public string GiftActivatedByEmail { get; set; }
        public DateTime? GiftActivatedDate { get; set; }

        /// <summary>
        /// Sleva z Promo kodu
        /// </summary>
        public decimal PercentageSale { get; set; }

        public bool IsPaid { get; set; }
    }
}
