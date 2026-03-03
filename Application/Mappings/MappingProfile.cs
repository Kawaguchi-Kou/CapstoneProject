using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile() {
            //Auth
            CreateMap<RegisterRequest, Account>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password));

            CreateMap<RegisterRequest, CreateProfileRequest>();

            CreateMap<Account, AccountResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role!.Name))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            //User
            //CreateMap<UpdateUserRequest, User>();
            CreateMap<CreateProfileRequest, Account>();

            CreateMap<UpdateUserRequest, Account>();

            CreateMap<Account, UserResponse>();
            CreateMap<Account, ProfileResponse>();

            //Preference
             CreateMap<UserPreferenceItem, UserPreference>();
             CreateMap<UserPreference, UserPreferenceResponse>()
                .ForMember(dest => dest.PreferenceName,
                opt => opt.MapFrom(src => src.Preference)); ;
            //AdSubscriptionPackage
            CreateMap<CreateAdSubscriptionPackageRequest, AdSubscriptionPackage>();
            CreateMap<AdSubscriptionPackage, AdSubscriptionPackageResponse>();

            //AccountSubscription
            CreateMap<AccountSubscription, AccountSubscriptionResponse>()
                .ForMember(dest => dest.PackageTitle, opt => opt.MapFrom(src => src.SubscriptionPackage != null ? src.SubscriptionPackage.Title : string.Empty));

            //Advertisement
            CreateMap<CreateAdvertisementRequest, Advertisement>();
            CreateMap<Advertisement, AdvertisementResponse>();
        }
    }
}
