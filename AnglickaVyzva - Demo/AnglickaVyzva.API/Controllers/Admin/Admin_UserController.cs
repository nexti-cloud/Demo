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
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Controllers.Admin
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class Admin_UserController : BaseController
    {
        public Admin_UserController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {
        }

        [HttpPost("GetList")]
        public async Task<IActionResult> GetList()
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var sevenDaysBack = DateTime.Today.AddDays(-7);
            var thirtyDaysBack = DateTime.Today.AddDays(-30);

            var users = await UserRepo.All.ToListAsync();
            var allProgressesLastMonth = await ProgressRepo.All
                                                            .Where(x => x.CreatedDate >= thirtyDaysBack)
                                                            .Select(x => new
                                                            {
                                                                UserId = x.UserId,
                                                                Created = x.Created,
                                                                Points = x.Points,
                                                                LessonOrder = x.LessonOrder,
                                                                SectionOrder = x.SectionOrder,
                                                                ExerciseOrder = x.ExerciseOrder,
                                                            })
                                                            .ToListAsync();
            var allTestProgressesLastMonth = await TestProgressRepo.All.Where(x=>x.CreatedDate >= thirtyDaysBack).ToListAsync();
            var allTopicProgressesLastMonth = await TopicPointsRepo.All.Where(x => x.Created >= thirtyDaysBack).ToListAsync();

            var usersDto = new List<Admin_User_ListDto>();

            var today = DateTime.Today;
            var yesterday = DateTime.Today.AddDays(-1);
            var twoDaysAgo = DateTime.Today.AddDays(-2);

            

            foreach(var user in users)
            {
                var progresses = allProgressesLastMonth.Where(x => x.UserId == user.Id).ToList();
                var doneTestProgresses = allTestProgressesLastMonth.Where(x => x.UserId == user.Id && x.Percentage >= TestProgress.ThresholdPoints).ToList();
                var topicProgresses = allTopicProgressesLastMonth.Where(x => x.UserId == user.Id).ToList();

                var pointsToday = progresses.Where(x=>x.Created.Date == today).Sum(x => x.Points) + doneTestProgresses.Where(x=>x.Created.Date == today).Sum(x => x.Points) + topicProgresses.Where(x => x.Created.Date == today).Sum(x => x.Points);

                var pointsYesterday = progresses.Where(x => x.Created.Date == yesterday).Sum(x => x.Points) + doneTestProgresses.Where(x => x.Created.Date == yesterday).Sum(x => x.Points) + topicProgresses.Where(x => x.Created.Date == yesterday).Sum(x => x.Points);

                var pointsTwoDaysAgo = progresses.Where(x => x.Created.Date == twoDaysAgo).Sum(x => x.Points) + doneTestProgresses.Where(x => x.Created.Date == twoDaysAgo).Sum(x => x.Points) + topicProgresses.Where(x => x.Created.Date == twoDaysAgo).Sum(x => x.Points);


                var pointsSevenDays = progresses.Where(x => x.Created.Date >= sevenDaysBack).Sum(x => x.Points) + doneTestProgresses.Where(x => x.Created.Date >= sevenDaysBack).Sum(x => x.Points) + topicProgresses.Where(x => x.Created.Date >= sevenDaysBack).Sum(x => x.Points);

                var pointsThirtyDays = progresses.Where(x => x.Created.Date >= thirtyDaysBack).Sum(x => x.Points) + doneTestProgresses.Where(x => x.Created.Date >= thirtyDaysBack).Sum(x => x.Points) + topicProgresses.Where(x => x.Created.Date >= thirtyDaysBack).Sum(x => x.Points);

                var furthestProgress = progresses.OrderByDescending(x => x.LessonOrder).ThenByDescending(x => x.SectionOrder).ThenByDescending(x => x.ExerciseOrder).FirstOrDefault();

                var furthestTestProgress = doneTestProgresses.OrderByDescending(x => x.LessonOrder).ThenByDescending(x => x.TestOrder).FirstOrDefault();

                var pointsOfDays = new List<int>();


                var processingDay = today;
                for(int i=0; i < 7; i++)
                {
                    var dayProgresses = progresses.Where(x => x.Created.Date == processingDay).ToList();
                    var dayDoneTestProgresses = doneTestProgresses.Where(x => x.Created.Date == processingDay).ToList();
                    var dayTopicProgresses = topicProgresses.Where(x => x.Created.Date == processingDay).ToList();
                    pointsOfDays.Add(dayProgresses.Sum(x => x.Points) + dayDoneTestProgresses.Sum(x=>x.Points) + dayTopicProgresses.Sum(x=>x.Points));

                    processingDay = processingDay.AddDays(-1);
                }


                var userDto = new Admin_User_ListDto
                {
                    Id = user.Id,
                    LoginEmail = user.GetLoginEmail(),
                    LoginMethod = user.LoginMethod,
                    IsTemporary = user.IsTemporary,
                    IsUserNameVerified = user.IsUserNameVerified,
                    CreatedDate = user.CreatedDate,
                    
                    IsPremium = user.IsPremium,
                    DoNotRenewSubscription = user.DoNotRenewSubscription,
                    SubscriptionType = user.SubscriptionType,

                    PrepaidUntil = user.PrepaidUntil,

                    Note = user.Note,

                    PointsToday = pointsToday,
                    PointsYesterday = pointsYesterday,
                    PointsTwoDaysAgo = pointsTwoDaysAgo,

                    PointsLast7Days = pointsSevenDays,
                    PointsLast30Days = pointsThirtyDays,

                    LessonOrder = furthestProgress?.LessonOrder,
                    SectionOrder = furthestProgress?.SectionOrder,
                    ExerciseOrder = furthestProgress?.ExerciseOrder,

                    TestLessonOrder = furthestTestProgress?.LessonOrder,
                    TestOrder = furthestTestProgress?.TestOrder,

                    PointsOfDays = pointsOfDays,
                };

                usersDto.Add(userDto);
            }

            return Ok(new
            {
                users = usersDto
            });
        }

        public class UpdateNode_Model
        {
            public int UserId { get; set; }
            public string Note { get; set; }
        }
        [HttpPost("updateNote")]
        public async Task<IActionResult> UpdateNode(UpdateNode_Model model)
        {
            AdminHelper.CheckAuthorization(await GetLoggedUser());

            var user = await UserRepo.All.FirstAsync(x => x.Id == model.UserId);

            user.Note = model.Note;

            await SaveAll();

            return Ok();
        }
    }
}
