using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.Entities;
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
    public class ChallengeController : BaseController
    {
        static Random random = new Random();

        public ChallengeController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {
        }

        public class StartChallenge_Model { public int ChallengeId { get; set; } public DateTime EndDate { get; set; } public int DailyGoal { get; set; } }
        public async Task<IActionResult> StartChallenge(StartChallenge_Model model)
        {
            if (model.EndDate < DateTime.Now)
                throw new Exception("Naplánovaný koncec výzvy je nastaven do minulosti.");

            if (model.DailyGoal <= 0)
                throw new Exception("Není nastaven denní cíl.");

            var relation = await ChallengeUserRelationRepo
                .All
                .Select(x => new
                {
                    x.IsAdmin,
                    x.ChallengeId,
                    x.UserId,
                })
                .FirstAsync(x=>x.ChallengeId == model.ChallengeId && x.UserId == UserId);

            if (relation.IsAdmin == false)
                throw new Exception("Nejsi správcem této výzvy. Nemůžeš ji odstartovat.");

            var challenge = await ChallengeRepo.All.FirstAsync(x => x.Id == model.ChallengeId);
            if (challenge.EndDate != null)
                throw new Exception("Tato výzva je už odstartovaná.");

            challenge.EndDate = model.EndDate;
            challenge.DailyGoal = model.DailyGoal;
            await ChallengeRepo.SaveAll();

            return Ok();
        }

        public class LeaveChallenge_Model { public int ChallengeId { get; set; } }
        public async Task<IActionResult> LeaveChallenge(LeaveChallenge_Model model)
        {
            var relation = await ChallengeUserRelationRepo.All.FirstAsync(x => x.ChallengeId == model.ChallengeId && x.UserId == UserId);
            relation.HasLeft = true;

            await ChallengeUserRelationRepo.SaveAll();

            return Ok();
        }

        public class JoinChallenge_Model { public string EnterCode { get; set; } }
        [HttpPost("joinChallenge")]
        public async Task<IActionResult> JoinChallenge(JoinChallenge_Model model)
        {
            if (string.IsNullOrWhiteSpace(model.EnterCode))
                throw new Exception("Není vyplněný vstupní kód.");

            var challenge = await ChallengeRepo
                .All
                .Select(x => new
                {
                    x.Id,
                    x.EndDate,
                    x.EnterCode,
                })
                .FirstAsync(x => x.EnterCode == x.EnterCode);

            if (await ChallengeUserRelationRepo.All.AnyAsync(x => x.ChallengeId == challenge.Id && x.UserId == UserId))
                throw new Exception("Do této výzvy se nemůžeš znovu připojit. Už v ní jsi.");

            if (challenge.EndDate != null && (DateTime)challenge.EndDate < DateTime.Now)
                throw new Exception("Tato výzva už skončila. Nemůžeš se do ní připojit.");

            var relation = new ChallengeUserRelation { UserId = UserId, ChallengeId = challenge.Id };
            ChallengeUserRelationRepo.Add(relation);
            await ChallengeUserRelationRepo.SaveAll();

            return Ok();
        }

        public class SetChallengeDescription_Model { public int ChallengeId { get; set; } public string Description { get; set; } }
        [HttpPost("setChallengeDescription")]
        public async Task<IActionResult> SetChallengeDescription(SetChallengeDescription_Model model)
        {
            if (string.IsNullOrWhiteSpace(model.Description))
                throw new Exception("Není vyplněný popis.");

            if (model.Description.Length > 5000)
                throw new Exception("Popis může být dlouhý maximálně 5 000 znaků.");

            var relation = await ChallengeUserRelationRepo.All.FirstAsync(x => x.ChallengeId == model.ChallengeId && x.UserId == UserId);

            if (relation.IsAdmin == false)
                throw new Exception("Nejste správcem výzvy.");

            var challenge = await ChallengeRepo.All.FirstAsync(x => x.Id == model.ChallengeId);
            challenge.Description = model.Description;

            await ChallengeRepo.SaveAll();

            return Ok();
        }

        public class CreateChallenge_Model { public string Name { get; set; } }
        [HttpPost("createChallenge")]
        public async Task<IActionResult> CreateChallenge(CreateChallenge_Model model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                throw new Exception("Není vyplněný název.");

            if (model.Name.Length > 50)
                throw new Exception("Název je moc dlouhý. Maximum je 50 znaků.");

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                var challenge = new Challenge { Name = model.Name, EnterCode = GenerateEnterCode() };

                while (await ChallengeRepo.All.AnyAsync(x => x.EnterCode == challenge.EnterCode))
                {
                    challenge.EnterCode = GenerateEnterCode();
                }

                ChallengeRepo.Add(challenge);
                await ChallengeRepo.SaveAll();

                // Propojim se do vyzvy jako admin
                var relation = new ChallengeUserRelation
                {
                    UserId = UserId,
                    ChallengeId = challenge.Id,
                    IsAdmin = true,
                };

                ChallengeUserRelationRepo.Add(relation);
                await ChallengeUserRelationRepo.SaveAll();

                await transaction.CommitAsync();
            }

            return Ok();
        }

        /// <summary>
        /// Neobsahuje 0 a O, aby se to nepletlo.
        /// </summary>
        /// <returns></returns>
        private static string GenerateEnterCode()
        {
            const string chars = "ABCDEFGHIJKLMNPQRSTUXZ123456789";
            return new string(Enumerable.Repeat(chars, 4)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpGet("getChallengeList")]
        public async Task<IActionResult> GetChallengeList()
        {
            var challengesIds = await ChallengeUserRelationRepo
                .All
                .Where(x => x.UserId == UserId && x.HasLeft == false)
                .Select(x => x.ChallengeId)
                .ToListAsync();

            var challenges = await ChallengeRepo
                .All
                .Where(x => challengesIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Name,
                    x.StartDate,
                    x.EndDate,
                })
                .ToListAsync();

            return Ok(new
            {
                challenges
            });
        }

        [HttpGet("getChallengeDetail/{id}")]
        public async Task<IActionResult> GetChallengeDetail(int challengeId)
        {
            if (!await ChallengeUserRelationRepo.All.AnyAsync(x =>
                x.ChallengeId == challengeId &&
                x.UserId == UserId &&
                x.HasLeft == false
            ))
            {
                return NotFound();
            }

            var challenge = await ChallengeRepo.All.FirstAsync(x => x.Id == challengeId);
            var usersIds = await ChallengeUserRelationRepo
                .All
                .Where(x => x.ChallengeId == challengeId)
                .Select(x => x.UserId)
                .ToListAsync();

            var users = await UserRepo
                .All
                .Where(x => usersIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.UserName,
                    IsMe = x.Id == UserId,
                })
                .ToListAsync();

            return Ok(new
            {
                challenge = new
                {
                    challenge.Name,
                    challenge.Description,
                    challenge.DailyGoal,
                    challenge.StartDate,
                    challenge.EndDate,
                    challenge.EnterCode,
                },
                users,
            });
        }

    }
}
