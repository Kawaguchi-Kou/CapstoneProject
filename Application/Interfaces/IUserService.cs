using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetById(Guid id);
        Task<List<User>> GetAll();
        Task<User> UpdateProfile(User user);
        Task<User> CreateProfile(User user);
        Task<List<User>> GetByIdsAsync(List<Guid> ids);
    }
}
