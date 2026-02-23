using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using tipitipapp.domain.Entities.DTOs;

namespace tipitipapp.domain.Utilities
{
    public static class Validations
    {
        public static bool IsValidEmail(string email)
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
        public static async Task<List<string>> ValidateRegistrationAsync(RegisterDto dto)
        {
            var errors = new List<string>();

            // Email validation
            if (string.IsNullOrWhiteSpace(dto.Email))
                errors.Add("Email is required");
            else if (!IsValidEmail(dto.Email))
                errors.Add("Invalid email format");

            // Password validation
            if (string.IsNullOrWhiteSpace(dto.Password))
                errors.Add("Password is required");
            else
            {
                if (dto.Password.Length < 6)
                    errors.Add("Password must be at least 6 characters");
                if (!Regex.IsMatch(dto.Password, @"[A-Z]"))
                    errors.Add("Password must contain at least one uppercase letter");
                if (!Regex.IsMatch(dto.Password, @"[a-z]"))
                    errors.Add("Password must contain at least one lowercase letter");
                if (!Regex.IsMatch(dto.Password, @"[0-9]"))
                    errors.Add("Password must contain at least one number");
            }        // Name validation
            if (string.IsNullOrWhiteSpace(dto.FirstName))
                errors.Add("First name is required");
            else if (dto.FirstName.Length < 2)
                errors.Add("First name must be at least 2 characters");

            if (string.IsNullOrWhiteSpace(dto.LastName))
                errors.Add("Last name is required");
            else if (dto.LastName.Length < 2)
                errors.Add("Last name must be at least 2 characters");

            // Terms acceptance
            if (!dto.AcceptTerms)
                errors.Add("You must accept the terms and conditions");

            return errors;
        }
    }
}
