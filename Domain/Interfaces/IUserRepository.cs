using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<List<User>> GetAllAsync();
        Task<User> UpdateProfileAsync(User user);
        Task<User> CreateProfileAsync(User user);
        Task<List<User>> GetByIdsAsync(List<Guid> ids);
    }
}
