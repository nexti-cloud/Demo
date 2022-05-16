using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.DTOs;
using AnglickaVyzva.API.Entities;
using AnglickaVyzva.API.Helpers;
using AnglickaVyzva.API.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AnglickaVyzva.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ExerciseController : BaseController
    {
        public ExerciseController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {
        }

        private async Task GetFirstAvailableExercise(List<Lesson> lessons)
        {
            var allProgresses = await ProgressRepo.All.Where(x => x.UserId == UserId)
                    .Select(x => new
                    {
                        x.LessonOrder,
                        x.SectionOrder,
                        x.ExerciseOrder,
                        x.ExerciseSuborder,
                    })
                    .ToListAsync();

            var allTestProgresses = await TestProgressRepo.All.Where(x => x.UserId == UserId && x.Percentage >= TestProgress.ThresholdPoints).ToListAsync();
        }

        [HttpGet("GetExercisesProgress/{lessonOrder}/{sectionOrder}")]
        public async Task<IActionResult> GetExercisesProgress(int lessonOrder, int sectionOrder)
        {
            await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);

            var user = await GetLoggedUser();
            var allProgresses = await ProgressRepo.All.Where(x => x.UserId == user.Id).ToListAsync();

            var progressesInThisSection = allProgresses.Where(x=>x.LessonOrder == lessonOrder && x.SectionOrder == sectionOrder).ToList();
            var section = await LessonRepo.GetCachedSection(lessonOrder, sectionOrder, user.IsPremium);

            var exerciseProgresses = new List<ExerciseProgressDto>();
            var subexerciseProgresses = new List<SubexerciseProgressDto>();


            var allTestProgresses = await TestProgressRepo.All.Where(x => x.UserId == UserId && x.Percentage >= TestProgress.ThresholdPoints).ToListAsync();
            //var lessonTestProgresses = await TestProgressRepo.All.Where(x => x.LessonOrder == lessonOrder && x.UserId == UserId).ToListAsync();
            var lessonTestProgresses = allTestProgresses.Where(x => x.LessonOrder == lessonOrder).ToList();

            var allLessons = await LessonRepo.GetCachedLessons(user.IsPremium);
            var lesson = allLessons.First(x => x.Order == lessonOrder);

            var doThisRightNowResult = ProgressController.GetDoThisRightNowExercise(allLessons, allProgresses, allTestProgresses);

            // POZOR - Pravdepodobne je mozne osekani kodu za pouziti vysledku ProgressController.GetDoThisRightNowExercise

            // Zjistim, kolik sekci je splneno testem. Pokud tato sekce, ze ktere se nacita postup pro cviceni, je take splena testem, tak vsechny cviceni v ni budu zobrazovat jako splnena.
            //int lastSectionIndexDoneByTest = int.MinValue;
            //int firstSectionDoneByTest = int.MaxValue;

            //                                      startIndex, endIndex
            var sectionsRangesDoneByTests = new List<Tuple<int, int>>();

            for (int i = 0; i < lesson.Tests.Count; i++)
            {
                var test = lesson.Tests[i];

                // Vytahnu z DB progres pro tento test
                var testProgress = lessonTestProgresses.FirstOrDefault(x => x.UserId == UserId && x.TestOrder == test.Order);

                // Pokud neni v DB progress pro tento test, uz nema cenu pokracovat dal, protoze se testy musi plnit postupni -> dalsi v poradi uz v DB byt nemuze
                // !!!Predchozi NEPLATI, protoze muze prvne cast(ktera spada pod prvni test) udelat normalne postupne a potom preskoci az tu cast druhou
                if (testProgress == null)
                {
                    // DRIV TU BYL BREAK
                    continue;
                }

                // Vysledek testu je urcite splneny, protoze z DB vytahuju jenom splnene vysledky

                // Kontroluji posledni test -> cela lekce je splnena
                if (i + 1 == lesson.Tests.Count)
                {
                    sectionsRangesDoneByTests.Add(new Tuple<int, int>(test.Order, lesson.Sections.Count));
                    //lastSectionIndexDoneByTest = lesson.Sections.Count;
                }
                else
                {
                    var nextTest = lesson.Tests[i + 1];
                    sectionsRangesDoneByTests.Add(new Tuple<int, int>(test.Order, nextTest.Order - 1));
                    //lastSectionIndexDoneByTest = nextTest.Order - 1;
                }

            }

            var isWholeSectionDoneByTest = sectionsRangesDoneByTests.Any(x => section.Order >= x.Item1 && section.Order <= x.Item2);
            //var isWholeSectionDoneByTest = lastSectionIndexDoneByTest >= section.Order;



            var areAllPrevExercisesDone = true; // true -> Prvni cviceni v sekci tak bude automaticky otevrene; Zamknuta cviceni tuto hodnotu nemeni -> jakoze neexistuji


            foreach (var exercise in section.Exercises)
            {
                // Zamcena cviceni jakoze neexistuji -> nemeni priznak isPrevExerciseDone
                if (exercise.IsLock)
                    continue;

                // Slovicka
                if (exercise.Type == "vocabulary")
                {
                    var allDone = true;
                    // Vsechny podcviceni slovicek
                    for (var i = 1; i <= 10; i++)
                    {

                        var subexerciseProgress = new SubexerciseProgressDto
                        {
                            Points = 10,
                            ExerciseOrder = exercise.Order,
                            SubexerciseOrder = i
                        };


                        // Prehled slovicek je vzdy otevreny
                        if (i == 1)
                        {
                            subexerciseProgress.IsOpen = true;
                        }

                        // Toto podcviceni ma hotove
                        if (progressesInThisSection.Any(x => x.ExerciseOrder == exercise.Order && x.ExerciseSuborder == i))
                        {
                            subexerciseProgress.IsOpen = true;
                            subexerciseProgress.IsDone = true;
                        }
                        else
                        {
                            // Prvni podcviceni, ktere nema hotove
                            if(allDone)
                            {
                                if(
                                    doThisRightNowResult.lessonOrder == lessonOrder && 
                                    doThisRightNowResult.sectionOrder == sectionOrder && 
                                    doThisRightNowResult.exerciseOrder == exercise.Order
                                    )
                                {
                                    subexerciseProgress.DoThisRightNow = true;
                                }
                            }

                            allDone = false;
                        }


                        // Toto podcviceni neni otevrene
                        if (subexerciseProgress.IsOpen == false)
                        {
                            // Zkontroluji, jestli predchozi podcviceni je hotove -> otevru tohle
                            var prevSubexercise = subexerciseProgresses.FirstOrDefault(x => x.SubexerciseOrder == i - 1);
                            if (prevSubexercise != null)
                            {
                                // Predchozi cviceni je hotove -> otevru mu dalsi, at ho muze zacit hrat
                                if (prevSubexercise.IsDone)
                                    subexerciseProgress.IsOpen = true;
                            }
                        }

                        subexerciseProgresses.Add(subexerciseProgress);
                    }

                    var exerciseProgress = new ExerciseProgressDto
                    {
                        ExerciseOrder = exercise.Order,
                        IsOpen = true, // Slovicka jsou prvni -> vzdycky jsou otevrena (pocitam s tim, ze se do teto sekce nedostane, pokud predchozi neni hotova)
                        Points = exercise.Points
                    };
                    // Vsechna podcviceni jsou hotova -> cele cviceni slovicek je hotove (dalsi cviceni uvidi ze je toto DONE a muze se otevrit)
                    if (allDone)
                    {
                        exerciseProgress.IsDone = true;
                    }
                    else
                    {

                        exerciseProgress.IsDone = false;
                        areAllPrevExercisesDone = false; // Nez se otevre dalsi cviceni, musi byt toto splneno
                    }

                    exerciseProgresses.Add(exerciseProgress);

                }
                else // Normalni cviceni
                {
                    var exerciseProgress = new ExerciseProgressDto
                    {
                        ExerciseOrder = exercise.Order,
                        Points = exercise.Points
                    };

                    if (areAllPrevExercisesDone)
                    {
                        exerciseProgress.IsOpen = true;
                    }

                    // Toto cviceni je splneno
                    if (progressesInThisSection.Any(x => x.ExerciseOrder == exercise.Order))
                    {
                        // Osetreni, pokud predtim splnil cviceni v zamcene sekci a ted se mu sekce odemknula, tak se muze stat, ze toto cviceni uz splnil, ale odemklo se predchozi cviceni,
                        // ktere tim padem splnene nema -> do tohoto splneneho cviceni by se nemohl dostat -> pro jistotu i zde nastavim IsOpen na true
                        exerciseProgress.IsOpen = true;

                        exerciseProgress.IsDone = true;
                    }
                    else // Toto cviceni jeste neni splneno
                    {
                        // Prvni cviceni, ktere neni splnene
                        if (areAllPrevExercisesDone)
                        {
                            if (
                                    doThisRightNowResult.lessonOrder == lessonOrder &&
                                    doThisRightNowResult.sectionOrder == sectionOrder &&
                                    doThisRightNowResult.exerciseOrder == exercise.Order
                                    )
                            {
                                exerciseProgress.DoThisRightNow = true;
                            }
                        }

                        exerciseProgress.IsDone = false;
                        areAllPrevExercisesDone = false;
                    }
                    exerciseProgresses.Add(exerciseProgress);
                }
            }

            // Pokud je cela sekce splnena testem, jsou vsechna cviceni otevrena
            if (isWholeSectionDoneByTest)
            {
                exerciseProgresses.ForEach(x => x.IsOpen = true);
                subexerciseProgresses.ForEach(x => x.IsOpen = true);
            }

            return Ok(new
            {
                exerciseProgresses,
                subexerciseProgresses
            });
        }
    }
}