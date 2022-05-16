using AnglickaVyzva.API.Data;
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
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Controllers.Admin
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class Admin_TopicSectionController : BaseController
    {
        public Admin_TopicSectionController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {

        }

        public class GetList_Model
        {
            public int TopicId { get; set; }
        }
        [HttpPost("getList")]
        public async Task<IActionResult> GetList(GetList_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSections = await TopicSectionRepo.All.Where(x => x.TopicId == model.TopicId).ToListAsync();

            return Ok(new
            {
                topicSections
            });
        }

        public class GetDetail_Model { public int TopicSectionId { get; set; } }
        [HttpPost("getDetail")]
        public async Task<IActionResult> GetDetail(GetDetail_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSection = await TopicSectionRepo.All.FirstAsync(x => x.Id == model.TopicSectionId);

            return Ok(new
            {
                topicSection
            });
        }

        public class Create_Model
        {
            public string Name { get; set; }
            public int TopicId { get; set; }
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create(Create_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topic = await TopicRepo.All.FirstAsync(x => x.Id == model.TopicId);

            var lastOrder = await TopicSectionRepo.All.Where(x => x.TopicId == topic.Id).Select(x => x.Order).OrderByDescending(x => x).FirstOrDefaultAsync();

            var topicSection = new TopicSection
            {
                Name = model.Name,
                Order = lastOrder + 1,
                IsHidden = true,

                TopicId = topic.Id,
            };

            TopicSectionRepo.Add(topicSection);
            await SaveAll();

            return Ok();
        }

        public class UpdateName_Model
        {
            public int TopicSectionId { get; set; }
            public string Name { get; set; }
        }
        [HttpPost("updateName")]
        public async Task<IActionResult> UpdateName(UpdateName_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSection = await TopicSectionRepo.All.FirstAsync(x => x.Id == model.TopicSectionId);

            topicSection.Name = model.Name;
            await SaveAll();

            return Ok();
        }

        public class UpdateIcon_Model
        {
            public int TopicSectionId { get; set; }
            public string Icon { get; set; }
        }
        [HttpPost("updateIcon")]
        public async Task<IActionResult> UpdateIcon(UpdateIcon_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSection = await TopicSectionRepo.All.FirstAsync(x => x.Id == model.TopicSectionId);

            topicSection.Icon = model.Icon;
            await SaveAll();

            return Ok();
        }

        public class Delete_Model
        {
            public int TopicSectionId { get; set; }
        }
        [HttpPost("delete")]
        public async Task<IActionResult> Delete(Delete_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSection = await TopicSectionRepo.All.FirstAsync(x => x.Id == model.TopicSectionId);

            TopicSectionRepo.Delete(topicSection);
            await SaveAll();

            return Ok();
        }

        public class SetVisibility_Model
        {
            public int TopicSectionId { get; set; }
            public bool IsHidden { get; set; }
        }
        [HttpPost("setVisibility")]
        public async Task<IActionResult> Hide(SetVisibility_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSection = await TopicSectionRepo.All.Where(x => x.Id == model.TopicSectionId).Include(x => x.TopicSets).ThenInclude(x => x.TopicItems).FirstAsync();

            topicSection.IsHidden = model.IsHidden;
            foreach (var set in topicSection.TopicSets)
            {
                set.IsHidden = model.IsHidden;

                foreach (var item in set.TopicItems)
                {
                    item.IsHidden = model.IsHidden;
                }
            }


            _dbContext.Database.SetCommandTimeout(240); // Prodlouzim timeout na 4 minuty

            await SaveAll();

            return Ok();
        }

        public class Move_Model
        {
            public int TopicSectionId { get; set; }
            public bool Up { get; set; }
        }
        [HttpPost("move")]
        public async Task<IActionResult> Move(Move_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSection = await TopicSectionRepo.All.FirstAsync(x => x.Id == model.TopicSectionId);

            // Nahoru
            if (model.Up)
            {
                var topicSectionAbove = await TopicSectionRepo.All.Where(x => x.Order < topicSection.Order && x.TopicId == topicSection.TopicId).OrderByDescending(x => x.Order).FirstOrDefaultAsync();

                if (topicSectionAbove != null)
                {
                    var tmp = topicSection.Order;
                    topicSection.Order = topicSectionAbove.Order;
                    topicSectionAbove.Order = tmp;
                }
            }
            else // Dolu
            {
                var topicSectionBelow = await TopicSectionRepo.All.Where(x => x.Order > topicSection.Order && x.TopicId == topicSection.TopicId).OrderBy(x => x.Order).FirstOrDefaultAsync();

                if (topicSectionBelow != null)
                {
                    var tmp = topicSection.Order;
                    topicSection.Order = topicSectionBelow.Order;
                    topicSectionBelow.Order = tmp;
                }
            }

            await SaveAll();

            return Ok();
        }
    }
}