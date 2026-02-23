using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tipitipapp.domain.Entities.DTOs;

namespace tipitipapp.domain.Interfaces.Services
{
    public interface IAuthService
    {
        public Task<(bool success, string message)> LoginAsync(LoginDto loginDto);
        public Task<RegisterResultDto> RegisterAsync(RegisterDto registerDto);
    }
}
