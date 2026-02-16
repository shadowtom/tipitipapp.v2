using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tipitipapp.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void OnEmailCompleted(object sender, EventArgs e)
        {
            PasswordEntry.Focus();
        }

        private void OnPasswordCompleted(object sender, EventArgs e)
        {
            // Trigger login
            var vm = BindingContext as ViewModels.LoginViewModel;
            vm?.LoginCommand.Execute(null);
        }
    }
}
