using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.DTOs.Progress
{
    public class ProgressResponseDto
    {
        public List<List<DailyProgressDto>> Weeks { get; set; }
        public int BeforeDayPoints { get; set; }
        public int ActualDayPoints { get; set; }
        public int AddedPoints { get; set; }

        public int BeforePointsInChest { get; set; }
        public int ActualPointsInChest { get; set; }
        public int AddedPointsToChest { get; set; }

        public int BeforeMonthPoints { get; set; }
        public int ActualMonthPoints { get; set; }
        public int DayGoal { get; set; }
        public int MonthGoal { get; set; }
        public int MonthThreshold { get; set; }
        public string MonthThresholdText { get; set; }

        public string StartDateStr { get; set; }
        public string EndDateStr { get; set; }

        public int AllChallengesCount { get; set; }
    }
}
