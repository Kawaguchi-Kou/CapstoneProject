using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken> GetByTokenAsync(string token);
        Task CreateTokenAsync(RefreshToken refreshToken);
        Task RevokeTokenAsync(RefreshToken refreshToken);
    }
}
