using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Services
{
    public class RefreshTokenService: IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public RefreshTokenService(IRefreshTokenRepository refreshTokenRepository)
        {
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<RefreshToken> GetByTokenAsync(string token)
        {
            return await _refreshTokenRepository.GetByTokenAsync(token);
        }

        public async Task CreateTokenAsync(RefreshToken refreshToken)
        {
            await _refreshTokenRepository.CreateTokenAsync(refreshToken);
        }

        public async Task RevokeTokenAsync(RefreshToken refreshToken)
        {
            await _refreshTokenRepository.RevokeTokenAsync(refreshToken);
        }
    }
}
