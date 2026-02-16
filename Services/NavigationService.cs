using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tipitipapp.Interfaces.Services;

namespace tipitipapp.Services
{
    internal class NavigationService : INavigationService
    {
        public async Task GoToLoginPageAsync()
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }

        public async Task GoToMainPageAsync()
        {
            await Shell.Current.GoToAsync("//MainPage");
        }

        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
