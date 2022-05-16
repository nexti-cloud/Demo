using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Controllers.Admin
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class Admin_OrderController : BaseController
    {
        public Admin_OrderController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {
        }

        public class GetDetail_Model { public int OrderId { get; set; } }
        [HttpPost("getDetail")]
        public async Task<IActionResult> GetDetail(GetDetail_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var order = await OrderRepo.All.FirstAsync(x => x.Id == model.OrderId);

            return Ok(new
            {
                order
            });
        }
    }
}
