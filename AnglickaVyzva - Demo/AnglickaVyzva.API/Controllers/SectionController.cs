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
    public class SectionController : BaseController
    {
        public SectionController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {

        }

        [HttpGet("GetSectionsAndTestsInfo/{lessonOrder}")]
        public async Task<IActionResult> GetSectionsAndTestsInfo(int lessonOrder)
        {
            await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);

            var sections = new List<SectionDto>();
            var tests = new List<TestDto>();


            var user = await GetLoggedUser();


            var progresses = await ProgressRepo.GetProgresses(user.Id);

            var allTestProgresses = await TestProgressRepo.All.Where(x => x.UserId == UserId && x.Percentage >= TestProgress.ThresholdPoints).ToListAsync();

            // Berou vsechny testy - i ty nesplnene. Nevim jestli je to potreba. Kdyby tady bylo nekdy potreba optimalozivat, mozna by slo brat testy z promenne allTestProgresses vise
            var testProgresses = await TestProgressRepo.GetTestProgresses(user.Id, lessonOrder);
            var prevLessonTestProgresses = await TestProgressRepo.GetTestProgresses(user.Id, lessonOrder - 1);

            var allLessons = await LessonRepo.GetCachedLessons(user.IsPremium);
            var lessonDto = _mapper.Map<LessonDto>(allLessons.First(x => x.Order == lessonOrder));

            //// Je predchozi lekce splnena? (potrebuju to vedet, abych podle toho zpristupnil aktualni lekci)
            #region Je predchozi lekce splnena?
            bool isPrevLessonDone;

            if (lessonOrder > 1)
            {

                var res = ProgressController.GetDoThisRightNowExercise(allLessons, progresses, allTestProgresses);
                // Aktualni cviceni, ktere je na rade, je v teto nebo v nasledujici lekci => urcite je predchozi hotova
                if(res.lessonOrder>= lessonOrder)
                {
                    isPrevLessonDone = true;
                }
                else
                {
                    isPrevLessonDone = false;
                }

                #region stary kod
                // --- TENTO KOD NEFUNGOVAL, POKUD V PREDCHOZI LEKCI BYLA V POSLEDNIM USEKU VSECHNA CVICENA ZAMCENA A NEBYL SPLNENI POSLEDNI TEST (nemusel byt splnen, protoze ta cvicena jsou vsechna zamcena)
                //var prevLessonDto = _mapper.Map<LessonDto>(allLessons.First(x => x.Order == lessonOrder - 1));
                ////Nastaveni splneni testu PREDCHOZI lekce
                //LessonHelper.SetTestProgressInLesson(prevLessonDto, prevLessonTestProgresses);

                //// Je predchozi lekce dokoncena pomoci testu?
                //if (prevLessonDto.Tests.Last().IsDone)
                //{
                //    isPrevLessonDone = true;
                //}
                //else // Predchozi lekce neni dokoncena pomoci testu
                //{
                //    // Zkontroluji jestli existuje postup (splneni) posledniho cviceni v predchozi sekci
                //    // Posledni nezamcena sekce v predchozi lekci
                //    var lastNonLockedSectionInPrevLesson = prevLessonDto.Sections.Last(x => x.IsLock == false);
                //    // Posledni nezamcene cviceni v posledni nezemcene sekci predchozi lekce
                //    var lastExerciseInPrevLesson = lastNonLockedSectionInPrevLesson.Exercises.Last(x => x.IsLock == false);

                //    // Je posledni cviceni Slovicka?
                //    if (lastExerciseInPrevLesson.Type == "vocabulary")
                //    {
                //        // Je splnene posledni cviceni a vsechny jeho podcviceni v predchozi lekci? (vsech 10 podcviceni musi byt hotovo)
                //        if (await ProgressRepo.IsVocabularyExerciseDone(UserId, prevLessonDto.Order, lastNonLockedSectionInPrevLesson.Order, lastExerciseInPrevLesson.Order))
                //        {
                //            isPrevLessonDone = true;
                //        }
                //        else
                //        {
                //            isPrevLessonDone = false;
                //        }
                //    }
                //    else // Normalni cviceni (NEJSOU to slovicka)
                //    {
                //        // Je splnene posledni cviceni v predchozi lekci?
                //        if (await ProgressRepo.IsNormalExerciseDone(UserId, prevLessonDto.Order, lastNonLockedSectionInPrevLesson.Order, lastExerciseInPrevLesson.Order))
                //        {
                //            isPrevLessonDone = true;
                //        }
                //        else
                //        {
                //            isPrevLessonDone = false;
                //        }
                //    }
                //}
                // --- END ---
                #endregion stary kod


            }
            else // Jsem v prvni lekci, nic prede mnou nemuze byt nesplneno
            {
                isPrevLessonDone = true;
            }
            //// END Je predchozi lekce splnena
            #endregion END Je predchozi lekce splnena?


            //// Nastaveni splneni testu
            LessonHelper.SetTestProgressInLesson(lessonDto, testProgresses);

            foreach (var test in lessonDto.Tests)
            {
                // IsOpen se nastavuje dale v kodu
                tests.Add(new TestDto { IsDone = test.IsDone, Order = test.Order, PercentageDone = test.PercentageDone });
            }
            //// END Nastaveni splneni testu

            //// Delal uz neco z teto lekce?
            bool isSomethingDoneInThisLesson = progresses.Any(x => x.LessonOrder == lessonOrder);
            //// END Delal uz neco z teto lekce?

            // Jsou vsechny predchozi sekce splnene (Osetreni pri koupi premioveho kurzu) Jinak by nastalo, ze se otevrou sekce po dokoncenych nepremiovych sekcich, ale nemel by splnene predchozi premiove sekce -> neumel by vsechno co potrebuje znat
            var areAllPrevSectionsDone = isPrevLessonDone; // Zacinam u prvni sekce. Pokud je predchozi LEKCE splnena, je splnena i predchozi sekce z predchozi splnene lekce

            // Projdu sekci po sekci a nastavim ji procenta splneni a informaci, jestli je otevrena (jestli uz se k ni propracoval)
            foreach (var section in lessonDto.Sections.OrderBy(x => x.Order).ToList())
            {

                var newSection = new SectionDto { Order = section.Order, IsLock = section.IsLock };

                //// Pokud nema koupeny kurz, bude se moct dostat jenom do Lekce 1
                //if (user.IsPremium == false && lessonOrder == 2)
                //{
                //    newSection.IsOpen = true;
                //    newSection.IsLock = true;
                //    newSection.IsDone = false;
                //    newSection.PercentageDone = 0;
                //    sections.Add(newSection);
                //    continue;
                //}

                // Pokud predchozi lekce neni hotova a zaroven v teto lekci jeste nic nedelal -> vsechny sekce jsou automaticky neresene a zavrene
                if (isSomethingDoneInThisLesson == false && isPrevLessonDone == false)
                {
                    newSection.IsOpen = false;
                    newSection.IsDone = false;
                    newSection.PercentageDone = 0;
                    sections.Add(newSection);
                    continue;
                }
                // END

                // Je tato sekce splnena testem?
                var isDoneByTest = false;
                for (var i = 0; i < lessonDto.Tests.Count; i++)
                {
                    var test = lessonDto.Tests[i];
                    TestDto nextTest = null;
                    if (i + 1 < lessonDto.Tests.Count)
                        nextTest = lessonDto.Tests[i + 1];

                    // Je tento test hotovy?
                    if (test.PercentageDone >= Test.PercentageThreshold)
                    {
                        // Spada cviceni do rozsahu tohoto testu?
                        if (test.Order <= section.Order && (nextTest == null || nextTest.Order > section.Order))
                        {
                            isDoneByTest = true;
                            //newSection.PercentageDone = 100;
                            newSection.IsDone = true;

                            if (!newSection.IsLock)
                                newSection.IsOpen = true;
                            break;
                        }
                    }
                }

                // Procento splneni sekce (prochazeni cviceni)
                var notLockedExercises = section.Exercises.Where(x => x.IsLock == false).ToList();
                foreach (var exercise in notLockedExercises) // Nacitam pouze odemcena civceni, protoze zamcena cviceni jako by neexistovala
                {

                    // Slovicka (Kontroluji podcviceni)
                    if (exercise.Type == "vocabulary")
                    {
                        // Vsechna podcviceni slovicek jsou splnena
                        if (progresses.Count(x => x.LessonOrder == lessonDto.Order && x.SectionOrder == section.Order && x.ExerciseOrder == exercise.Order) == 10)
                        {
                            newSection.PercentageDone += 100.0 / notLockedExercises.Count;

                            // Nekdy to spocita 100.00001 nebo 99.99999
                            if (newSection.PercentageDone >= 99.9)
                            {
                                newSection.PercentageDone = 100;
                            }
                        }
                    }
                    else // Normalni typ cviceni
                    {
                        if (progresses.Any(x => x.LessonOrder == lessonDto.Order && x.SectionOrder == section.Order && x.ExerciseOrder == exercise.Order))
                        {
                            newSection.PercentageDone += 100.0 / notLockedExercises.Count;

                            // Nekdy to spocita 100.00001
                            if (newSection.PercentageDone >= 99.9)
                            {
                                newSection.PercentageDone = 100;
                            }
                        }
                    }
                }
                // END Procento splneni sekce  (prochazeni cviceni)


                // Pokud je splneno testem, nemusim nic dal kontrolovat
                if (isDoneByTest)
                {
                    sections.Add(newSection);
                    continue;
                }
                // END JE toto cviceni splneno testem?

                // Je sekce OTEVRENA?
                //if (newSection.IsLock == false && (newSection.PercentageDone > 0 || areAllPrevSectionsDone)) // v teto sekci se uz neco delalo NEBO predchozi sekce je hotova
                if (newSection.IsLock == false && areAllPrevSectionsDone) // Je predchoyi sekce hotova?
                {
                    newSection.IsOpen = true;
                }
                // END Je sekce OTEVRENA?


                // Je sekce HOTOVA?
                if (newSection.PercentageDone >= 100) // Nekdy to secte 100.00000000001
                {
                    newSection.IsDone = true;
                }
                else // Sekce NENI hotova
                {
                    // Pokud je tato sekce zamcena, prenesu splneni predchozich sekci. Tato sekce jakoze neexistuje.
                    if (!newSection.IsLock)
                    {
                        // Prvni sekce, ktera neni splnena
                        if (areAllPrevSectionsDone)
                        {
                            newSection.DoThisRightNow = true;
                        }

                        areAllPrevSectionsDone = false; // Pro dalsi sekci bude tahle predchozi
                    }

                }
                // END JE sekce HOTOVA?

                sections.Add(newSection);
            }



            // Je test otevreny? Uz je na rade?
            TestDto prevTest = null;
            foreach (var test in tests)
            {
                // Pokud nema koupeny kurz, bude se moct dostat jenom do Lekce 1
                //if (user.IsPremium == false && lessonOrder > 2)
                //{
                //    test.IsOpen = false;
                //    test.IsDone = false;
                //    continue;
                //}

                // Prvni test v lekci a predchozi lekce je splnena -> tento test je prave na rade
                if (test.Order == 1 && isPrevLessonDone)
                {
                    test.IsOpen = true;
                    prevTest = test;
                    continue;
                }

                // Je predchozi test splneny?
                if (prevTest != null && prevTest.IsDone)
                {
                    test.IsOpen = true;
                    prevTest = test;
                    continue;
                }

                // Je posledni otevrena sekce pred timto testem hotova?
                // A ZAROVEN je predchozi test splnen?
                var lastDoneSectionBeforeTest = sections.LastOrDefault(x => x.IsDone == true && x.IsOpen == true && x.Order < test.Order);
                var lastSectionBeforeTest = sections.LastOrDefault(x => x.IsLock == false && x.Order < test.Order);
                if (lastDoneSectionBeforeTest != null && lastDoneSectionBeforeTest == lastSectionBeforeTest)
                {
                    // Tato podminka osetruje koupeni premioveho kurzu. Zabranuje tomu, aby byl otevreny test a pred nim byl nejaky zavreny
                    if (prevTest != null && prevTest.IsOpen)
                    {
                        test.IsOpen = true;
                    }
                }

                prevTest = test;
            }
            // END Je test otevreny? Uz je na rade?


            return Ok(new
            {
                sections,
                tests
            });
        }

        

        
    }
}