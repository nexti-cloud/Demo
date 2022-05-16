using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AnglickaVyzva.API.Helpers;
using AutoMapper;
using AnglickaVyzva.API.DTOs;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace AnglickaVyzva.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LessonController : BaseController
    {

        public LessonController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {
        }

        [HttpGet("GetLesson/{lessonOrder}")]
        public async Task<IActionResult> GetLesson(int lessonOrder)
        {
            await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);

            var user = await GetLoggedUser();
            var lesson = await LessonRepo.GetCachedLesson(lessonOrder, user.IsPremium);

            return Ok(lesson);
        }

        [HttpGet("GetLessonsInfo")]
        public async Task<IActionResult> GetLessonsInfo()
        {
            await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);

            var user = await this.GetLoggedUser();
            var progresses = await ProgressRepo.GetProgresses(user.Id);
            var testProgresses = await TestProgressRepo.GetTestProgresses(user.Id);
            var lessons = _mapper.Map<List<LessonDto>>(await LessonRepo.GetCachedLessons(user.IsPremium));

            var lastLessonOrder = 21;

            if(EnvironmentHelper.IsOnAnyDev())
            {
                lastLessonOrder = lessons.Count;
            }

            // Pripravim informace pro kazdou lekci
            for(var i=1; i <= lastLessonOrder; i++)
            {
                var lesson = lessons[i-1];
                if (i == 1) // Prvni lekce je vzdy pristupna
                    lesson.IsOpen = true;

                // Pokud uz nekdy neco v teto lekci delal, je jasne, ze ji ma zpristupnenou.
                // RESI to ten problem, kdy ma treba hotovou prvni lekci na 100% a ma rozpracovanou druhou. Koupi si kurz a odemknou se mu dalsi cviceni -> uz nema splnenou celou jednicku -> nemohl by pokracovat ve 2 -> byl by nazlobeny
                if (progresses.Any(x => x.LessonOrder == i))
                    lesson.IsOpen = true;

                // Nasekam obsah lekce do radku, aby to nemusela delat aplikace sama
                lesson.ContentGrammarLines = lesson.ContentGrammar?.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                lesson.ContentVocabularyLines = lesson.ContentVocabulary?.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

                LessonHelper.SetTestProgressInLesson(lesson, testProgresses);
                lesson.PercentageDone = (int)Math.Floor(LessonHelper.GetLessonPercentageDone(lesson, progresses));

                // Pokud je predchozi lekce scela splnena, otevru tuto lekci
                if (i > 1)
                {
                    if(lessons[i-2].PercentageDone == 100)
                    {
                        lesson.IsOpen = true;
                    }
                    else
                    {
                        lesson.IsOpen = false;
                    }
                }

                //Oddelam testy a sekce, abych je zbytecne neprenasel pres internet, kdyz nejsou potreba
                lesson.Sections = null;
                lesson.Tests = null;
            }

            return Ok(lessons);
        }


        
    }
}
