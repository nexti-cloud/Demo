using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class Feedback : BaseEntity
    {
        public string Dump { get; set; }
        public string Message { get; set; }
        public string ImageName { get; set; }
    }
}
