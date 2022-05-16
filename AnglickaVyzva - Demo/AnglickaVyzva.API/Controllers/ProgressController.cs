using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.DTOs;
using AnglickaVyzva.API.DTOs.Progress;
using AnglickaVyzva.API.Entities;
using AnglickaVyzva.API.Helpers;
using AnglickaVyzva.API.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AnglickaVyzva.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProgressController : BaseController
    {
        public ProgressController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {

        }

        //[AllowAnonymous]
        //[HttpGet("fakeProgress")]
        //public async Task<IActionResult> FakeProgress()
        //{
        //    var userId = 74; // slunicko@anglickavyzva.cz

        //    var lessons = LessonRepo.GetCachedLessons(false);

        //    foreach (var lesson in lessons)
        //    {
        //        foreach (var section in lesson.Sections)
        //        {

        //            if (lesson.Order == 2 && section.Order == 10)
        //            {
        //                goto Finish;
        //            }

        //            if (section.IsLock)
        //                continue;

        //            foreach (var exercise in section.Exercises)
        //            {
        //                if (exercise.IsLock)
        //                    continue;

        //                if (exercise.Type != IExercise.Types.ExerciseVocabulary)
        //                {
        //                    var progress = new Progress
        //                    {
        //                        UserId = userId,
        //                        LessonOrder = lesson.Order,
        //                        SectionOrder = section.Order,
        //                        ExerciseOrder = exercise.Order,
        //                        Points = exercise.Points,
        //                        Created = DateTime.Now,
        //                    };

        //                    ProgressRepo.Add(progress);
        //                }
        //                else
        //                {
        //                    for (var i = 1; i <= 10; i++)
        //                    {
        //                        var progress = new Progress
        //                        {
        //                            UserId = userId,
        //                            LessonOrder = lesson.Order,
        //                            SectionOrder = section.Order,
        //                            ExerciseOrder = exercise.Order,
        //                            ExerciseSuborder = i,
        //                            Points = 10,
        //                            Created = DateTime.Now,
        //                        };

        //                        ProgressRepo.Add(progress);
        //                    }
        //                }
        //            }
        //        }
        //    }

        //Finish:

        //    await SaveAll();


        //    return Ok();
        //}

        //[AllowAnonymous]
        //[HttpGet("fakeTest")]
        //public async Task<IActionResult> FakeTest()
        //{
        //    var userId = 74; // slunicko@anglickavyzva.cz

        //    var lessons = LessonRepo.GetCachedLessons(false);

        //    foreach (var lesson in lessons)
        //    {
        //        if(lesson.Order == 3)
        //        {
        //            break;
        //        }

        //        foreach(var test in lesson.Tests)
        //        {
        //            var testProgress = new TestProgress
        //            {
        //                LessonOrder = lesson.Order,
        //                Created = DateTime.Now,
        //                Percentage = 85,
        //                Points = 25,
        //                TestOrder = test.Order,
        //                UserId = userId,
        //            };

        //            TestProgressRepo.Add(testProgress);
        //        }
        //    }

        //    await SaveAll();


        //    return Ok();
        //}

        [HttpGet("getDailyAndMonthlyPoints/{dateParam:DateTime?}")]
        public async Task<IActionResult> GetDailyAndMonthlyPoints(DateTime? dateParam = null)
        {
            var user = await GetLoggedUser();

            var now = DateTime.Now;
            if (dateParam != null)
            {
                now = (DateTime)dateParam;
            }

            var activeChallenge = await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);

            var helpResult = await Helper_GetProgressesAndPoints(activeChallenge);

            return Ok(new
            {
                dayGoal = 100,
                dayPoints = helpResult.dayPoints,
                monthGoal = helpResult.challengeGoal,
                monthPoints = helpResult.challengePoints,
                monthThreshold = helpResult.dayTestProgresses,
                pointsInChest = helpResult.pointsInChest
            });
        }


        [HttpGet("getDailyAndMonthlyProgress/{takeOldChallenge?}")]
        public async Task<IActionResult> GetDailyAndMonthlyProgress(int? takeOldChallenge = null)
        {
            PersonalChallenge challenge;

            // Ziskavam aktualni vyzvu
            if (takeOldChallenge == null)
            {
                challenge = await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);
            }
            else
            {
                challenge = PersonalChallengeRepo.All
                    .Where(x => x.UserId == UserId)
                    .OrderByDescending(x => x.Id)
                    .Skip((int)takeOldChallenge)
                    .FirstOrDefault();
            }


            

            var helpResult = await Helper_GetProgressesAndPoints(challenge);

            var personalChallengesCount = await PersonalChallengeRepo.All.Where(x => x.UserId == UserId).CountAsync();



            return Ok(new ProgressResponseDto
            {
                Weeks = GenerateDailyProgresses(challenge, helpResult.challengeProgresses, helpResult.challengeTestProgresses, helpResult.challengeChestPointsUses, helpResult.topicPointsList, helpResult.dayGoal),
                DayGoal = helpResult.dayGoal,
                ActualDayPoints = helpResult.dayPoints,
                MonthThreshold = helpResult.todaysThreshold,
                MonthThresholdText = Helper_GetThresholdText(helpResult.todaysThreshold, helpResult.challengePoints),
                MonthGoal = helpResult.challengeGoal,
                ActualMonthPoints = helpResult.challengePoints,
                ActualPointsInChest = helpResult.pointsInChest,
                StartDateStr = challenge.StartDate.ToString("d. MMMM", CultureInfo.CreateSpecificCulture("cs")),
                EndDateStr = challenge.EndDate.ToString("d. MMMM", CultureInfo.CreateSpecificCulture("cs")),
                AllChallengesCount = personalChallengesCount,
                //monthName = today.ToString("MMMM", CultureInfo.CreateSpecificCulture("cs")),
            });
        }

        private List<List<DailyProgressDto>> GenerateDailyProgresses(PersonalChallenge challenge, List<Entities.Progress> monthProgresses, List<Entities.TestProgress> monthTestProgresses, List<Entities.ChestPointsUse> monthChestPointsUses, List<Entities.TopicPoints> monthTopicPointsList, int dayGoal)
        {
            //// Vytvorim pro kazdy den ve vyzve postup postup


            var weeks = new List<List<DailyProgressDto>>(); // Seznam tydnu ve vyzve obsahujici seznam dni v tydnu

            var daysToShiftStart = ((int)challenge.StartDate.Date.DayOfWeek) - 1;
            if (daysToShiftStart == -1)
                daysToShiftStart = 6;


            // Tabulka se vykresluje po tydnech, takze musim prvni prochozeny den posunout na pondeli
            var iteratingDay = challenge.StartDate.Date.AddDays(-daysToShiftStart);

            // Posledni prochazeny den posunu na nedeli
            var daysToShiftEnd = (7 - (int)challenge.EndDate.DayOfWeek) % 7;
            var lastDayInTable = challenge.EndDate.AddDays(daysToShiftEnd);

            while (iteratingDay <= lastDayInTable)
            {
                // Je pondeli -> vytvorim novy tyden (Pri prvnim pruchodu je vzdy pondeli)
                if (iteratingDay.DayOfWeek == DayOfWeek.Monday)
                    weeks.Add(new List<DailyProgressDto>());

                var week = weeks.Last();

                var todayProgresses = monthProgresses.Where(x => x.Created >= iteratingDay && x.Created < iteratingDay.AddDays(1)).ToList();
                var todayTestProgresses = monthTestProgresses.Where(x => x.Created >= iteratingDay && x.Created < iteratingDay.AddDays(1)).ToList();
                var todayChestPointsUses = monthChestPointsUses.Where(x => x.Created >= iteratingDay && x.Created < iteratingDay.AddDays(1)).ToList();
                var todayTopicPointsList = monthTopicPointsList.Where(x=>x.Created >= iteratingDay && x.Created < iteratingDay.AddDays(1)).ToList();

                var dailyProgressDto = new DailyProgressDto();

                // Prázdné (výplňové) dny před touto výzvou nebo po ni
                if (iteratingDay < challenge.StartDate || iteratingDay > challenge.EndDate)
                {
                    dailyProgressDto.IsPlaceholder = true;
                }

                // Je tento den v budoucnosti?
                if (iteratingDay > DateTime.Now)
                {
                    // Je tento den az po skonceni vyzvy?
                    if (iteratingDay > challenge.EndDate)
                    {
                        dailyProgressDto.IsPlaceholder = true;
                    }
                    else // Tento den jeste spada do vyzvy
                    {
                        dailyProgressDto.IsFuture = true;
                    }
                }

                // Nastaveni odehraneho dne -> Je tento den splnen uspesne nebo nesplnen a kolik bodu ziskal
                if (iteratingDay >= challenge.StartDate && iteratingDay <= challenge.EndDate && iteratingDay < DateTime.Now)
                {
                    dailyProgressDto.Points = todayProgresses.Sum(x => x.Points);
                    dailyProgressDto.Points += todayTestProgresses.Sum(x => x.Points); // Prictu body za poprve splnene testy v tento den
                    dailyProgressDto.Points += todayChestPointsUses.Sum(x => x.Points);
                    dailyProgressDto.Points += todayTopicPointsList.Sum(x=> x.Points);

                    if (dailyProgressDto.Points >= dayGoal)
                        dailyProgressDto.IsSuccess = true;
                    else
                        dailyProgressDto.IsFail = true;
                }

                // Je tento den dnesek? (Pokud se divam na stare vyzvy, nechci, aby blikal dnesek, ktery uz neni soucasti stare vyzvy)
                if (iteratingDay == DateTime.Now.Date && DateTime.Today <= challenge.EndDate)
                {
                    dailyProgressDto.IsToday = true;
                }


                week.Add(dailyProgressDto);


                // Posunu se o dalsi den
                iteratingDay = iteratingDay.AddDays(1);
            }

            return weeks;
        }

        [HttpPost("reportExerciseProgress/{lessonOrder}/{sectionOrder}/{exerciseOrder}/{exerciseSuborder:int?}")]
        public async Task<IActionResult> ReportExerciseProgress(int lessonOrder, int sectionOrder, int exerciseOrder, int? exerciseSuborder = null)
        {
            var user = await GetLoggedUser();
            var exercise = await LessonRepo.GetCachedExercise(lessonOrder, sectionOrder, exerciseOrder, user.IsPremium);

            Entities.Progress progress = new Entities.Progress
            {
                Created = DateTime.Now,
                LessonOrder = lessonOrder,
                SectionOrder = sectionOrder,
                ExerciseOrder = exerciseOrder,
                ExerciseSuborder = exerciseSuborder,
                Points = exercise.Points,
                UserId = user.Id
            };

            var activeChallenge = await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);

            var helpResult = await Helper_GetProgressesAndPoints(activeChallenge);

            var newAddedPoints = 0;
            var addedPointsToChest = 0;

            // Jestli jeste nema cviceni hotove, tak mu nastavim, ze ted uz jo
            if (!await ProgressRepo.ProgressExist(user.Id, lessonOrder, sectionOrder, exerciseOrder, exerciseSuborder))
            {
                newAddedPoints = progress.Points;
                addedPointsToChest = Helper_CalculateNewPointsToChest(newAddedPoints, helpResult.challengePoints, helpResult.challengeGoal);
                user.SparePoints += addedPointsToChest;

                ProgressRepo.Add(progress);

                if (!await ProgressRepo.SaveAll())
                    throw new Exception("Nastala chyba při ukládání výsledku cvičení.");



            }

            var actualDayPoints = helpResult.dayPoints + newAddedPoints;
            var actualMonthPoints = helpResult.challengePoints + newAddedPoints;
            var beforeDayPoints = helpResult.dayPoints;
            var beforeMonthPoints = helpResult.challengePoints;
            var beforePointsInChest = helpResult.pointsInChest;


            // Byla zmena - prepocitam HELP RESULT, abych zaposical nove pridane body
            if (newAddedPoints != 0)
            {
                helpResult = await Helper_GetProgressesAndPoints(activeChallenge);
            }

            return Ok(new ProgressResponseDto
            {
                Weeks = GenerateDailyProgresses(activeChallenge, helpResult.challengeProgresses, helpResult.challengeTestProgresses, helpResult.challengeChestPointsUses, helpResult.topicPointsList, helpResult.dayGoal),
                BeforeDayPoints = beforeDayPoints,
                ActualDayPoints = actualDayPoints,
                AddedPoints = newAddedPoints,

                BeforePointsInChest = beforePointsInChest,
                ActualPointsInChest = user.SparePoints,
                AddedPointsToChest = addedPointsToChest,

                BeforeMonthPoints = beforeMonthPoints,
                ActualMonthPoints = actualMonthPoints,
                DayGoal = helpResult.dayGoal,
                MonthGoal = helpResult.challengeGoal,
                MonthThreshold = helpResult.todaysThreshold,
                MonthThresholdText = Helper_GetThresholdText(helpResult.todaysThreshold, actualMonthPoints),
            });


        }

        // Za splneni testu dostane 10 bodu (kdyz uz ma vsechny cviceni splnene a presto si udela test, tak nic nedostane).
        // - Test na slovicka se do DB neuklada. Ukladaji se pouze postupu jednotliveho podcviceni, jakoby je udelal jedno po druhem
        // - Protoze mu ale chci dat jenom 10 bodu a ne 10x10, dam prvnimu nesplnenemu cviceni 10 bodu a ostatnim 0
        [HttpPost("reportVocabularyTestProgress/{lessonOrder}/{sectionOrder}/{exerciseOrder}")]
        public async Task<IActionResult> ReportVocabularyTestProgress(int lessonOrder, int sectionOrder, int exerciseOrder)
        {
            var user = await GetLoggedUser();
            var exercise = await LessonRepo.GetCachedExercise(lessonOrder, sectionOrder, exerciseOrder, user.IsPremium);

            var vocabularyProgresses = await ProgressRepo.GetProgresses(user.Id, lessonOrder, sectionOrder, exerciseOrder);

            var activeChallenge = await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);

            var helpResult = await Helper_GetProgressesAndPoints(activeChallenge);

            // Bude bud 10 nebo 0
            // - 10 pokud timto testem nejake cviceni splnil
            // - 0  pokud uz mel vsechna cviceni splnena a jenom si zkusil test
            var newAddedPoints = 0;

            for (int i = 1; i <= 10; i++)
            {
                // Toto podcviceni jiz splnil driv -> preskocim
                if (vocabularyProgresses.Any(x => x.ExerciseSuborder == i))
                {
                    continue;
                }
                else // Toto podcviceni jeste pred testem nesplnil -> nastavim, ze je hotove
                {
                    Entities.Progress prog = new Entities.Progress
                    {
                        Created = DateTime.Now,
                        LessonOrder = lessonOrder,
                        SectionOrder = sectionOrder,
                        ExerciseOrder = exerciseOrder,
                        ExerciseSuborder = i,
                        Points = exercise.Points - newAddedPoints, // Za prvni cviceni dostane 10 bodu, za dalsi uz nic
                        UserId = user.Id
                    };

                    newAddedPoints += prog.Points;

                    ProgressRepo.Add(prog);
                }
            }


            var beforePointsInChest = helpResult.pointsInChest;


            var addedPointsToChest = Helper_CalculateNewPointsToChest(newAddedPoints, helpResult.challengePoints, helpResult.challengeGoal);
            user.SparePoints += addedPointsToChest;

            var actualDayPoints = helpResult.dayPoints + newAddedPoints;
            var actualMonthPoints = helpResult.challengePoints + newAddedPoints;
            var beforeDayPoints = helpResult.dayPoints;
            var beforeMonthPoints = helpResult.challengePoints;

            await ProgressRepo.SaveAll(); // Kdyz se nepricitaji zadne body, tak se nic nezapise to DB => SaveAll Vrati false, protoze pocet ulozenych prvku je 0


            // Byla zmena - prepocitam HELP RESULT, abych zaposical nove pridane body
            if (newAddedPoints != 0)
            {
                helpResult = await Helper_GetProgressesAndPoints(activeChallenge);
            }

            return Ok(new ProgressResponseDto
            {
                Weeks = GenerateDailyProgresses(activeChallenge, helpResult.challengeProgresses, helpResult.challengeTestProgresses, helpResult.challengeChestPointsUses, helpResult.topicPointsList, helpResult.dayGoal),
                BeforeDayPoints = beforeDayPoints,
                ActualDayPoints = actualDayPoints,
                AddedPoints = newAddedPoints,

                BeforePointsInChest = beforePointsInChest,
                ActualPointsInChest = user.SparePoints,
                AddedPointsToChest = addedPointsToChest,

                BeforeMonthPoints = beforeMonthPoints,
                ActualMonthPoints = actualMonthPoints,
                DayGoal = helpResult.dayGoal,
                MonthGoal = helpResult.challengeGoal,
                MonthThreshold = helpResult.todaysThreshold,
                MonthThresholdText = Helper_GetThresholdText(helpResult.todaysThreshold, actualMonthPoints),
            });

            throw new Exception("Nastala chyba při ukládání výsledku testu.");
        }

        [HttpPost("reportTestProgress/{lessonOrder}/{testOrder}/{percentage}")]
        public async Task<IActionResult> ReportTestProgress(int lessonOrder, int testOrder, int percentage)
        {
            var user = await GetLoggedUser();

            var activeChallenge = await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);

            var helpResult = await Helper_GetProgressesAndPoints(activeChallenge);

            Entities.TestProgress prog = new Entities.TestProgress
            {
                Created = DateTime.Now,
                LessonOrder = lessonOrder,
                TestOrder = testOrder,
                Percentage = percentage,
                UserId = user.Id,
            };

            // Pokud tento test tedka splnil poprve, pridam mu za nej body
            if (percentage >= Test.PercentageThreshold)
            {
                if (!await TestProgressRepo.IsAnyTestProgressWithPoints(user.Id, lessonOrder, testOrder))
                {
                    prog.Points = Test.PointsForSuccess;
                }
            }

            var newAddedPoints = prog.Points;
            var addedPointsToChest = Helper_CalculateNewPointsToChest(newAddedPoints, helpResult.challengePoints, helpResult.challengeGoal);

            user.SparePoints += addedPointsToChest;

            TestProgressRepo.Add(prog);
            if (!await TestProgressRepo.SaveAll())
                throw new Exception("Nastala chyba při ukládání výsledku testu.");

            var actualDayPoints = helpResult.dayPoints + newAddedPoints;
            var actualMonthPoints = helpResult.challengePoints + newAddedPoints;
            var beforeDayPoints = helpResult.dayPoints;
            var beforeMonthPoints = helpResult.challengePoints;
            var beforePointsInChest = helpResult.pointsInChest;

            // Byla zmena - prepocitam HELP RESULT, abych zaposical nove pridane body
            if (newAddedPoints != 0)
            {
                helpResult = await Helper_GetProgressesAndPoints(activeChallenge);
            }

            return Ok(new ProgressResponseDto
            {
                Weeks = GenerateDailyProgresses(activeChallenge, helpResult.challengeProgresses, helpResult.challengeTestProgresses, helpResult.challengeChestPointsUses, helpResult.topicPointsList, helpResult.dayGoal),
                BeforeDayPoints = beforeDayPoints,
                ActualDayPoints = actualDayPoints,
                AddedPoints = newAddedPoints,

                BeforePointsInChest = beforePointsInChest,
                ActualPointsInChest = user.SparePoints,
                AddedPointsToChest = addedPointsToChest,

                BeforeMonthPoints = beforeMonthPoints,
                ActualMonthPoints = actualMonthPoints,
                DayGoal = helpResult.dayGoal,
                MonthGoal = helpResult.challengeGoal,
                MonthThreshold = helpResult.todaysThreshold,
                MonthThresholdText = Helper_GetThresholdText(helpResult.todaysThreshold, actualMonthPoints),
            });
        }



        public class TopicItemResult
        {
            public int Id { get; set; }
            public bool? DontKnow { get; set; }
            public bool WasMistake { get; set; }
            public bool WasCorrection { get; set; } // Po upozorneni se opravil
        }
        public class ReportTopicProgress_Model
        {
            public List<TopicItemResult> Items { get; set; }
        }
        [HttpPost("reportTopicProgress")]
        public async Task<IActionResult> ReportTopicProgress(ReportTopicProgress_Model model)
        {
            if (model.Items == null || model.Items.Count == 0)
            {
                return BadRequest("Seznam je prázdný");
            }


            var user = await GetLoggedUser();

            var activeChallenge = await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);

            var helpResult = await Helper_GetProgressesAndPoints(activeChallenge);

            


            var itemsIds = model.Items.Select(x => x.Id).ToList();

            var items_user = await TopicItem_UserRepo.All.Where(x => x.UserId == user.Id && itemsIds.Contains(x.TopicItemId)).ToListAsync();

            // Kolik mu pridam bodu do postupu
            // - Pokud se ucil slovicko pomoci celeho procesu (neznal ho) -> pridam mu 10 bodu
            // - Pokud slovickou pouze napsal a neprochazel celym procesem -> pridam mu 2 body
            // - Pokud u slovicka dal, ze ho neumi -> pridam mu jenom 1 bod (pravdepodobne ho nezkousel znovu a znovu dokud se ho nenaucil)
            var newPoints = 0;

            foreach (var itemResult in model.Items)
            {
                var item_user = items_user.FirstOrDefault(x => x.TopicItemId == itemResult.Id);
                if (item_user == null) // Jeste se toto slovicko neucil -> vytvorim mu vazbu se slovickem
                {
                    item_user = new TopicItem_User
                    {
                        TopicItemId = itemResult.Id,
                        UserId = user.Id,
                        Score = 0,
                    };

                    // Slovicko odpovedel na poprve spravne -> pridam mu trochu score, aby se mu priste neukazovalo mezi slovicky, ktere neznal
                    if(itemResult.WasCorrection != true && itemResult.WasMistake != true)
                    {
                        item_user.Score += 0.1;
                    }

                    TopicItem_UserRepo.Add(item_user);
                }

                if (item_user.DontKnow) // Prosel celym procesem -> 10 bodu (v poslednim cviceni "psani" mohl dat ze ho neumi, ale stejne musel projit vsemi 10 kroky, jinak by nemohl odeslat vysledek)
                {
                    newPoints += 10;
                }
                else if (itemResult.DontKnow != true) // Slovicko jenom napsal (mozna ho zkousel vickrat, nez se ho naucil) -> 2 body
                {
                    if (itemResult.WasMistake)
                    {
                        newPoints += 2; // Musel ho napsat minimalne dvakrat, protoze udelal chybu
                    }
                    else
                    {
                        newPoints += 1; // Napsal ho pouze jednou, protoze chybu neudelal -> nestravil s tim skoro zadny cas
                    }
                }
                else // Rekl ze slovicko neumi -> 1 bod
                {
                    newPoints += 1;
                }

                // Slovicko nezna a chce se ho naucit poradne
                if (itemResult.DontKnow == true)
                {
                    item_user.DontKnow = true;
                }
                else
                {
                    item_user.DontKnow = false; // Musel ho alespon jednou dobre napsat, protoze DontKnow je null

                    if (itemResult.WasMistake == true) // Byla chyba
                    {
                        double decreaseBy = item_user.Score * 0.25;
                        item_user.Score -= decreaseBy;
                    }
                    else if(itemResult.WasCorrection == true) // Opravil se po upozorneni (Mohl to byt preklep, ale nemusel si byt treba uplne jisty)
                    {
                        double decreaseBy = item_user.Score * 0.1;
                        item_user.Score -= decreaseBy;
                    }
                    else // Odpovedel spravne
                    {
                        double increaseBy = (1 - item_user.Score) * 0.5;
                        item_user.Score += increaseBy;
                    }
                }
            }


            var points = new TopicPoints
            {
                Points = newPoints,
                UserId = user.Id,
                Created = DateTime.Now,
            };
            TopicPointsRepo.Add(points);


            var newAddedPoints = newPoints;
            var addedPointsToChest = Helper_CalculateNewPointsToChest(newAddedPoints, helpResult.challengePoints, helpResult.challengeGoal);

            user.SparePoints += addedPointsToChest;

            await SaveAll();

            var actualDayPoints = helpResult.dayPoints + newAddedPoints;
            var actualMonthPoints = helpResult.challengePoints + newAddedPoints;
            var beforeDayPoints = helpResult.dayPoints;
            var beforeMonthPoints = helpResult.challengePoints;
            var beforePointsInChest = helpResult.pointsInChest;

            // Byla zmena - prepocitam HELP RESULT, abych zaposical nove pridane body
            if (newAddedPoints != 0)
            {
                helpResult = await Helper_GetProgressesAndPoints(activeChallenge);
            }

            

            return Ok(new ProgressResponseDto
            {
                Weeks = GenerateDailyProgresses(activeChallenge, helpResult.challengeProgresses, helpResult.challengeTestProgresses, helpResult.challengeChestPointsUses, helpResult.topicPointsList, helpResult.dayGoal),
                BeforeDayPoints = beforeDayPoints,
                ActualDayPoints = actualDayPoints,
                AddedPoints = newAddedPoints,

                BeforePointsInChest = beforePointsInChest,
                ActualPointsInChest = user.SparePoints,
                AddedPointsToChest = addedPointsToChest,

                BeforeMonthPoints = beforeMonthPoints,
                ActualMonthPoints = actualMonthPoints,
                DayGoal = helpResult.dayGoal,
                MonthGoal = helpResult.challengeGoal,
                MonthThreshold = helpResult.todaysThreshold,
                MonthThresholdText = Helper_GetThresholdText(helpResult.todaysThreshold, actualMonthPoints),
            });
        }



        [HttpPost("usePointsFromChest/{pointsToUse}")]
        public async Task<IActionResult> UsePointsFromChest(int pointsToUse)
        {
            var user = await GetLoggedUser();

            var activeChallenge = await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);
            var helpResult = await Helper_GetProgressesAndPoints(activeChallenge);

            // Kdyz v mesici ziska vic bodu nez je potreba, tak by to vratilo zapornou hodnotu, proto Math.Max
            var missingPoints = Math.Max(0, helpResult.challengeGoal - helpResult.challengePoints);

            if (pointsToUse > missingPoints)
            {
                throw new Exception("Nemůžete použít víc bodů, než zbývá do splnění měsíčního cíle.");
            }

            if (pointsToUse <= 0)
            {
                throw new Exception("Nebylo zadáno kladné číslo.");
            }

            if (user.SparePoints < pointsToUse)
            {
                throw new Exception("Nemáte dostatek bodů v truhličce.");
            }

            user.SparePoints -= pointsToUse;
            var chestPointsUse = new Entities.ChestPointsUse
            {
                UserId = user.Id,
                Created = DateTime.Now,
                Points = pointsToUse
            };

            ChestPointsUsesRepo.Add(chestPointsUse);


            if (!await ProgressRepo.SaveAll())
                throw new Exception("Nepodařilo se uložit výběr z truhličky.");

            var actualDayPoints = helpResult.dayPoints + pointsToUse;
            var actualMonthPoints = helpResult.challengePoints + pointsToUse;

            // Byla zmena - prepocitam HELP RESULT, abych zaposical nove pridane body
            helpResult = await Helper_GetProgressesAndPoints(activeChallenge);


            return Ok(new ProgressResponseDto
            {
                Weeks = GenerateDailyProgresses(activeChallenge, helpResult.challengeProgresses, helpResult.challengeTestProgresses, helpResult.challengeChestPointsUses, helpResult.topicPointsList, helpResult.dayGoal),

                //beforeDayPoints = helpResult.dayPoints,
                ActualDayPoints = actualDayPoints,
                //addedPoints = pointsToUse,

                //beforePointsInChest = helpResult.pointsInChest,
                ActualPointsInChest = user.SparePoints,
                //addedPointsToChest = -pointsToUse,

                //beforeMonthPoints = helpResult.challengePoints,
                ActualMonthPoints = actualMonthPoints,
                //dayGoal = helpResult.dayGoal,
                //monthGoal = helpResult.monthGoal,
                //monthThreshold = helpResult.monthThreshold
            });
        }

        private int Helper_CalculateNewPointsToChest(int newAddedPoints, int monthPoints, int monthGoal)
        {
            var addedPointsToChest = 0;
            // Splnil vic nez mesicni cil -> body navic mu ulozim do truhlicky
            if (newAddedPoints + monthPoints > monthGoal)
            {
                var monthPointsOverflow = Math.Max(0, monthPoints - monthGoal); // Kolik ma mesicnich bodu pres mesicni cil. Pokud ma mene nez je mesicni cil, nastavi se nula
                // OLD   NEW                                                                                            Dostane do truhlicky
                // 3095  +  12   >                         (3107)               -     3100            -       0             =  7;
                // 3120  +   2  =>                         (3122)               -     3100            -      20             =  2;
                // 3130  +   0  =>                         (3130)               -     3100            -      30             =  0;
                addedPointsToChest = (monthPoints + newAddedPoints) - monthGoal - monthPointsOverflow;
            }

            return addedPointsToChest;
        }

        private async Task<(
            List<Entities.Progress> challengeProgresses,
            List<Entities.TestProgress> challengeTestProgresses,
            List<Entities.ChestPointsUse> challengeChestPointsUses,
            List<Entities.Progress> dayProgresses,
            List<Entities.TestProgress> dayTestProgresses,
            List<Entities.ChestPointsUse> dayChestPointsUses,
            List<Entities.TopicPoints> topicPointsList,
            int challengePoints,
            int dayPoints,
            int todaysThreshold,
            int dayGoal,
            int challengeGoal,
            int pointsInChest
            )>
            Helper_GetProgressesAndPoints(PersonalChallenge challenge)
        {
            var loggedUser = await GetLoggedUser();

            return await PublicHelper_GetProgressesAndPoints(
                challenge: challenge,
                userId: loggedUser.Id,
                userSparePoints: loggedUser.SparePoints,
                ProgressRepo,
                TestProgressRepo,
                ChestPointsUsesRepo,
                TopicPointsRepo
                );
        }

        public static async Task<(
            List<Entities.Progress> challengeProgresses,
            List<Entities.TestProgress> challengeTestProgresses,
            List<Entities.ChestPointsUse> challengeChestPointsUses,
            List<Entities.Progress> dayProgresses,
            List<Entities.TestProgress> dayTestProgresses,
            List<Entities.ChestPointsUse> dayChestPointsUses,
            List<Entities.TopicPoints> topicPointsList,
            int challengePoints,
            int dayPoints,
            int todaysThreshold,
            int dayGoal,
            int challengeGoal,
            int pointsInChest
            )>
            PublicHelper_GetProgressesAndPoints(PersonalChallenge challenge, int userId, int userSparePoints, EFProgressRepo progressRepo, EFTestProgressRepo testProgressRepo, EFChestPointsUsesRepo chestPointsUsesRepo, EFTopicPointsRepo topicPointsRepo)
        {
            var today = DateTime.Now.Date;
            var tomorrow = today.AddDays(1);

            var challengeProgresses = await progressRepo.All.Where(x => x.UserId == userId && x.Created >= challenge.StartDate && x.Created <= challenge.EndDate).ToListAsync();
            var challengeTestProgresses = await testProgressRepo.All.Where(x => x.UserId == userId && x.Created >= challenge.StartDate && x.Created <= challenge.EndDate).ToListAsync();
            var challengeChestPointsUses = await chestPointsUsesRepo.All.Where(x => x.UserId == userId && x.Created >= challenge.StartDate && x.Created <= challenge.EndDate).ToListAsync();
            var challengeTopicProgresses = await topicPointsRepo.All.Where(x => x.UserId == userId && x.Created >= challenge.StartDate && x.Created <= challenge.EndDate).ToListAsync();

            var dayProgresses = challengeProgresses.Where(x => x.Created >= today && x.Created < tomorrow).ToList();
            var dayTestProgresses = challengeTestProgresses.Where(x => x.Created >= today && x.Created < tomorrow).ToList();
            var dayChestPointsUses = challengeChestPointsUses.Where(x => x.Created >= today && x.Created < tomorrow).ToList();
            var dayTopicProgresses = challengeTopicProgresses.Where(x=>x.Created >= today && x.Created < tomorrow).ToList();


            var challengePoints = challengeProgresses.Sum(x => x.Points) + challengeTestProgresses.Sum(x => x.Points) + challengeChestPointsUses.Sum(x => x.Points) + challengeTopicProgresses.Sum(x=>x.Points);
            var dayPoints = dayProgresses.Sum(x => x.Points) + dayTestProgresses.Sum(x => x.Points) + dayChestPointsUses.Sum(x => x.Points) + dayTopicProgresses.Sum(x=>x.Points);

            

            Helper_GetChallengeGoalAndTodaysThreshold(challenge, out int challengeGoal, out int todaysThreshold);




            return (challengeProgresses, challengeTestProgresses, challengeChestPointsUses, dayProgresses, dayTestProgresses, dayChestPointsUses, challengeTopicProgresses, challengePoints, dayPoints, todaysThreshold, 100, challengeGoal, userSparePoints);
        }


        //private async Task Helper_GetActualProgressesAndPoints()
        //{
        //    var challenge = await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);

        //    var today = DateTime.Now.Date;
        //    var tomorrow = today.AddDays(1);

        //    var challengeProgresses = await ProgressRepo.All.Where(x => x.UserId == UserId && x.Created >= challenge.StartDate && x.Created <= challenge.EndDate).ToListAsync();
        //    var challengeTestProgresses = await TestProgressRepo.All.Where(x => x.UserId == UserId && x.Created >= challenge.StartDate && x.Created <= challenge.EndDate).ToListAsync();
        //    var challengeChestPointsUses = await ChestPointsUsesRepo.All.Where(x => x.UserId == UserId && x.Created >= challenge.StartDate && x.Created <= challenge.EndDate).ToListAsync();

        //    var dayProgresses = challengeProgresses.Where(x => x.Created >= today && x.Created < tomorrow).ToList();
        //    var dayTestProgresses = challengeTestProgresses.Where(x => x.Created >= today && x.Created < tomorrow).ToList();
        //    var dayChestPointsUses = challengeChestPointsUses.Where(x => x.Created >= today && x.Created < tomorrow).ToList();

        //    var challengePoints = challengeProgresses.Sum(x => x.Points) + challengeTestProgresses.Sum(x => x.Points) + challengeChestPointsUses.Sum(x => x.Points);
        //    var dayPoints = dayProgresses.Sum(x => x.Points) + dayTestProgresses.Sum(x => x.Points) + dayChestPointsUses.Sum(x => x.Points);
        //}

        private string Helper_GetThresholdText(int monthThreshold, int monthPoints)
        {
            string monthThresholdText = "";
            var thresholdDiff = monthThreshold - monthPoints;


            if (thresholdDiff > 0) // Body chybi - musi pridat
            {
                monthThresholdText = $"Makej, chybí ti ještě {thresholdDiff} bodů.";
            }
            else if (thresholdDiff < 0) // Ma vic bodu - muze spomalit
            {
                monthThresholdText = $"Paráda, máš o {Math.Abs(thresholdDiff)} bodů víc. Můžeš jít na párek.";
            }
            else // Ma akorat
            {
                monthThresholdText = $"Dneska už si můžeš dát leháro. Máš splněno.";
            }

            return monthThresholdText;
        }

        public static void Helper_GetChallengeGoalAndTodaysThreshold(PersonalChallenge challenge, out int challengeGoal, out int todaysThreshold)
        {
            var now = DateTime.Now;

            var daysInChallenge = (int)(challenge.EndDate.Date.AddDays(1) - challenge.StartDate.Date).TotalDays;

            challengeGoal = daysInChallenge * 100;

            // Cislovano od jednicky
            var nthDayOfChallenge = (int)(now.Date - challenge.StartDate.Date).TotalDays + 1;
            todaysThreshold = nthDayOfChallenge * 100;
        }

        // Zacal hrat teprve v prubehu tohoto mesice? (predtim nikdy nic)
        private async Task<bool> Helper_IsThisMonthFirst(int userId, DateTime now)
        {
            var thisMonth_firstDay = new DateTime(now.Year, now.Month, 1);

            bool isThisMonthFirst = false;
            if (
                !await ProgressRepo.IsAnyProgressBeforeThisDate(userId, thisMonth_firstDay) &&
                !await TestProgressRepo.IsAnyTestProgressBeforeThisDate_WARNING_MushHavePoints(userId, thisMonth_firstDay)
                )
            {
                isThisMonthFirst = true;
            }

            return isThisMonthFirst;
        }

        public static (int lessonOrder, int sectionOrder, int exerciseOrder) GetDoThisRightNowExercise(List<Lesson> lessons, List<Progress> progresses, List<TestProgress> testProgresses)
        {
            foreach (var lesson in lessons)
            {
                foreach (var section in lesson.Sections)
                {
                    // Je sekce splnena testem?
                    if (IsSectionDoneByTest(lesson, section.Order, testProgresses))
                    {
                        // Nekontroluji uz cviceni v teto sekci, protoze cela sekce je splnena testem
                        continue;
                    }

                    foreach (var exercise in section.Exercises)
                    {
                        if (!IsExerciseDoneByNormalProgress(lesson.Order, section.Order, exercise, progresses))
                        {
                            // Cviceni NENI splneno -> Je to prvni cviceni, ktere neni splneno -> zaznamenam ho
                            return (lesson.Order, section.Order, exercise.Order);
                        }
                    }
                }
            }

            // Pokus jsem se dostal sem, tak uz je vsechno splnene -> Falesne nastavim prvni cviceni c 999 lekce
            return (999, 1, 1);
        }

        public static bool IsSectionDoneByTest(Lesson lesson, int sectionOrder, List<TestProgress> testProgresses)
        {
            var doneTestProgresses = testProgresses.Where(x => x.LessonOrder == lesson.Order && x.Percentage >= Test.PercentageThreshold).ToList();

            for (var i = 0; i < lesson.Tests.Count; i++)
            {
                var test = lesson.Tests[i];
                Test nextText = null;
                if (i + 1 < lesson.Tests.Count)
                    nextText = lesson.Tests[i + 1];

                // Spada sekce do rozsahu tohoto testu?
                if (test.Order <= sectionOrder && (nextText == null || nextText.Order > sectionOrder))
                {
                    // Je test hotovy?
                    if (doneTestProgresses.Any(x => x.TestOrder == test.Order))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // Sem se to nesmi nikdy dostat, protoze kazda sekce musi byt v nejakem testu.

            throw new System.Exception("Sekce nelezi v rozsahu zadneho testu.");
        }

        /// <summary>
        /// Je cviceni splnene normalnim odehranim (NE TESTEM!)
        /// </summary>
        /// <returns></returns>
        public static bool IsExerciseDoneByNormalProgress(int lessonOrder, int sectionOrder, IExercise exercise, List<Progress> progresses)
        {
            // Zamcena cviceni ma jako hotova
            if(exercise.IsLock)
            {
                return true;
            }

            // Slovicka -> Musi byt splnena vsechna podcviceni
            if (exercise.Type == IExercise.Types.ExerciseVocabulary)
            {
                // Kdyz ma splnene 10. podcviceni -> ma splneno i vsechno predtim
                return progresses.Any(x => x.LessonOrder == lessonOrder && x.SectionOrder == sectionOrder && x.ExerciseOrder == exercise.Order && x.ExerciseSuborder == 10);
            }
            else // Normalni cviceni
            {
                return progresses.Any(x => x.LessonOrder == lessonOrder && x.SectionOrder == sectionOrder && x.ExerciseOrder == exercise.Order);
            }

        }
    }
}