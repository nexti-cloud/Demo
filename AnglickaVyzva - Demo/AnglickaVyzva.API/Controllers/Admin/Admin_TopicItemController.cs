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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.TextToSpeech.V1;
using Microsoft.AspNetCore.StaticFiles;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using System.IO;

namespace AnglickaVyzva.API.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class Admin_TopicItemController : BaseController
    {
        public Admin_TopicItemController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {

        }

        public class GetList_Model
        {
            public int TopicSetId { get; set; }
        }
        [HttpPost("getList")]
        public async Task<IActionResult> GetList(GetList_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicItems = await TopicItemRepo.All.Where(x => x.TopicSetId == model.TopicSetId).ToListAsync();

            return Ok(new
            {
                topicItems
            });
        }

        public class GetDetail_Model { public int TopicItemId { get; set; } }
        [HttpPost("getDetail")]
        public async Task<IActionResult> GetDetail(GetDetail_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicItem = await TopicItemRepo.All.FirstAsync(x => x.Id == model.TopicItemId);

            return Ok(new
            {
                topicItem
            });
        }

        // Prvne se vytvori pouze ceska cast. Potom se postupne pridaji EN spravne odpovedi
        public class Create_Model
        {
            public string Cz { get; set; }
            public int TopicSetId { get; set; }
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create(Create_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var newTopicItem = new TopicItem { Cz = model.Cz, TopicSetId = model.TopicSetId };

            await TopicHelper.InsertNewTopicItem(newTopicItem, TopicSetRepo, TopicItemRepo);

            await SaveAll();

            return Ok();
        }

        public class UpdateCz_Model
        {
            public int TopicItemId { get; set; }
            public string Cz { get; set; }
        }
        [HttpPost("updateCz")]
        public async Task<IActionResult> UpdateCz(UpdateCz_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var item = await TopicItemRepo.All.FirstAsync(x => x.Id == model.TopicItemId);

            item.Cz = model.Cz;
            await SaveAll();

            return Ok();
        }

        public class UpdateNoteCz_Model
        {
            public int TopicItemId { get; set; }
            public string NoteCz { get; set; }
        }
        [HttpPost("updateNoteCz")]
        public async Task<IActionResult> UpdateNoteCz(UpdateNoteCz_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var item = await TopicItemRepo.All.FirstAsync(x => x.Id == model.TopicItemId);

            item.NoteCz = model.NoteCz;
            await SaveAll();

            return Ok();
        }

        public class UpdateEnList_Model
        {
            public int TopicItemId { get; set; }
            public List<TopicItem_En> EnList { get; set; } = new List<TopicItem_En>();
        }
        [HttpPost("updateEnList")]
        public async Task<IActionResult> UpdateEnList(UpdateEnList_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            await TopicHelper.UpdateTopicItemEnList(model.TopicItemId, model.EnList, TopicItemRepo);

            return Ok();
        }

        public class Delete_Model { public int TopicItemId { get; set; }}
        [HttpPost("delete")]
        public async Task<IActionResult> Delete(Delete_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicItem = await TopicItemRepo.All.FirstAsync(x => x.Id == model.TopicItemId);

            var items_users = await TopicItem_UserRepo.All.Where(x => x.TopicItemId == topicItem.Id).ToListAsync();

            foreach(var item_user in items_users)
            {
                TopicItem_UserRepo.Delete(item_user);
            }

            TopicItemRepo.Delete(topicItem);

            await SaveAll();

            return Ok();
        }

        public class SetVisibility_Model
        {
            public int TopicItemId { get; set; }
            public bool IsHidden { get; set; }
        }
        [HttpPost("setVisibility")]
        public async Task<IActionResult> Hide(SetVisibility_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicItem = await TopicItemRepo.All.Where(x => x.Id == model.TopicItemId).FirstAsync();

            topicItem.IsHidden = model.IsHidden;

            await SaveAll();


            return Ok();
        }

        public class Move_Model
        {
            public int TopicItemId { get; set; }
            public bool Up { get; set; }
        }
        [HttpPost("move")]
        public async Task<IActionResult> Move(Move_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicItem = await TopicItemRepo.All.FirstAsync(x => x.Id == model.TopicItemId);

            // Nahoru
            if (model.Up)
            {
                var topicItemAbove = await TopicItemRepo.All.Where(x => x.Order < topicItem.Order && x.TopicSetId == topicItem.TopicSetId).OrderByDescending(x => x.Order).FirstOrDefaultAsync();

                if (topicItemAbove != null)
                {
                    var tmp = topicItem.Order;
                    topicItem.Order = topicItemAbove.Order;
                    topicItemAbove.Order = tmp;
                }
            }
            else // Dolu
            {
                var topicItemBelow = await TopicItemRepo.All.Where(x => x.Order > topicItem.Order && x.TopicSetId == topicItem.TopicSetId).OrderBy(x => x.Order).FirstOrDefaultAsync();

                if (topicItemBelow != null)
                {
                    var tmp = topicItem.Order;
                    topicItem.Order = topicItemBelow.Order;
                    topicItemBelow.Order = tmp;
                }
            }

            await SaveAll();

            return Ok();
        }




    }
}
