using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.Entities;
using AnglickaVyzva.API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using static AnglickaVyzva.API.Helpers.EmailHelper;

namespace AnglickaVyzva.API.Controllers
{
    public class BaseController : ControllerBase
    {
        protected readonly IConfiguration _config;
        protected readonly IMapper _mapper;
        protected readonly DataContext _dbContext;
        protected readonly IWebHostEnvironment _env;

        public BaseController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env)
        {
            _config = config;
            _mapper = mapper;
            _dbContext = dbContext;
            _env = env;
        }

        protected SaveParameters SaveParameters
        {
            get
            {
                var saveParameters = new SaveParameters
                {
                    UserIp = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                    UserName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                    Host = Request.Host.Host
                };


                return saveParameters;
            }
        }



        protected ServerEmailCredentials ServerEmailCredentials
        {
            get
            {
                var serverEmailCredentials = new EmailHelper.ServerEmailCredentials(_config["Email-Login"], _config["Email-Password"]);
                return serverEmailCredentials;
            }
        }

        protected bool IsOnRemoteDebug()
        {
            var isDebug = Environment.GetEnvironmentVariable("isOnRemoteDev");

            if(isDebug == "true")
            {
                return true;
            }
            return false;
        }

        private EFUserRepo _userRepo;
        public EFUserRepo UserRepo { get { return _userRepo == null ? (_userRepo = new EFUserRepo(_dbContext, SaveParameters)) : _userRepo; } }

        private EFProgressRepo _progressRepo;
        public EFProgressRepo ProgressRepo { get { return _progressRepo == null ? (_progressRepo = new EFProgressRepo(_dbContext, SaveParameters)) : _progressRepo; } }

        private EFTestProgressRepo _testProgressRepo;
        public EFTestProgressRepo TestProgressRepo { get { return _testProgressRepo == null ? (_testProgressRepo = new EFTestProgressRepo(_dbContext, SaveParameters)) : _testProgressRepo; } }

        private EFChestPointsUsesRepo _chestPointsUsesRepo;
        public EFChestPointsUsesRepo ChestPointsUsesRepo { get { return _chestPointsUsesRepo == null ? (_chestPointsUsesRepo = new EFChestPointsUsesRepo(_dbContext, SaveParameters)) : _chestPointsUsesRepo; } }

        private EFLessonRepo _lessonRepo;
        public EFLessonRepo LessonRepo { get { return _lessonRepo == null ? (_lessonRepo = new EFLessonRepo(_env)) : _lessonRepo; } }

        private EFLogRepo _logRepo;
        public EFLogRepo LogRepo { get { return _logRepo == null ? (_logRepo = new EFLogRepo(_dbContext, SaveParameters)) : _logRepo; } }

        private EFEmailRepo _emailRepo;
        public EFEmailRepo EmailRepo { get { return _emailRepo == null ? (_emailRepo = new EFEmailRepo(_dbContext, SaveParameters)) : _emailRepo; } }

        private EFNotificationTokenRepo _notificationTokenRepo;
        public EFNotificationTokenRepo NotificationTokenRepo { get { return _notificationTokenRepo == null ? (_notificationTokenRepo = new EFNotificationTokenRepo(_dbContext, SaveParameters)) : _notificationTokenRepo; } }
        private EFFeedbackRepo _feedbackRepo;
        public EFFeedbackRepo FeedbackRepo { get { return _feedbackRepo == null ? (_feedbackRepo = new EFFeedbackRepo(_dbContext, SaveParameters)) : _feedbackRepo; } }
        private EFChallengeRepo _challengeRepo;
        public EFChallengeRepo ChallengeRepo { get { return _challengeRepo == null ? (_challengeRepo = new EFChallengeRepo(_dbContext, SaveParameters)) : _challengeRepo; } }
        private EFChallengeUserRelationRepo _challengeUserRelationRepo;
        public EFChallengeUserRelationRepo ChallengeUserRelationRepo { get { return _challengeUserRelationRepo == null ? (_challengeUserRelationRepo = new EFChallengeUserRelationRepo(_dbContext, SaveParameters)) : _challengeUserRelationRepo; } }
        private EFPersonalChallengeRepo _personalChallengeRepo;
        public EFPersonalChallengeRepo PersonalChallengeRepo { get { return _personalChallengeRepo == null ? (_personalChallengeRepo = new EFPersonalChallengeRepo(_dbContext, SaveParameters)) : _personalChallengeRepo; } }

