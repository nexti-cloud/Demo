using AnglickaVyzva.API.DTOs;
using AnglickaVyzva.API.DTOs.Admin;
using AnglickaVyzva.API.DTOs.Topic;
using AnglickaVyzva.API.DTOs.User;
using AnglickaVyzva.API.Entities;
using AnglickaVyzva.API.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Topic = AnglickaVyzva.API.Entities.Topic;
using TopicItem = AnglickaVyzva.API.Entities.TopicItem;

namespace AnglickaVyzva.API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Username, opts => opts.MapFrom(src => src.GetLoginEmail()));
            CreateMap<Lesson, LessonDto>();
            CreateMap<Section, SectionDto>();
            CreateMap<Test, TestDto>();

            CreateMap<PromoCode, Admin_PromoCode_ListDto>();

            CreateMap<ActivationCode, Admin_ActivationCode_ListDto>();

            CreateMap<Topic, Topic_Dto>();
            CreateMap<TopicSection, TopicSection_Dto>();
            CreateMap<TopicSet, TopicSet_Dto>();
            CreateMap<TopicItem, TopicItem_Dto>();

            #region User Account
            CreateMap<User, User_ForAccountDetailDto>()
                .ForMember(dest => dest.IsCompletelyRegistered, opts => opts.MapFrom(src => src.IsCompletelyRegistered()))
                .ForMember(dest => dest.CompletelyRegisteredErrorMessage, opts => opts.MapFrom(src => src.GetCompletelyRegisteredError()))
                .ForMember(dest => dest.DisplayName, opts => opts.MapFrom(src => new string[] { 
                    src.UserName, 
                    src.GoogleEmail, 
                    src.FacebookEmail, 
                    src.AppleEmail, 
                    (src.IsTemporary ? "Zkušební účet" : null) }
                .First(x => x != null)))
            ;
            #endregion END User Account
        }
    }
}
