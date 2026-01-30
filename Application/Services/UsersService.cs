using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class UsersService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UsersService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public Task<List<Account>> GetAll()
        {
            var users = _userRepository.GetAllAsync();
            if(users == null || users.Result.Count == 0)
            {
                throw new Exception("No users found");
            }
            return users;
        }

        public Task<Account?> GetById(Guid id)
        {
            var user = _userRepository.GetByIdAsync(id);
            if(user == null || user.Result == null)
            {
                throw new Exception("User not found");
            }
            return user;
        }

        public async Task<Account> UpdateProfile(Account user)
        {
            var existedUser = await _userRepository.GetByIdAsync(user.Id);
            if(existedUser == null || existedUser == null)
            {
                throw new Exception("User not found");
            }

            // Update
            existedUser.Name = user.Name;
            existedUser.Address = user.Address;
            existedUser.PhoneNumber = user.PhoneNumber;
            existedUser.Gender = user.Gender;
            existedUser.DateOfBirth = user.DateOfBirth;
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                existedUser.AvatarUrl = user.AvatarUrl;
            }

            return await _userRepository.UpdateProfileAsync(existedUser);
        }

        public async Task<Account> CreateProfile(Account user)
        {
            var existedUser = await _userRepository.GetByIdAsync(user.Id);
            if (existedUser != null)
            {
                throw new Exception("User already exists");
            }
            return await _userRepository.CreateProfileAsync(user);
        }

        public async Task<List<Account>> GetByIdsAsync(List<Guid> ids)
        {
            if (ids == null || !ids.Any())
                throw new ArgumentException("Ids list cannot be null or empty");

            var users = await _userRepository.GetByIdsAsync(ids);

            if (users == null || !users.Any())
                throw new KeyNotFoundException("No users found for the provided Ids");

            return users;
        }
    }
}
