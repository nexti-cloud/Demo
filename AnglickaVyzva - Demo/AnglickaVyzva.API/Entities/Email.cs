using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class Email : BaseEntity
    {
        public string ToAddress { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime? SendTime { get; set; }

        public Email() { }
        public Email(string toAddress, string subject, string body)
        {
            ToAddress = toAddress;
            Subject = subject;
            Body = body;
        }
    }
}
