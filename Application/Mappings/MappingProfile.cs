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
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

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
                .ForMember(dest => dest.PackageTitle, opt => opt.MapFrom(src => src.SubscriptionPackage != null ? src.SubscriptionPackage.Title : string.Empty))
                .ForMember(dest => dest.PackageStatus, opt => opt.MapFrom(src => src.SubscriptionPackage != null ? src.SubscriptionPackage.Status : string.Empty))
                .ForMember(dest => dest.DurationDays, opt => opt.MapFrom(src => src.SubscriptionPackage != null ? src.SubscriptionPackage.DurationDays : 0))
                .ForMember(dest => dest.ExpiredAt, opt => opt.MapFrom(src => src.SubscriptionPackage != null ? src.CreatedAt.AddDays(src.SubscriptionPackage.DurationDays) : (DateTime?)null));

            //Advertisement
            CreateMap<CreateAdvertisementRequest, Advertisement>();
            CreateMap<Promotion, PromotionSummaryResponse>();
            CreateMap<Advertisement, AdvertisementResponse>()
                .ForMember(dest => dest.Promotion,
                    opt => opt.MapFrom(src => src.Promotion));

            CreateMap<SavedPromotion, SavedPromotionResponse>()
                .ForMember(dest => dest.AdId,
                    opt => opt.MapFrom(src => src.Promotion != null ? src.Promotion.AdId : Guid.Empty))
                .ForMember(dest => dest.PromotionTitle,
                    opt => opt.MapFrom(src => src.Promotion != null ? src.Promotion.Title : string.Empty))
                .ForMember(dest => dest.AdvertisementTitle,
                    opt => opt.MapFrom(src => src.Promotion != null && src.Promotion.Advertisement != null ? src.Promotion.Advertisement.Title : string.Empty));

            //POI
            CreateMap<POI, PoiResponse>()
                .ForMember(dest => dest.LocationName,
                opt => opt.MapFrom(src => src.Location.LocationName));

            CreateMap<POI, RecommendedPoiResponse>()
                .ForMember(dest => dest.POIPreferences,
                    opt => opt.MapFrom(src =>
                        src.PoiPreferences
                            .Where(x => x.Preference != null)
                            .Select(x => x.Preference.Name)
                            .ToList()));

            //Location
            CreateMap<CreatePoiRequest, POI>();
            CreateMap<UpdatePoiRequest, POI>();

            //Trip Risk Profile
            CreateMap<Trip, TripRiskContextResponse>();

            CreateMap<TripSegment, SegmentRiskContextResponse>();

            CreateMap<ItineraryDetail, ItineraryRiskContextResponse>()
                .ForMember(d => d.StoredRiskScore,
                    opt => opt.MapFrom(s => s.WeatherRiskScore));

            //Trip
            CreateMap<TripRequest, Trip>();
            CreateMap<Trip, TripResponse>();

            //Segment
            CreateMap<AddTripSegmentRequest, TripSegment>();

            CreateMap<TripSegment, TripSegmentResponse>();
        }
    }
}
