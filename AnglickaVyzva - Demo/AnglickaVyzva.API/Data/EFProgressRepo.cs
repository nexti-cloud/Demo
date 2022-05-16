using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Data
{
    public class EFProgressRepo : BaseRepo<Progress>
    {
        public EFProgressRepo(DbContext context, SaveParameters saveParameters) : base(context, saveParameters)
        {
        }

        //public async Task<List<Progress>> GetProgressesInSection(int userId, int lessonOrder, int sectionOrder)
        //{
        //    var progresses = await All.Where(x => 
        //        x.UserId == userId && 
        //        x.LessonOrder == lessonOrder && 
        //        x.SectionOrder == sectionOrder
        //        ).ToListAsync();
        //    return progresses;
        //}

        public async Task<List<Progress>> GetProgresses(int userId, int? lessonOrder = null, int? sectionOrder = null, int? exerciseOrder = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var q = All.Where(x => x.UserId == userId);

            if (lessonOrder != null)
                q = q.Where(x => x.LessonOrder == lessonOrder);

            if (sectionOrder != null)
                q = q.Where(x => x.SectionOrder == sectionOrder);

            if (exerciseOrder != null)
                q = q.Where(x => x.ExerciseOrder == exerciseOrder);

            if (startDate != null)
                q = q.Where(x => x.Created >= startDate);

            if (endDate != null)
                q = q.Where(x => x.Created < endDate);

            var progresses = await q.ToListAsync();
            return progresses;
        }

        public async Task<List<Progress>> GetProgresses_Day(int userId, DateTime date)
        {
            var today = date.Date;
            var tomorrow = today.AddDays(1);

            var progresses = await All.Where(x => x.UserId == userId && x.Created >= today && x.Created <= tomorrow).ToListAsync();
            return progresses;
        }

        public async Task<List<Progress>> GetProgresses_Month(int userId, DateTime date)
        {
            var today = date.Date;

            var thisMonth_firstDay = new DateTime(today.Year, today.Month, 1);
            var nextMonth_firstDay = thisMonth_firstDay.AddMonths(1);

            var progresses = await All.Where(x => x.UserId == userId && x.Created >= thisMonth_firstDay && x.Created <= nextMonth_firstDay).ToListAsync();
            return progresses;
        }

        public async Task<bool> ProgressExist(int userId, int lessonOrder, int sectionOrder, int exerciseOrder, int? exerciseSuborder)
        {
            return await All.AnyAsync(x =>
                x.UserId == userId &&
                x.LessonOrder == lessonOrder &&
                x.SectionOrder == sectionOrder &&
                x.ExerciseOrder == exerciseOrder &&
                x.ExerciseSuborder == exerciseSuborder);
        }

        public async Task<bool> IsNormalExerciseDone(int userId, int lessonOrder, int sectionOrder, int exerciseOrder)
        {
            return await All.AnyAsync(x => x.UserId == userId && x.LessonOrder == lessonOrder && x.SectionOrder == sectionOrder && x.ExerciseOrder == exerciseOrder);
        }

        public async Task<bool> IsVocabularyExerciseDone(int userId, int lessonOrder, int sectionOrder, int exerciseOrder)
        {
            // Kontroluju slovicka -> musi byt splnenych 10 podcviceni
            var count = await All.CountAsync(x => x.UserId == userId && x.LessonOrder == lessonOrder && x.SectionOrder == sectionOrder && x.ExerciseOrder == exerciseOrder);
            if (count == 10)
                return true;
            return false;
        }

        public async Task<bool> IsAnyProgressBeforeThisDate(int userId, DateTime date)
        {
            return await All.AnyAsync(x => x.UserId == userId && x.Created < date);
        }

    }
}
