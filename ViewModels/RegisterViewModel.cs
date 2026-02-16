using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using tipitipapp.domain.Entities.DTOs;
using tipitipapp.domain.Services;

namespace tipitipapp.ViewModels
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;

        // Registration fields
        private string _email = string.Empty;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _phoneNumber = string.Empty;
        private bool _acceptTerms;
        private bool _showPassword;
        private bool _showConfirmPassword;

        // UI state fields
        private string _message = string.Empty;
        private bool _isBusy;
        private bool _isSuccess;
        private Color _messageColor = Colors.Red;
        private double _passwordStrength;

        public RegisterViewModel(AuthService authService)
        {
            _authService = authService;

            // Initialize commands
            RegisterCommand = new Command(async () => await ExecuteRegisterCommand());
            GoToLoginCommand = new Command(async () => await ExecuteGoToLoginCommand());
            ToggleShowPasswordCommand = new Command(() => ShowPassword = !ShowPassword);
            ToggleShowConfirmPasswordCommand = new Command(() => ShowConfirmPassword = !ShowConfirmPassword);

            // Initialize properties
            Title = "Create Account";
        }

        // Properties with change notification
        public string Title { get; }

        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged();
                    ValidateEmail();
                }
            }
        }

        public string FirstName
        {
            get => _firstName;
            set
            {
                if (_firstName != value)
                {
                    _firstName = value;
                    OnPropertyChanged();
                    ValidateFirstName();
                }
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                if (_lastName != value)
                {
                    _lastName = value;
                    OnPropertyChanged();
                    ValidateLastName();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                    ValidatePassword();
                    CalculatePasswordStrength();
                    ValidateConfirmPassword();
                }
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (_confirmPassword != value)
                {
                    _confirmPassword = value;
                    OnPropertyChanged();
                    ValidateConfirmPassword();
                }
            }
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (_phoneNumber != value)
                {
                    _phoneNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AcceptTerms
        {
            get => _acceptTerms;
            set
            {
                if (_acceptTerms != value)
                {
                    _acceptTerms = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowPassword
        {
            get => _showPassword;
            set
            {
                if (_showPassword != value)
                {
                    _showPassword = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowConfirmPassword
        {
            get => _showConfirmPassword;
            set
            {
                if (_showConfirmPassword != value)
                {
                    _showConfirmPassword = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    ((Command)RegisterCommand).ChangeCanExecute();
                }
            }
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set
            {
                if (_isSuccess != value)
                {
                    _isSuccess = value;
                    OnPropertyChanged();
                }
            }
        }

        public Color MessageColor
        {
            get => _messageColor;
            set
            {
                if (_messageColor != value)
                {
                    _messageColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public double PasswordStrength
        {
            get => _passwordStrength;
            set
            {
                if (_passwordStrength != value)
                {
                    _passwordStrength = value;
                    OnPropertyChanged();
                }
            }
        }

        // Validation properties
        public bool IsEmailValid { get; private set; } = true;
        public bool IsFirstNameValid { get; private set; } = true;
        public bool IsLastNameValid { get; private set; } = true;
        public bool IsPasswordValid { get; private set; } = true;
        public bool IsConfirmPasswordValid { get; private set; } = true;

        public string EmailError { get; private set; } = string.Empty;
        public string FirstNameError { get; private set; } = string.Empty;
        public string LastNameError { get; private set; } = string.Empty;
        public string PasswordError { get; private set; } = string.Empty;
        public string ConfirmPasswordError { get; private set; } = string.Empty;

        // Commands
        public ICommand RegisterCommand { get; }
        public ICommand GoToLoginCommand { get; }
        public ICommand ToggleShowPasswordCommand { get; }
        public ICommand ToggleShowConfirmPasswordCommand { get; }

        // Validation methods
        private void ValidateEmail()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                IsEmailValid = false;
                EmailError = "Email is required";
            }
            else if (!IsValidEmail(Email))
            {
                IsEmailValid = false;
                EmailError = "Invalid email format";
            }
            else
            {
                IsEmailValid = true;
                EmailError = string.Empty;
            }
            OnPropertyChanged(nameof(IsEmailValid));
            OnPropertyChanged(nameof(EmailError));
        }

        private void ValidateFirstName()
        {
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                IsFirstNameValid = false;
                FirstNameError = "First name is required";
            }
            else if (FirstName.Length < 2)
            {
                IsFirstNameValid = false;
                FirstNameError = "First name must be at least 2 characters";
            }
            else
            {
                IsFirstNameValid = true;
                FirstNameError = string.Empty;
            }
            OnPropertyChanged(nameof(IsFirstNameValid));
            OnPropertyChanged(nameof(FirstNameError));
        }

        private void ValidateLastName()
        {
            if (string.IsNullOrWhiteSpace(LastName))
            {
                IsLastNameValid = false;
                LastNameError = "Last name is required";
            }
            else if (LastName.Length < 2)
            {
                IsLastNameValid = false;
                LastNameError = "Last name must be at least 2 characters";
            }
            else
            {
                IsLastNameValid = true;
                LastNameError = string.Empty;
            }
            OnPropertyChanged(nameof(IsLastNameValid));
            OnPropertyChanged(nameof(LastNameError));
        }

        private void ValidatePassword()
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                IsPasswordValid = false;
                PasswordError = "Password is required";
            }
            else
            {
                var errors = new List<string>();
                if (Password.Length < 6)
                    errors.Add("at least 6 characters");
                if (!Password.Any(char.IsUpper))
                    errors.Add("an uppercase letter");
                if (!Password.Any(char.IsLower))
                    errors.Add("a lowercase letter");
                if (!Password.Any(char.IsDigit))
                    errors.Add("a number");

                if (errors.Any())
                {
                    IsPasswordValid = false;
                    PasswordError = $"Password must contain {string.Join(", ", errors)}";
                }
                else
                {
                    IsPasswordValid = true;
                    PasswordError = string.Empty;
                }
            }
            OnPropertyChanged(nameof(IsPasswordValid));
            OnPropertyChanged(nameof(PasswordError));
        }

        private void ValidateConfirmPassword()
        {
            if (Password != ConfirmPassword)
            {
                IsConfirmPasswordValid = false;
                ConfirmPasswordError = "Passwords do not match";
            }
            else
            {
                IsConfirmPasswordValid = true;
                ConfirmPasswordError = string.Empty;
            }
            OnPropertyChanged(nameof(IsConfirmPasswordValid));
            OnPropertyChanged(nameof(ConfirmPasswordError));
        }

        private void CalculatePasswordStrength()
        {
            if (string.IsNullOrEmpty(Password))
            {
                PasswordStrength = 0;
                return;
            }

            int strength = 0;

            // Length check
            if (Password.Length >= 8) strength += 25;
            else if (Password.Length >= 6) strength += 15;

            // Character variety
            if (Password.Any(char.IsUpper)) strength += 25;
            if (Password.Any(char.IsLower)) strength += 25;
            if (Password.Any(char.IsDigit)) strength += 25;

            // Special characters
            if (Password.Any(ch => !char.IsLetterOrDigit(ch))) strength += 25;

            PasswordStrength = Math.Min(100, strength);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateAll()
        {
            ValidateEmail();
            ValidateFirstName();
            ValidateLastName();
            ValidatePassword();
            ValidateConfirmPassword();

            return IsEmailValid && IsFirstNameValid && IsLastNameValid &&
                   IsPasswordValid && IsConfirmPasswordValid && AcceptTerms;
        }

        // Command implementations
        private async Task ExecuteRegisterCommand()
        {
            if (IsBusy) return;

            if (!ValidateAll())
            {
                Message = "Please fix the validation errors";
                MessageColor = Colors.Red;
                return;
            }

            try
            {
                IsBusy = true;
                IsSuccess = false;
                Message = string.Empty;

                var registerDto = new RegisterDto
                {
                    Email = Email.Trim(),
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Password = Password,
                    ConfirmPassword = ConfirmPassword,
                    PhoneNumber = PhoneNumber?.Trim(),
                    AcceptTerms = AcceptTerms
                };

                var result = await _authService.RegisterAsync(registerDto);

                IsSuccess = result.Success;
                Message = result.Message;
                MessageColor = result.Success ? Colors.Green : Colors.Red;

                if (result.Success)
                {
                    // Clear form on success
                    await ClearForm();

                    // Optionally navigate to login or verification page
                    await Shell.Current.DisplayAlert("Success", result.Message, "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    // Show errors
                    if (result.Errors.Any())
                    {
                        Message = string.Join("\n", result.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                Message = "An error occurred during registration";
                MessageColor = Colors.Red;
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteGoToLoginCommand()
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }

        private async Task ClearForm()
        {
            Email = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            PhoneNumber = string.Empty;
            AcceptTerms = false;
            Message = string.Empty;

            await Task.CompletedTask;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
