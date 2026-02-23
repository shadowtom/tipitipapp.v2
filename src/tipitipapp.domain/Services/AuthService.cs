using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using tipitipapp.domain.Entities;
using tipitipapp.domain.Entities.DTOs;
using tipitipapp.domain.Interfaces;
using tipitipapp.domain.Interfaces.Repository;
using tipitipapp.domain.Interfaces.Services;
using tipitipapp.domain.Utilities;

namespace tipitipapp.domain.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(IServiceProvider serviceProvider)
        {
            _userRepository = serviceProvider.GetRequiredService<IUserRepository>();
            _passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();
        }

        public async Task<(bool success, string message)> LoginAsync(LoginDto loginDto)
        {
            User user = await _userRepository.GetByEmailAsync(loginDto.Email);

            if (user == null)
                return (false, "Invalid email or password");

            if (!_passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
                return (false, "Invalid email or password");

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return (true, "Login successful");
        }

        public async Task<RegisterResultDto> RegisterAsync(RegisterDto registerDto)
        {
            var result = new RegisterResultDto();

            // Validate input
            var validationErrors = await Validations.ValidateRegistrationAsync(registerDto);
            if (validationErrors.Any())
            {
                result.Errors.AddRange(validationErrors);
                result.Message = "Registration validation failed";
                return result;
            }

            try
            {
                // Check if email already exists
                var existingUser = await _userRepository.GetByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    result.Errors.Add("Email is already registered");
                    result.Message = "Registration failed";
                    return result;
                }

                // Create new user
                var user = new User
                {
                    Email = registerDto.Email.Trim().ToLower(),
                    FirstName = registerDto.FirstName.Trim(),
                    LastName = registerDto.LastName.Trim(),
                    PhoneNumber = registerDto.PhoneNumber?.Trim(),
                    PasswordHash = _passwordHasher.HashPassword(registerDto.Password),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Save to database
                await _userRepository.AddAsync(user);

                result.Success = true;
                result.Message = "Registration successful!";
                result.UserId = user.Id;
                result.Email = user.Email;
            }
            catch (Exception ex)
            {
                result.Errors.Add("An error occurred during registration. Please try again.");
                // Log exception
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
            }

            return result;
        }
    }
}
