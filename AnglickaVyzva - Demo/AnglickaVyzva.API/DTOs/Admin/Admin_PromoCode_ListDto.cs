using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.DTOs.Admin
{
    public class Admin_PromoCode_ListDto
    {
        public int Id { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string ExpirationDateStr { get
            {
                return ExpirationDate.ToString("dd. MM. yyyy HH:mm");
            }
        }
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

        public DateTime CreatedDate { get; set; }
        public string CreatedDateStr
        {
            get
            {
                return CreatedDate.ToString("dd. MM. yyyy HH:mm");
            }
        }

        public bool IsExpired
        {
            get
            {
                return DateTime.Now > ExpirationDate;
            }
        }
    }
}
