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
            CreateMap<Account, ProfileResponse>()
                .ForMember(dest => dest.AvtUrl, opt => opt.MapFrom(src => src.AvatarUrl));

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
                opt => opt.MapFrom(src => src.Location.LocationName))
                .ForMember(dest => dest.Preferences,
                    opt => opt.MapFrom(src =>
                        src.PoiPreferences
                            .Where(x => x.Preference != null)
                            .Select(x => x.Preference.Name)
                            .ToList()));

            CreateMap<POI, RecommendedPoiResponse>()
                .ForMember(dest => dest.POIPreferences,
                    opt => opt.MapFrom(src =>
                        src.PoiPreferences
                            .Where(x => x.Preference != null)
                            .Select(x => x.Preference.Name)
                            .ToList()));

            //Location
            CreateMap<CreatePoiRequest, POI>()
                .ForMember(dest => dest.PoiPreferences, opt => opt.Ignore());
            CreateMap<UpdatePoiRequest, POI>()
                .ForMember(dest => dest.PoiPreferences, opt => opt.Ignore());
            CreateMap<Location, DTOs.Responses.LocationResponse>();

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

            //District
            CreateMap<District, DistrictResponse>();

            //Participant
            CreateMap<Participant, ParticipantResponse>()
            .ForMember(dest => dest.UserEmail,
                opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Role,
                opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.PhoneNumber,
                opt => opt.MapFrom(src => src.User.PhoneNumber))
            .ForMember(dest => dest.Gender,
                opt => opt.MapFrom(src => src.User.Gender))
            .ForMember(dest => dest.AvatarUrl,
                opt => opt.MapFrom(src => src.User.AvatarUrl));


            // 🔥 ROOT
            CreateMap<List<TripSegment>, FullTripResponse>()
                .ForMember(dest => dest.Segments, opt => opt.MapFrom(src => src));

            // 🔥 SEGMENT
            CreateMap<TripSegment, SegmentResponse>()
                .ForMember(dest => dest.Days, opt => opt.MapFrom(src =>
                    src.Itineraries!
                       .SelectMany(i => i.ItineraryDetails!)
                       .GroupBy(d => d.VisitDate.Date)
                ));

            // 🔥 GROUP → DAY
            CreateMap<IGrouping<DateTime, ItineraryDetail>, DayPlanResponse>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Key))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src));

            // 🔥 DETAIL → ITEM
            CreateMap<ItineraryDetail, ItineraryItemResponse>()
                .ForMember(dest => dest.Period, opt => opt.MapFrom(src =>
                    src.StartTime.Hour < 11 ? "Morning" :
                    src.StartTime.Hour < 15 ? "Noon" :
                    "Evening"
                ))

                .ForMember(dest => dest.PoiName, opt => opt.MapFrom(src => src.POI!.Name))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.POI!.Address))
                .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.POI!.Location.LocationName))
                .ForMember(dest => dest.IsIndoor, opt => opt.MapFrom(src => src.POI!.IsIndoor))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.WeatherRiskScore, opt => opt.MapFrom(src => src.WeatherRiskScore));
        }
    }
}
