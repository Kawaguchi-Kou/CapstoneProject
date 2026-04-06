using System.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.EntitiesConfigurations
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        //db set
        //Auth
        public DbSet<Account> Accounts { get; set; }
        public DbSet<OtpVerification> OtpVerifications { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        //Trip
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripSegment> TripSegments { get; set; }
        public DbSet<Itinerary> Itineraries { get; set; }
        public DbSet<ItineraryDetail> ItineraryDetails { get; set; }
        public DbSet<Location> Locations { get; set; }

        //Notification
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationRecipient> Recipients { get; set; }
        public DbSet<Participant> Participants {  get; set; }

        //POI&Preference Vector
        public DbSet<POI> POIs { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<POIPreference> POIPreferences { get; set; }
        public DbSet<Preference> Preferences { get; set; }

        //Advertisement
        public DbSet<Advertisement> Advertisements { get; set; }
        public DbSet<AdSubscriptionPackage> adSubscriptionPackages { get; set; }
        public DbSet<AdPayment> adPayments { get; set; }
        public DbSet<AccountSubscription> accountSubscriptions { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<SavedPromotion> SavedPromotions { get; set; }

        //Weather Forecast
        public DbSet<WeatherForecast> WeatherForecasts { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // =========================
            // AUTH
            // =========================
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("accounts");
                entity.HasKey(u => u.Id);
                //entity.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(u => u.CreatedAt).HasColumnType("timestamp with time zone");
                entity.Property(u => u.IsActive).IsRequired();
                entity.Property(u => u.DateOfBirth).IsRequired();
                entity.Property(u => u.Address).IsRequired(false);
                entity.Property(u => u.AvatarUrl).IsRequired(false);
                entity.Property(u => u.Gender).IsRequired();
                entity.Property(u => u.PhoneNumber).IsRequired(false);
                entity.Property(u => u.Name).IsRequired();
                entity.Property(u => u.ResetToken).IsRequired(false);

                // Unique constraint cho email
                entity.HasIndex(u => u.Email).IsUnique();

                // Quan hệ 1 Role - nhiều Account
                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Accounts)
                      .HasForeignKey(u => u.RoleId)
                      .OnDelete(DeleteBehavior.Restrict); // tránh xóa Role thì xóa luôn Account
            });

            modelBuilder.Entity<OtpVerification>(entity =>
            {
                entity.ToTable("otp_verifications");
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Id).ValueGeneratedOnAdd();
                entity.Property(o => o.Email).IsRequired().HasMaxLength(255);
                entity.Property(o => o.OtpCode).IsRequired().HasMaxLength(10);
                entity.Property(o => o.ExpiresAt).HasColumnType("timestamp with time zone");
                entity.Property(o => o.Purpose).IsRequired().HasMaxLength(100);
                entity.Property(o => o.IsUsed).IsRequired();
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("refresh_tokens");
                entity.HasKey(rt => rt.Id);
                //entity.Property(rt => rt.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(rt => rt.Token).IsRequired().HasMaxLength(500);
                entity.Property(rt => rt.CreatedAt).HasColumnType("timestamp with time zone");
                entity.Property(rt => rt.InitialLoginAt).HasColumnType("timestamp with time zone");
                entity.Property(rt => rt.IsRevoked).IsRequired();

                entity.HasOne(rt => rt.Account)
                      .WithMany(a => a.RefreshTokens)
                      .HasForeignKey(rt => rt.AccountId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // TRIPS
            // =========================
            modelBuilder.Entity<Trip>(entity =>
            {
                entity.ToTable("trips");

                entity.HasKey(t => t.TripId);

                entity.Property(t => t.Status)
                      .IsRequired();

                entity.Property(t => t.Title)
                      .HasMaxLength(255);

                entity.Property(t => t.TripType)
                    .IsRequired();

                entity.Property(t => t.CreatedAt)
                      .HasDefaultValueSql("NOW()");

                // Relationships
                entity.HasMany(t => t.TripSegments)
                      .WithOne(s => s.Trip)
                      .HasForeignKey(s => s.TripId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // LOCATIONS
            // =========================
            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("locations");

                entity.HasKey(l => l.LocationId);

                entity.Property(l => l.LocationName)
                .HasMaxLength(150);
            });

            // =========================
            // TRIP_SEGMENTS
            // =========================
            modelBuilder.Entity<TripSegment>(entity =>
            {
                entity.ToTable("trip_segments");

                entity.HasKey(s => s.SegmentId);

                entity.Property(s => s.OrderIndex)
                      .IsRequired();

                entity.Property(s => s.CreatedAt)
                      .HasDefaultValueSql("NOW()");

                // relationship
                entity.HasOne(s => s.Location)
                      .WithMany(l => l.Segments)
                      .HasForeignKey(s => s.LocationId);

                entity.HasMany(s => s.Itineraries)
                      .WithOne(i => i.Segment)
                      .HasForeignKey(i => i.SegmentId);
            });

            // =========================
            // ITINERARIES
            // =========================
            modelBuilder.Entity<Itinerary>(entity =>
            {
                entity.ToTable("itineraries");

                entity.HasKey(i => i.ItineraryId);

                entity.Property(i => i.GeneratedAt)
                      .HasDefaultValueSql("NOW()");

                //relationship  
                entity.HasMany(i => i.ItineraryDetails)
                      .WithOne(d => d.Itinerary)
                      .HasForeignKey(d => d.ItineraryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // ITINERARY_DETAILS
            // =========================
            modelBuilder.Entity<ItineraryDetail>(entity =>
            {
                entity.ToTable("itinerary_details");

                entity.HasKey(d => d.DetailId);

                entity.Property(d => d.WeatherRiskScore)
                      .HasDefaultValue(0);

                entity.Property(d => d.CreatedAt)
                      .HasDefaultValueSql("NOW()");

                entity.HasOne(id => id.POI)
                      .WithMany(p => p.ItineraryDetails)
                      .HasForeignKey(d => d.PoiId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // =========================
            // MANUAL_OVERRIDES
            // =========================
            //modelBuilder.Entity<ManualOverride>(entity =>
            //{
            //    entity.ToTable("manual_overrides");
            //    entity.HasKey(o => o.Id);
            //    entity.Property(o => o.CreatedAt)
            //          .HasDefaultValueSql("NOW()");
            //    entity.HasOne(o => o.Detail)
            //          .WithMany(d => d.Overrides)
            //          .HasForeignKey(o => o.DetailId)
            //          .OnDelete(DeleteBehavior.Cascade);
            //});

            // =========================
            // NOTIFICATIONS
            // =========================
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("notifications");
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Title)
                      .IsRequired()
                      .HasMaxLength(255);
                entity.Property(n => n.Message)
                      .IsRequired();
                entity.Property(n => n.CreatedAt)
                      .HasDefaultValueSql("NOW()");
            });

            // =========================
            // NOTIFICATION RECIPIENTS
            // =========================
            modelBuilder.Entity<NotificationRecipient>(entity =>
            {
                entity.ToTable("recipients");

                entity.HasKey(r => r.Id);

                entity.HasOne(r => r.Notification)
                      .WithMany(n => n.Recipients)
                      .HasForeignKey(r => r.NotificationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Recipient)
                      .WithMany(a => a.Recipients)
                      .HasForeignKey(r => r.RecipientId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // POI & PREFERENCE VECTORS
            // =========================
            modelBuilder.Entity<POI>(entity =>
            {
                entity.ToTable("pois");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name)
                      .IsRequired()
                      .HasMaxLength(255);
                entity.Property(p => p.ApproxCost)
                      .IsRequired()
                      .HasMaxLength(255);
                // City is now sourced from related Location, not pois table column.
                entity.Ignore(p => p.City);
                entity.Property(p => p.Address)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.HasOne(poi => poi.Partner)
                      .WithMany(p => p.POIs)
                      .HasForeignKey(poi => poi.PartnerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<POIPreference>(entity =>
            {
                entity.ToTable("poi_preferences");

                // composite key
                entity.HasKey(pp => new { pp.PoiId, pp.PreferenceId });

                entity.HasOne(pp => pp.POI)
                      .WithMany(p => p.PoiPreferences)
                      .HasForeignKey(pp => pp.PoiId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pp => pp.Preference)
                      .WithMany(p => p.PoiPreferences)
                      .HasForeignKey(pp => pp.PreferenceId)
                      .OnDelete(DeleteBehavior.Cascade);

                //entity.Property(pp => pp.Weight)
                //      .IsRequired();
            });

            modelBuilder.Entity<UserPreference>(entity =>
            {
                entity.ToTable("user_preferences");

                entity.HasKey(upv => upv.Id);

                entity.HasOne(upv => upv.Account)
                      .WithMany(a => a.UserPreferenceVectors)
                      .HasForeignKey(upv => upv.AccountId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // ADVERTISEMENT
            // =========================
            modelBuilder.Entity<AdSubscriptionPackage>(entity =>
            {
                entity.ToTable("ad_subscription_packages");

                entity.HasKey(e => e.PackageId);

                entity.Property(e => e.Title)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.Currency)
                      .HasMaxLength(10);

                entity.Property(e => e.Status)
                      .HasMaxLength(50);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("NOW()");
            });

            modelBuilder.Entity<AccountSubscription>(entity =>
            {
                entity.ToTable("account_subscriptions");

                entity.HasKey(e => e.SubscriptionId);

                entity.Property(e => e.Status)
                      .HasMaxLength(50);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("NOW()");

                entity.HasOne(e => e.Account)
                      .WithMany(a => a.AccountSubscriptions)
                      .HasForeignKey(e => e.AccountId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SubscriptionPackage)
                      .WithMany(p => p.AccountSubscriptions)
                      .HasForeignKey(e => e.SubscriptionPackageId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Advertisement>(entity =>
            {
                entity.ToTable("advertisements");

                entity.HasKey(e => e.AdId);

                entity.Property(e => e.Title)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Status)
                      .HasConversion<int>() // enum -> int
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("NOW()");

                entity.HasOne(e => e.Account)
                      .WithMany(a => a.Advertisements)
                      .HasForeignKey(e => e.AccountId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Package)
                      .WithMany()
                      .HasForeignKey(e => e.PackageId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.POI)
                      .WithMany(p => p.Advertisements)
                      .HasForeignKey(e => e.POIId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.ToTable("promotions");

                entity.HasKey(e => e.PromotionId);

                entity.Property(e => e.Title)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Description)
                      .HasMaxLength(2000);

                entity.Property(e => e.Terms)
                      .HasMaxLength(2000);

                entity.Property(e => e.Status)
                      .HasConversion<int>()
                      .IsRequired();

                entity.Property(e => e.SaveCount)
                      .HasDefaultValue(0);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("NOW()");

                entity.HasIndex(e => e.AdId)
                      .IsUnique();

                entity.HasOne(e => e.Advertisement)
                      .WithOne(a => a.Promotion)
                      .HasForeignKey<Promotion>(e => e.AdId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SavedPromotion>(entity =>
            {
                entity.ToTable("saved_promotions");

                entity.HasKey(e => e.SavedPromotionId);

                entity.Property(e => e.SavedAt)
                      .HasDefaultValueSql("NOW()");

                entity.HasIndex(e => new { e.PromotionId, e.AccountId })
                      .IsUnique();

                entity.HasOne(e => e.Promotion)
                      .WithMany(p => p.SavedPromotions)
                      .HasForeignKey(e => e.PromotionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Account)
                      .WithMany(a => a.SavedPromotions)
                      .HasForeignKey(e => e.AccountId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AdPayment>(entity =>
            {
                entity.ToTable("ad_payments");

                entity.HasKey(e => e.PaymentId);

                entity.Property(e => e.Currency)
                      .HasMaxLength(10);

                entity.Property(e => e.PaymentMethod)
                      .HasMaxLength(50);

                entity.Property(e => e.PaymentStatus)
                      .HasConversion<int>() // enum -> int
                      .IsRequired();

                entity.Property(e => e.TransactionContent)
                      .HasMaxLength(500);

                entity.Property(e => e.AccountNumber)
                      .HasMaxLength(50);

                entity.Property(e => e.SubAccount)
                      .HasMaxLength(100);

                entity.Property(e => e.Gateway)
                      .HasMaxLength(100);

                entity.Property(e => e.Code)
                      .HasMaxLength(100);

                entity.Property(e => e.PaidAt)
                      .HasDefaultValueSql("NOW()");

                entity.HasOne(e => e.Subscription)
                      .WithMany(s => s.Payments)
                      .HasForeignKey(e => e.SubscriptionId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            //==========================
            //WEATHER_FORECAST
            //==========================
            modelBuilder.Entity<WeatherForecast>(entity =>
                {
                    entity.ToTable("weather_forecast");

                    entity.HasIndex(x => x.City)
                            .IsUnique();
                });

            //==========================
            //PARTICIPANTS
            //==========================
            modelBuilder.Entity<Participant>(entity =>
            {
                entity.ToTable("participants");
                entity.HasKey(p => p.Id);
                entity.HasOne(p => p.User)
                      .WithMany(a => a.Participants)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.Trip)
                      .WithMany(t => t.Participants)
                      .HasForeignKey(p => p.TripId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

//dotnet ef migrations add InitialCreate --project Infrastructure --startup-project WebAPI
//dotnet ef migrations add InitSupabase  --project Infrastructure --startup-project WebAPI

//dotnet ef database update --project Infrastructure --startup-project WebAPI

//dotnet ef migrations remove --project Infrastructure --startup-project WebAPI

