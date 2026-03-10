using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Interfaces
{
    public interface IAdminService
    {
        Task<List<Account>> GetAll();
    }

}
