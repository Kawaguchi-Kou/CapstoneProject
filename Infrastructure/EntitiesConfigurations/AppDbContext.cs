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


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Auth
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(u => u.Id);
                //entity.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(u => u.CreatedAt).HasColumnType("timestamp with time zone");
                entity.Property(u => u.IsActive).IsRequired();
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



            base.OnModelCreating(modelBuilder);
        }
    }
}

//dotnet ef migrations add InitialCreate --project Infrastructure --startup-project WebAPI
//dotnet ef migrations add InitSupabase  --project Infrastructure --startup-project WebAPI

//dotnet ef database update --project Infrastructure --startup-project WebAPI

//dotnet ef migrations remove --project Infrastructure --startup-project WebAPI

