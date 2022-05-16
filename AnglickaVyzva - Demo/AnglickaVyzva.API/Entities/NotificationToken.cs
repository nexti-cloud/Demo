using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class NotificationToken : BaseEntity
    {
        public string Token { get; set; }
        public int DeviceType { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        public static class DeviceTypes
        {
            public const int Android = 1;
            public const int IOs = 2;
        }
    }
}
