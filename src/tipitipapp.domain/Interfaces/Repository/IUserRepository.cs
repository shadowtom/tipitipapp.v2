using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tipitipapp.domain.Entities;

namespace tipitipapp.domain.Interfaces.Repository
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task<User?> GetByEmailAsync(string email);
        Task UpdateAsync(User user);
    }
}
