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
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Controllers.Admin
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class Admin_TopicController : BaseController
    {
        public Admin_TopicController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {

        }

        public class GetList_Model { }
        [HttpPost("getList")]
        public async Task<IActionResult> GetList(GetList_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topics = await TopicRepo.All.ToListAsync();

            return Ok(new
            {
                topics
            });
        }

        public class GetDetail_Model { public int TopicId { get; set; } }
        [HttpPost("getDetail")]
        public async Task<IActionResult> GetDetail(GetDetail_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topic = await TopicRepo.All.FirstAsync(x=>x.Id == model.TopicId);

            return Ok(new
            {
                topic
            });
        }

        public class Create_Model
        {
            public string Name { get; set; }
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create(Create_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var lastOrder = await TopicRepo.All.Select(x => x.Order).OrderByDescending(x => x).FirstOrDefaultAsync(); // Default intu je 0


            var topic = new Topic
            {
                Name = model.Name,
                Order = lastOrder + 1,
                IsHidden = true,
            };

            TopicRepo.Add(topic);
            await SaveAll();

            return Ok();
        }

        public class UpdateName_Model
        {
            public int TopicId { get; set; }
            public string Name { get; set; }
        }
        [HttpPost("updateName")]
        public async Task<IActionResult> UpdateName(UpdateName_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topic = await TopicRepo.All.FirstAsync(x => x.Id == model.TopicId);

            topic.Name = model.Name;
            await SaveAll();

            return Ok();
        }

        public class UpdateIcon_Model
        {
            public int TopicId { get; set; }
            public string Icon { get; set; }
        }
        [HttpPost("updateIcon")]
        public async Task<IActionResult> UpdateIcon(UpdateIcon_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topic = await TopicRepo.All.FirstAsync(x => x.Id == model.TopicId);

            topic.Icon = model.Icon;
            await SaveAll();

            return Ok();
        }

        public class SetVisibility_Model
        {
            public int TopicId { get; set; }
            public bool IsHidden { get; set; }
        }
        [HttpPost("setVisibility")]
        public async Task<IActionResult> Hide(SetVisibility_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topic = await TopicRepo.All.Where(x => x.Id == model.TopicId).Include(x => x.TopicSections).ThenInclude(x => x.TopicSets).ThenInclude(x => x.TopicItems).FirstAsync();

            topic.IsHidden = model.IsHidden;
            foreach (var section in topic.TopicSections)
            {
                section.IsHidden = model.IsHidden;

                foreach (var set in section.TopicSets)
                {
                    set.IsHidden = model.IsHidden;

                    foreach (var item in set.TopicItems)
                    {
                        item.IsHidden = model.IsHidden;
                    }
                }
            }

            await SaveAll();


            return Ok();
        }

        public class Move_Model
        {
            public int TopicId { get; set; }
            public bool Up { get; set; }
        }
        [HttpPost("move")]
        public async Task<IActionResult> Move(Move_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topic = await TopicRepo.All.FirstAsync(x => x.Id == model.TopicId);

            // Nahoru
            if (model.Up)
            {
                var topicAbove = await TopicRepo.All.Where(x => x.Order < topic.Order).OrderByDescending(x => x.Order).FirstOrDefaultAsync();

                if(topicAbove != null)
                {
                    var tmp = topic.Order;
                    topic.Order = topicAbove.Order;
                    topicAbove.Order = tmp;
                }
            }
            else // Dolu
            {
                var topicBelow = await TopicRepo.All.Where(x => x.Order > topic.Order).OrderBy(x => x.Order).FirstOrDefaultAsync();

                if (topicBelow != null)
                {
                    var tmp = topic.Order;
                    topic.Order = topicBelow.Order;
                    topicBelow.Order = tmp;
                }
            }

            await SaveAll();

            return Ok();
        }
    }
}
