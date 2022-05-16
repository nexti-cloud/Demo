using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class TestProgress : BaseEntity
    {
        public int LessonOrder { get; set; }
        public int TestOrder { get; set; }
        public int Percentage { get; set; }
        public DateTime Created { get; set; }
        public int Points { get; set; } // Body za test dostane pouze poprve, kdyz ho splni na 80%

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        public const int ThresholdPoints = 80;
    }
}
