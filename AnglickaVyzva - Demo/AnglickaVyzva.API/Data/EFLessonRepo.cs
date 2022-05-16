using AnglickaVyzva.API.Helpers;
using AnglickaVyzva.API.Models;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Data
{
    public class EFLessonRepo
    {
        private IWebHostEnvironment _env;

        

        public EFLessonRepo(IWebHostEnvironment env)
        {
            _env = env;
        }

        private static List<Lesson> FreeLessons;
        private static List<Lesson> PremiumLessons;
        private static SemaphoreSlim semaphoreCache = new SemaphoreSlim(1, 1);

        private async Task<List<Lesson>> GetCachedLessons_FromFile(bool isPremium, bool forceReload = false)
        {
            try
            {
                await semaphoreCache.WaitAsync();

                if (isPremium)
                {
                    if (PremiumLessons == null || forceReload)
                    {
                        var folderPath = System.IO.Path.Combine(_env.ContentRootPath, "App_Data/Lekce");
                        PremiumLessons = ExcelHelper.GetLessons(folderPath, true);
                    }
                    return PremiumLessons.ToList();
                }
                else
                {
                    if (FreeLessons == null || forceReload)
                    {
                        var folderPath = System.IO.Path.Combine(_env.ContentRootPath, "App_Data/Lekce");
                        FreeLessons = ExcelHelper.GetLessons(folderPath, false);
                    }
                    return FreeLessons.ToList();
                }
            }
            finally
            {
                semaphoreCache.Release();
            }
        }

        public async Task ReloadLessonCache()
        {
                await GetCachedLessons_FromFile(true, forceReload: true);
                await GetCachedLessons_FromFile(false, forceReload: true);
        }

        public async Task<Lesson> GetCachedLesson(int lessonOrder, bool isPremium)
        {
            var lessons = await GetCachedLessons_FromFile(isPremium);
            return lessons.First(x => x.Order == lessonOrder);
        }

        public async Task<List<Lesson>> GetCachedLessons(bool isPremium)
        {
            var lessons = await GetCachedLessons_FromFile(isPremium);
            return lessons;
        }

        public async Task<Section> GetCachedSection(int lessonOrder, int sectionOrder, bool isPremium)
        {
            var lesson = await GetCachedLesson(lessonOrder, isPremium);
            var section = lesson.Sections.First(x => x.Order == sectionOrder);
            return section;
        }

        public async Task<IExercise> GetCachedExercise(int lessonOrder, int sectionOrder, int exerciseOrder, bool isPremium)
        {
            var lesson = await GetCachedLesson(lessonOrder, isPremium);
            var section = lesson.Sections.First(x => x.Order == sectionOrder);
            var exercise = section.Exercises.First(x => x.Order == exerciseOrder);
            return exercise;
        }
    }
}
