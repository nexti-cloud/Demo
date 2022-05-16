using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Data
{
    public class EFTestProgressRepo : BaseRepo<TestProgress>
    {
        public EFTestProgressRepo(DbContext context, SaveParameters saveParameters) : base(context, saveParameters)
        {
        }

        public async Task<List<TestProgress>> GetTestProgresses(int userId, int? lessonOrder = null, int? minPercentage = null, bool? anyPoints = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var q = All.Where(x => x.UserId == userId);

            if (lessonOrder != null)
                q = q.Where(x => x.LessonOrder == lessonOrder);

            if (minPercentage != null)
                q = q.Where(x => x.Percentage >= minPercentage);

            if (anyPoints == true)
            {
                q = q.Where(x => x.Points > 0);
            }

            if (anyPoints == false)
            {
                q = q.Where(x => x.Points == 0);
            }

            if (startDate != null)
                q = q.Where(x => x.Created >= startDate);

            if (endDate != null)
                q = q.Where(x => x.Created < endDate);

            var testProgresses = await q.ToListAsync();

            return testProgresses;
        }

        public async Task<List<TestProgress>> GetTestProgresses_Day(int userId, DateTime date)
        {
            var today = date.Date;
            var tomorrow = today.AddDays(1);

            var testProgresses = await All.Where(x => x.UserId == userId && x.Created >= today && x.Created <= tomorrow).ToListAsync();
            return testProgresses;
        }

        public async Task<List<TestProgress>> GetTestProgresses_Month(int userId, DateTime date)
        {
            var today = date.Date;

            var thisMonth_firstDay = new DateTime(today.Year, today.Month, 1);
            var nextMonth_firstDay = thisMonth_firstDay.AddMonths(1);

            var testProgresses = await All.Where(x => x.UserId == userId && x.Created >= thisMonth_firstDay && x.Created <= nextMonth_firstDay).ToListAsync();
            return testProgresses;
        }

        public async Task<bool> IsAnyTestProgressBeforeThisDate_WARNING_MushHavePoints(int userId, DateTime date)
        {
            return await All.AnyAsync(x => x.UserId == userId && x.Created < date && x.Points > 0);
        }

        public async Task<bool> IsAnyTestProgressWithPoints(int userId, int lessonOrder, int testOrder)
        {

            return await All.AnyAsync(x => x.UserId == userId && x.LessonOrder == lessonOrder && x.TestOrder == testOrder && x.Points > 0);
        }
    }
}
