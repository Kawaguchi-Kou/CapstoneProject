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


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // =========================
            // AUTH
            // =========================
            modelBuilder.Entity<Account>(entity =>
            {
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
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
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

                entity.Property(t => t.StartLocation)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(t => t.EndLocation)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(t => t.Status)
                      .IsRequired();

                entity.Property(t => t.CreatedAt)
                      .HasDefaultValueSql("NOW()");

                // Relationships
                entity.HasMany(t => t.TripSegments)
                      .WithOne(s => s.Trip)
                      .HasForeignKey(s => s.TripId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(t => t.Itineraries)
                      .WithOne(i => i.Trip)
                      .HasForeignKey(i => i.TripId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // TRIP_SEGMENTS
            // =========================
            modelBuilder.Entity<TripSegment>(entity =>
            {
                entity.ToTable("trip_segments");

                entity.HasKey(s => s.SegmentId);

                entity.Property(s => s.SequenceNo)
                      .IsRequired();

                entity.Property(s => s.FromLocation)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(s => s.ToLocation)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(s => s.CreatedAt)
                      .HasDefaultValueSql("NOW()");

                // Ensure sequence is unique per trip
                entity.HasIndex(s => new { s.TripId, s.SequenceNo })
                      .IsUnique();
            });

            // =========================
            // ITINERARIES
            // =========================
            modelBuilder.Entity<Itinerary>(entity =>
            {
                entity.ToTable("itineraries");

                entity.HasKey(i => i.ItineraryId);

                entity.Property(i => i.Version)
                      .IsRequired();

                entity.Property(i => i.IsActive)
                      .HasDefaultValue(false);

                entity.Property(i => i.GeneratedAt)
                      .HasDefaultValueSql("NOW()");

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

                // Optional relationship to TripSegment
                entity.HasOne(d => d.TripSegment)
                      .WithMany()
                      .HasForeignKey(d => d.SegmentId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

        }
    }
}

//dotnet ef migrations add InitialCreate --project Infrastructure --startup-project WebAPI
//dotnet ef migrations add InitSupabase  --project Infrastructure --startup-project WebAPI

//dotnet ef database update --project Infrastructure --startup-project WebAPI

//dotnet ef migrations remove --project Infrastructure --startup-project WebAPI

