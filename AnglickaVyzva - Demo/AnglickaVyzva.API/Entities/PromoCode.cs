using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class PromoCode : BaseEntity
    {
        public DateTime ExpirationDate { get; set; }
        public decimal PercentageSale { get; set; }
        public string Code { get; set; }
        public string Note { get; set; }

        public bool IsActivated { get; set; }
        public int ActivatedByOrderId { get; set; }
        public string ActivatedForUserName { get; set; }
        public DateTime ActivationDate { get; set; }

        public bool IsForMonth { get; set; }
        public bool IsForYear { get; set; }
        public bool IsForGift { get; set; }
    }
}
