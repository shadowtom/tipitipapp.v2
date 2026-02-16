using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tipitipapp.Interfaces.Services
{
    public interface INavigationService
    {
        Task GoToLoginPageAsync();
        Task GoToMainPageAsync();
        Task GoBackAsync();
    }
}