        private EFOrderRepo _orderRepo;
        public EFOrderRepo OrderRepo { get { return _orderRepo == null ? (_orderRepo = new EFOrderRepo(_dbContext, SaveParameters)) : _orderRepo; } }

        private EFInvoiceRepo _invoiceRepo;
        public EFInvoiceRepo InvoiceRepo { get { return _invoiceRepo == null ? (_invoiceRepo = new EFInvoiceRepo(_dbContext, SaveParameters)) : _invoiceRepo; } }

        private EFPromoCodeRepo _promoCodeRepo;
        public EFPromoCodeRepo PromoCodeRepo { get { return _promoCodeRepo == null ? (_promoCodeRepo = new EFPromoCodeRepo(_dbContext, SaveParameters)) : _promoCodeRepo; } }

        private EFActivationCodeRepo _activationCodeRepo;
        public EFActivationCodeRepo ActivationCodeRepo { get { return _activationCodeRepo == null ? (_activationCodeRepo = new EFActivationCodeRepo(_dbContext, SaveParameters)) : _activationCodeRepo; } }

        private EFTopicRepo _topicRepo;
        public EFTopicRepo TopicRepo { get { return _topicRepo == null ? (_topicRepo = new EFTopicRepo(_dbContext, SaveParameters)) : _topicRepo; } }

        private EFTopicItemRepo _topicItemRepo;
        public EFTopicItemRepo TopicItemRepo { get { return _topicItemRepo == null ? (_topicItemRepo = new EFTopicItemRepo(_dbContext, SaveParameters)) : _topicItemRepo; } }

        private EFTopicItem_UserRepo _topicItem_userRepo;
        public EFTopicItem_UserRepo TopicItem_UserRepo { get { return _topicItem_userRepo == null ? (_topicItem_userRepo = new EFTopicItem_UserRepo(_dbContext, SaveParameters)) : _topicItem_userRepo; } }

        private EFTopicPointsRepo _topicPointsRepo;
        public EFTopicPointsRepo TopicPointsRepo { get { return _topicPointsRepo == null ? (_topicPointsRepo = new EFTopicPointsRepo(_dbContext, SaveParameters)) : _topicPointsRepo; } }

        private EFTopicSectionRepo _topicSectionRepo;
        public EFTopicSectionRepo TopicSectionRepo { get { return _topicSectionRepo == null ? (_topicSectionRepo = new EFTopicSectionRepo(_dbContext, SaveParameters)) : _topicSectionRepo; } }

        private EFTopicSetRepo _topicSet;
        public EFTopicSetRepo TopicSetRepo { get { return _topicSet == null ? (_topicSet = new EFTopicSetRepo(_dbContext, SaveParameters)) : _topicSet; } }

        public async Task SaveAll()
        {
            await BaseRepoDefault.SaveAll(SaveParameters, _dbContext);
        }

        private User _loggedUser;
        public async Task<User> GetLoggedUser()
        {
            if (_loggedUser == null)
            {
                var userId = UserIdOrDefault;
                if (userId == null)
                    return null;

                _loggedUser = await UserRepo.All.FirstAsync(x => x.Id == UserId);
            }

            return _loggedUser;
        }
        public int UserId
        {
            get { return int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value); ; }
        }
        protected int? UserIdOrDefault
        {
            get
            {
                if (int.TryParse(this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out _))
                {
                    return UserId;
                }
                return null;
            }
        }
    }
}