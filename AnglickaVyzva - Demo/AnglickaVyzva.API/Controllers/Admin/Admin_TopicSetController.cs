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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Controllers.Admin
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class Admin_TopicSetController : BaseController
    {
        public Admin_TopicSetController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {

        }

        public class GetList_Model
        {
            public int TopicSectionId { get; set; }
        }
        [HttpPost("getList")]
        public async Task<IActionResult> GetList(GetList_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSets = await TopicSetRepo.All.Where(x => x.TopicSectionId == model.TopicSectionId).ToListAsync();

            return Ok(new
            {
                topicSets
            });
        }

        public class GetDetail_Model { public int TopicSetId { get; set; } }
        [HttpPost("getDetail")]
        public async Task<IActionResult> GetDetail(GetDetail_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSet = await TopicSetRepo.All.FirstAsync(x => x.Id == model.TopicSetId);

            return Ok(new
            {
                topicSet
            });
        }

        public class Create_Model
        {
            public string Name { get; set; }
            public int TopicSectionId { get; set; }
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create(Create_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSection = await TopicSectionRepo.All.FirstAsync(x => x.Id == model.TopicSectionId);

            var lastOrder = await TopicSetRepo.All.Where(x => x.TopicSectionId == topicSection.Id).Select(x => x.Order).OrderByDescending(x => x).FirstOrDefaultAsync();

            var topicSet = new TopicSet
            {
                Name = model.Name,
                Order = lastOrder + 1,
                IsHidden = true,

                TopicSectionId = topicSection.Id,
            };

            TopicSetRepo.Add(topicSet);
            await SaveAll();

            return Ok();
        }

        public class UpdateName_Model
        {
            public int TopicSetId { get; set; }
            public string Name { get; set; }
        }
        [HttpPost("updateName")]
        public async Task<IActionResult> UpdateName(UpdateName_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSet = await TopicSetRepo.All.FirstAsync(x => x.Id == model.TopicSetId);

            topicSet.Name = model.Name;
            await SaveAll();

            return Ok();
        }

        public class UpdateIcon_Model
        {
            public int TopicSetId { get; set; }
            public string Icon { get; set; }
        }
        [HttpPost("updateIcon")]
        public async Task<IActionResult> UpdateIcon(UpdateIcon_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSet = await TopicSetRepo.All.FirstAsync(x => x.Id == model.TopicSetId);

            topicSet.Icon = model.Icon;
            await SaveAll();

            return Ok();
        }



        public class Delete_Model
        {
            public int TopicSetId { get; set; }
        }
        [HttpPost("delete")]
        public async Task<IActionResult> Delete(Delete_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSet = await TopicSetRepo.All.FirstAsync(x => x.Id == model.TopicSetId);

            TopicSetRepo.Delete(topicSet);
            await SaveAll();

            return Ok();
        }

        public class ImportItemsFromExcel_Model
        {
            public int TopicSetId { get; set; }
            public string FileBase64 { get; set; }
        }
        [HttpPost("importItemsFromExcel")]
        public async Task<IActionResult> ImportItemsFromExcel(ImportItemsFromExcel_Model model)
        {
            var withoutPrefix = model.FileBase64;
            if (model.FileBase64.StartsWith("data:"))
            {
                var splits = withoutPrefix.Split(";base64,", StringSplitOptions.RemoveEmptyEntries);

                if (splits.Length > 2)
                {
                    throw new Exception("V Base64 se dvakrat vyskytuje ;base64, - to je mega nahoda, ze se z random znaku nekde vygenerovalo base64. Reseni je asi trosku upravit excel. Nema cenu to programovat.");
                }

                withoutPrefix = splits[1];
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // Jinak to hazi chybu
            var ms = new MemoryStream(Convert.FromBase64String(withoutPrefix));

            await TopicHelper.ImportItemsFromExcel(model.TopicSetId, ms, TopicSetRepo, TopicItemRepo);

            return Ok();
        }

        public class SetVisibility_Model
        {
            public int TopicSetId { get; set; }
            public bool IsHidden { get; set; }
        }
        [HttpPost("setVisibility")]
        public async Task<IActionResult> Hide(SetVisibility_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSet = await TopicSetRepo.All.Where(x => x.Id == model.TopicSetId).Include(x => x.TopicItems).FirstAsync();

            topicSet.IsHidden = model.IsHidden;
            foreach (var item in topicSet.TopicItems)
            {
                item.IsHidden = model.IsHidden;
            }

            await SaveAll();


            return Ok();
        }



        public class Move_Model
        {
            public int TopicSetId { get; set; }
            public bool Up { get; set; }
        }
        [HttpPost("move")]
        public async Task<IActionResult> Move(Move_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var topicSet = await TopicSetRepo.All.FirstAsync(x => x.Id == model.TopicSetId);

            // Nahoru
            if (model.Up)
            {
                var topicSetAbove = await TopicSetRepo.All.Where(x => x.Order < topicSet.Order && x.TopicSectionId == topicSet.TopicSectionId).OrderByDescending(x => x.Order).FirstOrDefaultAsync();

                if (topicSetAbove != null)
                {
                    var tmp = topicSet.Order;
                    topicSet.Order = topicSetAbove.Order;
                    topicSetAbove.Order = tmp;
                }
            }
            else // Dolu
            {
                var topicSetBelow = await TopicSetRepo.All.Where(x => x.Order > topicSet.Order && x.TopicSectionId == topicSet.TopicSectionId).OrderBy(x => x.Order).FirstOrDefaultAsync();

                if (topicSetBelow != null)
                {
                    var tmp = topicSet.Order;
                    topicSet.Order = topicSetBelow.Order;
                    topicSetBelow.Order = tmp;
                }
            }

            await SaveAll();

            return Ok();
        }
    }
}
