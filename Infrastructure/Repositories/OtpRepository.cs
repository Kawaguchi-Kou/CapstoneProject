using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EntitiesConfigurations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class OtpRepository: IOtpRepository
    {
        private readonly AppDbContext _context;
        public OtpRepository(AppDbContext context) => _context = context;

        public async Task AddAsync(OtpVerification otp)
        {
            await _context.OtpVerifications.AddAsync(otp);
        }

        public async Task<OtpVerification?> GetValidOtpAsync(string email, string otpCode, string purpose)
        {
            return await _context.OtpVerifications
                .FirstOrDefaultAsync(
                    o => o.Email == email &&
                    o.OtpCode == otpCode &&
                    !o.IsUsed &&
                    o.ExpiresAt > DateTime.UtcNow &&
                    o.Purpose == purpose);
        }

        public async Task<List<OtpVerification>> GetAllByAccountAsync(string email, string purpose)
        {
            return await _context.OtpVerifications
                .Where(o => o.Email == email &&
                            o.Purpose == purpose)
                .ToListAsync();
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
