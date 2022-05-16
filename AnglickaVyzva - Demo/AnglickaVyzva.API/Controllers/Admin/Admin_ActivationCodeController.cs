using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.DTOs.Admin;
using AnglickaVyzva.API.Entities;
using AnglickaVyzva.API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class Admin_ActivationCodeController : BaseController
    {
        static Random random = new Random();

        static SemaphoreSlim semaphore_Create = new SemaphoreSlim(1, 1);
        public static SemaphoreSlim semaphore_AssignOrDelete = new SemaphoreSlim(1, 1);

        public Admin_ActivationCodeController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {
        }

        [HttpGet("getList")]
        public async Task<IActionResult> GetActivationCodeList()
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var activationCodes = await ActivationCodeRepo.All.OrderByDescending(x => x.Id).ToListAsync();

            return Ok(new
            {
                activationCodes = _mapper.Map<List<Admin_ActivationCode_ListDto>>(activationCodes)
            });
        }

        public class CreateActivationCode_Model
        {
            public int DaysToExpire { get; set; }
            public decimal PercentageSale { get; set; }
            public bool IsForYear { get; set; }
            public bool IsForMonth { get; set; }
        }
        [HttpPost("createActivationCode")]
        public async Task<IActionResult> CreateActivationCode(CreateActivationCode_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            if (model.IsForYear == false && model.IsForMonth == false)
            {
                return BadRequest("Aktivační kód nemá nastaven ani jeden typ produktu (rok, měsíc)");
            }

            if(model.IsForYear && model.IsForMonth)
            {
                return BadRequest("Aktivační kód má nastaveno více než jeden typ produktu (rok, měsíc)");
            }

            try
            {
                await semaphore_Create.WaitAsync();



                if (model.DaysToExpire <= 0)
                {
                    return BadRequest("Počet dnů do vypršení musí být větší než nula");
                }

                const string chars = "ABCDEFGHJKLMNPRSTUVWXYZ23456789";

                string code = null;

                while (true)
                {
                    var first = new string(Enumerable.Repeat(chars, 3)
                                                            .Select(s => s[random.Next(s.Length)]).ToArray());
                    var second = new string(Enumerable.Repeat(chars, 3)
                                                            .Select(s => s[random.Next(s.Length)]).ToArray());
                    var third = new string(Enumerable.Repeat(chars, 3)
                                                            .Select(s => s[random.Next(s.Length)]).ToArray());
                    var fourth = new string(Enumerable.Repeat(chars, 3)
                                                            .Select(s => s[random.Next(s.Length)]).ToArray());

                    code = $"{first}-{second}-{third}-{fourth}";

                    if (!await PromoCodeRepo.All.AnyAsync(x => x.Code == code) && !await ActivationCodeRepo.All.AnyAsync(x => x.Code == code))
                    {
                        break;
                    }
                }

                var activationCode = new ActivationCode
                {
                    ExpirationDate = DateTime.Today.AddDays(model.DaysToExpire + 1).AddMilliseconds(-1), // Napr: Chci aby to platilo MINIMALNE 3 dny. Odecitani milisekundy je tam proto, aby se to na webu zobrazovalo intuitivne
                    Code = code,
                    IsForYear = model.IsForYear,
                    IsForMonth = model.IsForMonth,
                };

                ActivationCodeRepo.Add(activationCode);
                await SaveAll();

            }
            finally
            {
                semaphore_Create.Release();
            }


            return Ok();
        }

        public class UpdateNote_Model
        {
            public int ActivationCodeId { get; set; }
            public string Note { get; set; }
        }
        [HttpPost("updateNote")]
        public async Task<IActionResult> UpdateNote(UpdateNote_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var promo = await ActivationCodeRepo.All.FirstAsync(x => x.Id == model.ActivationCodeId);

            promo.Note = model.Note;

            await SaveAll();

            return Ok();
        }

        public class DeleteActivationCode_Model
        {
            public int ActivationCodeId { get; set; }
        }
        [HttpPost("deleteActivationCode")]
        public async Task<IActionResult> DeleteActivationCode(DeleteActivationCode_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            try
            {
                await semaphore_AssignOrDelete.WaitAsync();

                var promo = await ActivationCodeRepo.All.FirstAsync(x => x.Id == model.ActivationCodeId);

                if (promo.IsActivated)
                {
                    return BadRequest("Nelze smazat použitý aktivační kód");
                }

                ActivationCodeRepo.Delete(promo);

                await SaveAll();

            }
            finally
            {
                semaphore_AssignOrDelete.Release();
            }


            return Ok();
        }

        [HttpPost("deleteExpiredAndNotActivatedActivationCodes")]
        public async Task<IActionResult> DeleteExpiredAndNotActivatedActivationCodes()
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            try
            {
                await semaphore_AssignOrDelete.WaitAsync();

                var now = DateTime.Now;

                var promos = await ActivationCodeRepo.All.Where(x => x.IsActivated == false && x.ExpirationDate < now).ToListAsync();

                var count = promos.Count;

                foreach (var promo in promos)
                {
                    ActivationCodeRepo.Delete(promo);
                }

                await SaveAll();

                return Ok(new
                {
                    deletedCount = count
                });
            }
            finally
            {
                semaphore_AssignOrDelete.Release();
            }
        }
    }
}
