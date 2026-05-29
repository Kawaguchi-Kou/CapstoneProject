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
using System.Net;
using Polly;
using Polly.Extensions.Http;


AppContext.SetSwitch("System.Net.DisableIPv6", true);
//load .env
Env.Load();

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.License.SetNonCommercialPersonal("CapstoneProject");


// Add DbContext
var connectionString = Environment.GetEnvironmentVariable("SUPABASE");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

////Hangfire configuration 
//builder.Services.AddHangfire(config =>
//    config.UsePostgreSqlStorage(options =>
//        options.UseNpgsqlConnection(connectionString)
//    ));
//builder.Services.AddHangfireServer(options =>
//{
//    options.WorkerCount = 2; 
//});

// =====================
// WEATHER - OPEN METEO
// =====================
builder.Services.Configure<OpenMeteoOptions>(
    builder.Configuration.GetSection(OpenMeteoOptions.SectionName));

builder.Services.AddHttpClient<IOpenMeteoService, OpenMeteoService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
        new SocketsHttpHandler
        {
            // 🔥 FIX SSL / EOF issue
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
            },

            // 🔥 FIX VN network instability
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 5,

            // 🔥 avoid weird decompression bugs
            AutomaticDecompression = System.Net.DecompressionMethods.All
        })
    .ConfigureHttpClient(client =>
    {
        // 🔥 CRITICAL: disable HTTP/2 (causes EOF a lot)
        client.DefaultRequestVersion = HttpVersion.Version11;

        // 🔥 timeout
        client.Timeout = TimeSpan.FromSeconds(10);
    });

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

//District
builder.Services.AddScoped<IDistrictService, DistrictService>();

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
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

//AccountAdmin
builder.Services.AddScoped<IAdminService, AdminService>();

//Location
builder.Services.AddScoped<ILocationService, LocationService>();

//Partner Statistics
builder.Services.AddScoped<IPartnerStatisticService, PartnerStatisticService>();
builder.Services.AddScoped<IManagerStatisticService, ManagerStatisticService>();

//Admin Statistics
builder.Services.AddScoped<IAdminStatisticService, AdminStatisticService>();

//Segment
builder.Services.AddScoped<ITripSegmentService, TripSegmentService>();

//RouteGraph (singleton — JSON is static)
var graphPath = Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..",   // up from WebAPI/bin/Debug/net8.0 → solution root
    "Infrastructure", "Graph", "vietnam_phuot_graph.json");

if (!File.Exists(graphPath))
{
    // fallback: try relative to ContentRootPath
    graphPath = Path.Combine(
        builder.Environment.ContentRootPath,
        "..", "Infrastructure", "Graph", "vietnam_phuot_graph.json");
}

builder.Services.AddSingleton<IRouteGraphService>(
    _ => new RouteGraphService(Path.GetFullPath(graphPath)));

//Gemini
builder.Services.AddHttpClient<IGeminiService, GeminiService>()
   .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
   {
       ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
   })
   .AddTransientHttpErrorPolicy(policyBuilder =>
    // Retries 3 times. Wait 500ms, then 1s, then 2s.
    policyBuilder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(500 * Math.Pow(2, retryAttempt - 1))));

//Add repositories
//Auth
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();

//UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

//User
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Preference
builder.Services.AddScoped<IPreferenceRepository, PreferenceRepository>();

//District
builder.Services.AddScoped<IDistrictRepository, DistrictRepository>();

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

//Planner&RePlanner
builder.Services.AddScoped<IPlannerRepository, PlannerRepository>();

//Participant
builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();



//Cloudinary
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

//BackGroundJob
//builder.Services.AddScoped<IWeatherMonitorJob, WeatherMonitorJob>();
builder.Services.AddScoped<IPaymentExpiryJob, PaymentExpiryJob>();
builder.Services.AddScoped<IAdSchedulingJob, AdSchedulingJob>();
// IWeatherPreloadJob removed — weather preload is now triggered on-demand via TripWeatherController

//RiskEngine
builder.Services.AddScoped<IAdaptiveWeatherRiskEngine, AdaptiveWeatherRiskEngine>();

//Notification
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationRecipientService, NotificationRecipientService>();
builder.Services.AddScoped<INotificationRecipientRepository, NotificationRecipientRepository>();

//Participant
builder.Services.AddScoped<IParticipantService, ParticipantService>();

//Partner
builder.Services.AddScoped<IPartnerRequestRepository, PartnerRequestRepository>();
builder.Services.AddScoped<IPartnerProfileRepository, PartnerProfileRepository>();
builder.Services.AddScoped<IPartnerRequestService, PartnerRequestService>();
builder.Services.AddScoped<IPartnerProfileService, PartnerProfileService>();


//WeatherRiskScan
//builder.Services.AddScoped<IWeatherRiskScanService, WeatherRiskScanService>();

//Weatherforecast
builder.Services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
// IBackgroundJobService / HangfireJobService removed — no longer used for weather preload

//Trip
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<ITripQueryService, TripQueryService>();

//Planner&RePlanner
builder.Services.AddScoped<IPlannerService, PlannerService>();

//SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

// add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:7176",
                "https://localhost:7176",
                "http://localhost:5173",
                "https://localhost:5173"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    //options.AddPolicy("AllowFrontend",
    //    policy =>
    //    {
    //        policy.WithOrigins("https://traveler-planner-nine.vercel.app")
    //              .AllowAnyHeader()
    //              .AllowAnyMethod()
    //              .AllowCredentials();
    //    });
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
//app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectName API v1"));

//app.UseHangfireDashboard("/hangfire");

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");

// ============================
// HANGFIRE RECURRING JOB
// ============================
//RecurringJob.AddOrUpdate<IWeatherMonitorJob>(
//    "weather-hourly-scan",
//    x => x.ScanUpcomingTripsAsync(),
//    Cron.Hourly);

RecurringJob.AddOrUpdate<IAdSchedulingJob>(
    "ad-scheduling-scan",
    x => x.ProcessScheduledAndExpiredAdsAsync(),
    "*/5 * * * *");  // Mỗi 5 phút



app.Run();