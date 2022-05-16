using AnglickaVyzva.API.Data;
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
    public class Admin_FeedbackController : BaseController
    {
        public Admin_FeedbackController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {

        }
        
        [HttpPost("getList")]
        public async Task<IActionResult> GetList()
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var feedbacks = await FeedbackRepo.All.OrderByDescending(x => x.Id).ToListAsync();

            return Ok(new
            {
                feedbacks,
            });
        }
    }
}
