// constructor
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using AnglickaVyzva.API.Data;
using Microsoft.AspNetCore.Hosting;
using System.Threading;
using Microsoft.EntityFrameworkCore;
// end constructor

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AnglickaVyzva.API.Entities;

namespace AnglickaVyzva.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PersonalChallengeController : BaseController
    {
        public class NotPaidException : Exception
        {
            public NotPaidException() : base(PredefinedMessage)
            {
                
            }

            public const string PredefinedMessage = "--not-paid--";
        }

        public PersonalChallengeController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {
        }


        private static SemaphoreSlim semaphore_GetOrCreateActivePersonalChallenge = new SemaphoreSlim(1, 1);

        public static async Task<PersonalChallenge> GetOrCreateActivePersonalChallenge(EFPersonalChallengeRepo personalChallengeRepo, EFUserRepo userRepo, int userId)
        {
            var activePersonChallenge = await GetActivePersonalChallenge(personalChallengeRepo, userId);
            if(activePersonChallenge == null)
            {
                try
                {
                    await semaphore_GetOrCreateActivePersonalChallenge.WaitAsync();
                    return await CreatePersonalChallenge(personalChallengeRepo, userRepo, userId);
                }
                finally
                {
                    semaphore_GetOrCreateActivePersonalChallenge.Release();
                }
            }
            else
            {
                return activePersonChallenge;
            }
        }

        public static async Task<PersonalChallenge> GetActivePersonalChallenge(EFPersonalChallengeRepo personalChallengeRepo, int userId)
        {
            var now = DateTime.Now;

            return await personalChallengeRepo.All.Where(x => x.UserId == userId && x.EndDate > now).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        }

        // POZOR!!! Musi se volat uvnitr semaforu!!! Jinak se muzou vytvorit dve vyzvy zaroven
        private static async Task<PersonalChallenge> CreatePersonalChallenge(EFPersonalChallengeRepo personalChallengeRepo, EFUserRepo userRepo, int userId)
        {
            // Pro jistotu zkontroluji, jestli se v mezicase uz vyzva nevytvorila
            var activePersonalChallenge = await GetActivePersonalChallenge(personalChallengeRepo, userId);
            if(activePersonalChallenge != null) // vyzva se mezitim vytvorila -> vratim ji bez dalsi prace (chtel vyzvu, ma vyzvu)
            {
                return activePersonalChallenge;
            }

            var user = await userRepo.All.FirstAsync(x=>x.Id == userId);

            // ZDE JE GARANTOVANO, ZE NEMA AKTIVNI OSOBNI VYZVU


            // Docasny nebo testovaci ucet -> AUTOMATICKY mu vytvorim novou vyzvu

            // Premium ucet - kontrola, jestli ma zaplaceno
            if (user.IsPremium)
            {
                if (user.PrepaidUntil == null || user.PrepaidUntil < DateTime.Now)
                {
                    throw new NotPaidException();
                }
            }

            // Muze se stat, ze bude hrat free trial a potom se vecer rozhodne, ze si to koupi, tak se mu postupy z rana pripoctou i do nove koupene premiove vyzvy -> aspon uz uvidi postup.
            var startDate = DateTime.Now.Date;
            var endDate = startDate.AddMonths(1).Date.AddMilliseconds(-1);

            var challenge = new PersonalChallenge
            {
                UserId = user.Id,
                DailyGoal = 100,
                StartDate = startDate,
                EndDate = endDate,
            };

            personalChallengeRepo.Add(challenge);
            await personalChallengeRepo.SaveAll();

            return challenge;
        }
    }
}
