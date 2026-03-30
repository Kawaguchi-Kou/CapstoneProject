using System;
using System.Security.Claims;
using System.Text;
using Application.Hubs;
using Application.Interfaces;
using Application.Mappings;
using Application.Services;
using Domain.Interfaces;
using Domain.Weather;
using DotNetEnv;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.BackgroundJobs;
using Infrastructure.EntitiesConfigurations;
using Infrastructure.ExternalApis.OpenMeteo;
using Infrastructure.ExternalApis.SePay;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using WebAPI.Converters;
using System.Text.Json.Serialization;


//load .env
Env.Load();

System.Net.ServicePointManager.SecurityProtocol =
    System.Net.SecurityProtocolType.Tls12;

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.License.SetNonCommercialPersonal("CapstoneProject");


// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Supabase")));

//Hangfire configuration 
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(
            builder.Configuration.GetConnectionString("Supabase")
        )
    ));
builder.Services.AddHangfireServer();

// =====================
// WEATHER - OPEN METEO
// =====================
builder.Services.Configure<OpenMeteoOptions>(
    builder.Configuration.GetSection(OpenMeteoOptions.SectionName));

builder.Services.AddHttpClient<IOpenMeteoService, OpenMeteoService>();

//======================
//GEMINI - GENERATIVE AI
//======================
builder.Configuration["Gemini:ApiKey"] =
    Environment.GetEnvironmentVariable("GEMINI_API_KEY");


// =====================
// SEPAY - PAYMENT
// =====================
builder.Services.Configure<SePayOptions>(
    builder.Configuration.GetSection(SePayOptions.SectionName));

builder.Services.AddScoped<ISePayService, SePayService>();

//=====================
// GEOCODING - MAPBOX
//=====================
builder.Services
    .AddHttpClient<IGeocodingService, MapboxGeocodingService>();


// Add services 
//Auth
builder.Services.AddScoped<IAuthService, AuthServices>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

//User
builder.Services.AddScoped<IUserService, UsersService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// Preference
builder.Services.AddScoped<IPreferenceService, PreferenceService>();

//POI
builder.Services.AddScoped<IPOIService, POIService>();

//AdSubscriptionPackage
builder.Services.AddScoped<IAdSubscriptionPackageService, AdSubscriptionPackageService>();

//AccountSubscription
builder.Services.AddScoped<IAccountSubscriptionService, AccountSubscriptionService>();

//Payment
builder.Services.AddScoped<Application.Interfaces.IPaymentService, Application.Services.PaymentService>();

//Advertisement
builder.Services.AddScoped<IAdvertisementService, AdvertisementService>();

//AccountAdmin
builder.Services.AddScoped<IAdminService, AdminService>();

//Location
builder.Services.AddScoped<ILocationService, LocationService>();

//Segment
builder.Services.AddScoped<ITripSegmentService, TripSegmentService>();

//Gemini
builder.Services.AddHttpClient<IGeminiService, GeminiService>()
   .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
   {
       ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
   });

//Add repositories
//Auth
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();

//User
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Preference
builder.Services.AddScoped<IPreferenceRepository, PreferenceRepository>();

//POI
builder.Services.AddScoped<IPOIRepository, POIRepository>();

//AdSubscriptionPackage
builder.Services.AddScoped<IAdSubscriptionPackageRepository, AdSubscriptionPackageRepository>();

//AccountSubscription
builder.Services.AddScoped<IAccountSubscriptionRepository, AccountSubscriptionRepository>();

//Payment
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

//Advertisement
builder.Services.AddScoped<IAdvertisementRepository, AdvertisementRepository>();

//Itinerary
builder.Services.AddScoped<IItineraryRepository, ItineraryRepository>();

//ItineraryDetail
builder.Services.AddScoped<IItineraryDetailRepository, ItineraryDetailRepository>();

//Segment
builder.Services.AddScoped<ITripSegmentRepository, TripSegmentRepository>();

//Location
builder.Services.AddScoped<ILocationRepository, LocationRepository>();



//Cloudinary
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

//BackGroundJob
builder.Services.AddScoped<IWeatherMonitorJob, WeatherMonitorJob>();
builder.Services.AddScoped<IWeatherPreloadJob, WeatherPreloadJob>();

//RiskEngine
builder.Services.AddScoped<IAdaptiveWeatherRiskEngine, AdaptiveWeatherRiskEngine>();

//Notification
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationRecipientService, NotificationRecipientService>();
builder.Services.AddScoped<INotificationRecipientRepository, NotificationRecipientRepository>();

//Participant
builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();

//WeatherRiskScan
builder.Services.AddScoped<IWeatherRiskScanService, WeatherRiskScanService>();

//Weatherforecast
builder.Services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IBackgroundJobService, HangfireJobService>();

//Trip
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<ITripQueryService, TripQueryService>();

//Planner&RePlanner
builder.Services.AddScoped<IPlannerService, PlannerService>();

//SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});

// Configure JWT
var jwtConfig = builder.Configuration.GetSection("JwtSettings");
if (builder.Environment.IsDevelopment() || builder.Environment.IsProduction())
{
    if (!jwtConfig.Exists())
        throw new Exception("JwtSettings section is missing in configuration.");
}

var secretKey = jwtConfig["SecretKey"];
var issuer = jwtConfig["Issuer"];
var audience = jwtConfig["Audience"];
var expiryInMinutes = jwtConfig["ExpiryInMinutes"];


if ((builder.Environment.IsDevelopment() || builder.Environment.IsProduction())
    && string.IsNullOrEmpty(secretKey))
{
    throw new Exception("SecretKey is null or empty in JwtSettings.");
}


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        NameClaimType = ClaimTypes.NameIdentifier, 
        RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/notification"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.UseInlineDefinitionsForEnums();
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TravelPlanner API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


var app = builder.Build();

app.UseRouting();

app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectName API v1"));

app.UseHangfireDashboard("/hangfire");

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");

// ============================
// HANGFIRE RECURRING JOB
// ============================
RecurringJob.AddOrUpdate<IWeatherMonitorJob>(
    "weather-hourly-scan",
    x => x.ScanUpcomingTripsAsync(),
    Cron.Hourly);

app.Run();