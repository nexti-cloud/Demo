using AnglickaVyzva.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.DTOs
{
    public class TestDto
    {
        public int Order { get; set; }

        public string Title { get; set; }
        public List<Test.ItemTest> ItemList { get; set; } = new List<Test.ItemTest>();

        public int PercentageDone { get; set; }
        public bool IsDone { get; set; }
        public bool IsOpen { get; set; }

        public string DataFolderPath { get; set; }

        //public const double PercentageThreshold = Domain.Entities.Test.PercentageThreshold;
    }
}
