using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.DTOs
{
    public class SubexerciseProgressDto
    {
        public int ExerciseOrder { get; set; }
        public int SubexerciseOrder { get; set; }
        public double Points { get; set; }
        public bool IsOpen { get; set; }
        public bool IsDone { get; set; }
        public bool? DoThisRightNow { get; set; }
    }
}
