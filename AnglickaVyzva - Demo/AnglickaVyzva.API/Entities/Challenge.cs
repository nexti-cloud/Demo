using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class Challenge : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int DailyGoal { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string EnterCode { get; set; }

        public bool? IsFreeTrial { get; set; }
    }
}
