using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Controllers.Admin
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class Admin_SettingController : BaseController
    {
        public Admin_SettingController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {

        }

        [HttpPost("reloadLessonCache")]
        public async Task<IActionResult> ReloadLessonCache()
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            await LessonRepo.ReloadLessonCache();
            return Ok();
        }
    }
}
