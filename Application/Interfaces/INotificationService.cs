using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(CreateNotificationRequest notification);
    }
}
