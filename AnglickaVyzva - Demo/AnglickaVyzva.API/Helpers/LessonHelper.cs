using AnglickaVyzva.API.DTOs;
using AnglickaVyzva.API.Entities;
using AnglickaVyzva.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Helpers
{
    public class LessonHelper
    {
        public static double GetLessonPercentageDone(LessonDto lesson, List<Progress> progresses)
        {
            var progress = 0.0;

            foreach (var section in lesson.Sections)
            {
                // Preskakuji, problem se splnenymi procenty to neudela, protoze pri pricitani procent za splnene sekce pocitam jenom s otevrenymi (viz spodek teto metody)
                if (section.IsLock)
                {
                    continue;
                }

                // Je tato sekce splnena testem?
                var isDoneByTest = false;
                for (var i = 0; i < lesson.Tests.Count; i++)
                {
                    var test = lesson.Tests[i];
                    TestDto nextTest = null;
                    if (i + 1 < lesson.Tests.Count)
                        nextTest = lesson.Tests[i + 1];

                    // Je tento test hotovy?
                    if (test.PercentageDone >= Test.PercentageThreshold)
                    {
                        // Spada cviceni do rozsahu tohoto testu?
                        if (test.Order <= section.Order && (nextTest == null || nextTest.Order > section.Order))
                        {
                            isDoneByTest = true;
                            progress += 100.0 / lesson.Sections.Count(x => x.IsLock == false); // Pridam cas procent za tuto sekci. Pocitam jenom s odemcenymi lekcemi
                            break;
                        }
                    }
                }
                if (isDoneByTest)
                    continue;
                // END JE toto cviceni splneno testem?

                var allExercisesCompleted = true;

                foreach (var exercise in section.Exercises)
                {
                    // Kdyz je zamcene cviceni, tak ho nemuze mit hotove -> preskakuji, abych nenastavil allExercisesCompleted na FALSE
                    if (exercise.IsLock)
                    {
                        continue;
                    }

                    // Cviceni slovicek se sklada z dalsich deset podcviceni. Aby byla slovicka splnena, musi byt splneno vsech deset podcviceni
                    if (exercise.Type == "vocabulary")
                    {
                        if (progresses.Count(x => x.LessonOrder == lesson.Order && x.SectionOrder == section.Order && x.ExerciseOrder == exercise.Order) != 10)
                        {
                            // Vsech deset podcviceni neni hotovych
                            allExercisesCompleted = false;
                            break;
                        }
                    }
                    else // Normalni cviceni (nejsou to slovicka)
                    {
                        if (!progresses.Any(x => x.LessonOrder == lesson.Order && x.SectionOrder == section.Order && x.ExerciseOrder == exercise.Order))
                        {
                            // Toto cviceni jeste nesplnil
                            allExercisesCompleted = false;
                            break;
                        }
                    }
                }

                // Vsechna cviceni jsou v sekci splnena -> je splnena i sekce
                if (allExercisesCompleted)
                {
                    progress += 100.0 / lesson.Sections.Count(x => x.IsLock == false); // Pridam cast procent za tuto sekci. Pocitam jenom s odemcenymi lekcemi
                    continue;
                }

            }
            if (progress > 99.9)
                progress = 100;

            // Workaround pro lekce, ktere jsou cele zamcene, ale splnil jsem vsechny testy v nich (konkretne se jedna o lekci Order:15 a dalsi)
            if(lesson.Sections.Count(x=>x.IsOpen) == 0)
            {
                if(lesson.Tests.All(x=>x.IsDone))
                {
                    progress = 100;
                }
            }

            return progress;
        }


        public static void SetTestProgressInLesson(LessonDto lesson, List<TestProgress> testProgresses)
        {
            foreach (var test in lesson.Tests)
            {
                var testProgress = testProgresses.OrderByDescending(x => x.Percentage).FirstOrDefault(x => x.LessonOrder == lesson.Order && x.TestOrder == test.Order);
                if (testProgress != null)
                {
                    test.PercentageDone = testProgress.Percentage;
                    if (test.PercentageDone >= Test.PercentageThreshold)
                    {
                        test.IsDone = true;
                    }
                    else
                        test.IsDone = false;
                }
            }
        }
    }
}
