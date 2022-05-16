using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.DTOs.Admin
{
    public class Admin_User_ListDto
    {
        public int Id { get; set; }
        public string LoginEmail { get; set; }
        public string LoginMethod { get; set; }
        public bool IsTemporary { get; set; }
        public bool IsUserNameVerified { get; set; }
        public DateTime CreatedDate { get; set; }

        public bool IsPremium { get; set; }
        public bool? DoNotRenewSubscription { get; set; }
        public string SubscriptionType { get; set; }
        public DateTime? PrepaidUntil { get; set; }

        public string Note { get; set; }

        public List<int> PointsOfDays { get; set; }

        public int PointsToday { get; set; }
        public int PointsYesterday { get; set; }
        public int PointsTwoDaysAgo { get; set; }

        public int PointsLast7Days { get; set; }
        public int PointsLast30Days { get; set; }


        // Kam se az dostal
        public int? LessonOrder { get; set; }
        public int? SectionOrder { get; set; }
        public int? ExerciseOrder { get; set; }
        public int? TestLessonOrder { get; set; }
        public int? TestOrder { get; set; }
        
    }
}
