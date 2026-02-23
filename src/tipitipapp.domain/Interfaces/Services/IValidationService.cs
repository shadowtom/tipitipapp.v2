using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tipitipapp.domain.Interfaces.Services
{
    public interface IValidationService
    {
        bool IsValidEmail(string email);
        bool IsStrongPassword(string password);
        bool IsValidPhoneNumber(string phoneNumber);
    }
}
