using AnglickaVyzva.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.DTOs
{
    public class SectionDto
    {
        public override string ToString()
        {
            return (string.IsNullOrWhiteSpace(NameCZ) ? base.ToString() : NameCZ) + "[" + DataFolderPath + "]";
        }

        public int Order { get; set; } // Cislo/poradi
        public string NameCZ { get; set; }
        public string NameEN { get; set; }

        public string DataFolderPath { get; set; }
        public List<IExercise> Exercises { get; set; }
        public string Type { get; set; }
        public bool IsLock { get; set; }

        //// INFORMACE ZVLAST PRO KAZDEHO UZIVATELE - dopocitavaji se za behu pro kazdeho uzivatele zvlast
        public bool IsOpen { get; set; }
        public bool IsDone { get; set; }
        public double PercentageDone { get; set; }
        public bool? DoThisRightNow { get; set; }
        //// END INFORMACE ZVLAST PRO KAZDEHO UZIVATELE
    }
}
