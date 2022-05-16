using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.DTOs.Admin;
using AnglickaVyzva.API.Entities;
using AnglickaVyzva.API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class Admin_PromoCodeController : BaseController
    {
        static Random random = new Random();

        static SemaphoreSlim semaphore_Create = new SemaphoreSlim(1, 1);
        public static SemaphoreSlim semaphore_AssignOrDelete = new SemaphoreSlim(1, 1);

        public Admin_PromoCodeController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {
        }

        [HttpGet("getList")]
        public async Task<IActionResult> GetPromoCodeList()
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var promoCodes = await PromoCodeRepo.All.OrderByDescending(x => x.Id).ToListAsync();

            return Ok(new
            {
                promoCodes = _mapper.Map<List<Admin_PromoCode_ListDto>>(promoCodes)
            });
        } 

        public class CreatePromoCode_Model
        {
            public int DaysToExpire { get; set; }
            public decimal PercentageSale { get; set; }
            public bool IsForYear { get; set; }
            public bool IsForGift { get; set; }
            public bool IsForMonth { get; set; }
        }
        [HttpPost("createPromoCode")]
        public async Task<IActionResult> CreatePromoCode(CreatePromoCode_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            if(model.IsForYear == false && model.IsForGift == false && model.IsForMonth == false)
            {
                return BadRequest("Promokód nemá povolenu ani jednu metodu (rok, dárek, měsíc)");
            }

            try
            {
                await semaphore_Create.WaitAsync();



                if (model.DaysToExpire <= 0)
                {
                    return BadRequest("Počet dnů do vypršení musí být větší než nula");
                }

                if (model.PercentageSale <= 0)
                {
                    return BadRequest("Procenta slevy musí být větší než nula");
                }

                if (model.PercentageSale >= 100)
                {
                    return BadRequest("Procenta slevy musí být menší než 100");
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

                    
                    if (!await PromoCodeRepo.All.AnyAsync(x => x.Code == code) && !await ActivationCodeRepo.All.AnyAsync(x=>x.Code == code))
                    {
                        break;
                    }
                }

                var promo = new PromoCode
                {
                    ExpirationDate = DateTime.Today.AddDays(model.DaysToExpire+1).AddMilliseconds(-1), // Napr: Chci aby to platilo MINIMALNE 3 dny. Odecitani milisekundy je tam proto, aby se to na webu zobrazovalo intuitivne
                    PercentageSale = model.PercentageSale,
                    Code = code,
                    IsForYear = model.IsForYear,
                    IsForGift = model.IsForGift,
                    IsForMonth = model.IsForMonth,
                };

                PromoCodeRepo.Add(promo);
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
            public int PromoCodeId { get; set; }
            public string Note { get; set; }
        }
        [HttpPost("updateNote")]
        public async Task<IActionResult> UpdateNote(UpdateNote_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var promo = await PromoCodeRepo.All.FirstAsync(x => x.Id == model.PromoCodeId);

            promo.Note = model.Note;

            await SaveAll();

            return Ok();
        }

        public class DeletePromoCode_Model
        {
            public int PromoCodeId { get; set; }
        }
        [HttpPost("deletePromoCode")]
        public async Task<IActionResult> DeletePromoCode(DeletePromoCode_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            try
            {
                await semaphore_AssignOrDelete.WaitAsync();

                var promo = await PromoCodeRepo.All.FirstAsync(x => x.Id == model.PromoCodeId);

                if (promo.IsActivated)
                {
                    return BadRequest("Nelze smazat použitý promo kód");
                }

                PromoCodeRepo.Delete(promo);

                await SaveAll();

            }
            finally
            {
                semaphore_AssignOrDelete.Release();
            }


            return Ok();
        }

        [HttpPost("deleteExpiredAndNotActivatedPromoCodes")]
        public async Task<IActionResult> DeleteExpiredAndNotActivatedPromoCodes()
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            try
            {
                await semaphore_AssignOrDelete.WaitAsync();

                var now = DateTime.Now;

                var promos = await PromoCodeRepo.All.Where(x => x.IsActivated == false && x.ExpirationDate < now).ToListAsync();

                var count = promos.Count;

                foreach(var promo in promos)
                {
                    PromoCodeRepo.Delete(promo);
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
