using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using tipitipapp.domain.Entities.DTOs;
using tipitipapp.domain.Interfaces.Services;
using tipitipapp.domain.Services;
using tipitipapp.domain.Utilities.Security;
using tipitipapp.infrastructure.Repository;

namespace tipitipapp.ViewModels
{
    public partial class LoginViewModel : BindableObject, INotifyPropertyChanged
    {
        private readonly IAuthService _authService = null!;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _message = string.Empty;
        private bool _isBusy;

        public LoginViewModel(IServiceProvider serviceProvider)
        {
            _authService = serviceProvider.GetRequiredService<AuthService>();

            LoginCommand = new Command(async () => await LoginAsync());
            RegisterCommand = new Command(async () => await RegisterAsync());
        }

        public LoginViewModel()
        {
            // Inicialización para evitar CS8618
            _authService = null!;
            LoginCommand = new Command(async () => await LoginAsync());
            RegisterCommand = new Command(async () => await RegisterAsync());
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public Command LoginCommand { get; } = null!;
        public Command RegisterCommand { get; } = null!;

        private async Task LoginAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                Message = string.Empty;

                var loginDto = new LoginDto { Email = Email, Password = Password };
                var result = await _authService.LoginAsync(loginDto);

                Message = result.message;

                if (result.success)
                {
                    // Navigate to main page
                    await Shell.Current.GoToAsync("//MainPage");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RegisterAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                Message = string.Empty;

                // For simplicity, using same password for confirm
                var registerDto = new RegisterDto
                {
                    Email = Email,
                    Password = Password,
                    ConfirmPassword = Password
                };

                var result = await _authService.RegisterAsync(registerDto);
                Message = result.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged; 
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
