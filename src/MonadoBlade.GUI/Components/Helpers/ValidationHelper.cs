using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MonadoBlade.GUI.Components.Helpers
{
    /// <summary>
    /// Validation helper for common input validation scenarios (80 LOC)
    /// Provides reusable validation methods to reduce duplication in components.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validation result
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }

            public ValidationResult(bool isValid = true, string error = "")
            {
                IsValid = isValid;
                ErrorMessage = error;
            }

            public static ValidationResult Success() => new(true);
            public static ValidationResult Failure(string error) => new(false, error);
        }

        /// <summary>
        /// Validate required field
        /// </summary>
        public static ValidationResult ValidateRequired(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Failure($"{fieldName} is required");
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate email format
        /// </summary>
        public static ValidationResult ValidateEmail(string email)
        {
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email, pattern))
                return ValidationResult.Failure("Invalid email format");
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate minimum length
        /// </summary>
        public static ValidationResult ValidateMinLength(string value, int minLength, string fieldName)
        {
            if (string.IsNullOrEmpty(value) || value.Length < minLength)
                return ValidationResult.Failure(
                    $"{fieldName} must be at least {minLength} characters");
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate maximum length
        /// </summary>
        public static ValidationResult ValidateMaxLength(string value, int maxLength, string fieldName)
        {
            if (value != null && value.Length > maxLength)
                return ValidationResult.Failure(
                    $"{fieldName} cannot exceed {maxLength} characters");
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate numeric value
        /// </summary>
        public static ValidationResult ValidateNumeric(string value, string fieldName)
        {
            if (!double.TryParse(value, out _))
                return ValidationResult.Failure($"{fieldName} must be a valid number");
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate number range
        /// </summary>
        public static ValidationResult ValidateRange(double value, double min, double max, string fieldName)
        {
            if (value < min || value > max)
                return ValidationResult.Failure(
                    $"{fieldName} must be between {min} and {max}");
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate URL format
        /// </summary>
        public static ValidationResult ValidateUrl(string url)
        {
            try
            {
                _ = new Uri(url);
                return ValidationResult.Success();
            }
            catch
            {
                return ValidationResult.Failure("Invalid URL format");
            }
        }

        /// <summary>
        /// Validate date format
        /// </summary>
        public static ValidationResult ValidateDate(string date, string format = "yyyy-MM-dd")
        {
            if (!DateTime.TryParseExact(date, format, 
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _))
                return ValidationResult.Failure($"Invalid date format. Expected {format}");
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate password strength
        /// </summary>
        public static ValidationResult ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return ValidationResult.Failure("Password must be at least 8 characters");

            var hasUpperCase = password.Any(char.IsUpper);
            var hasLowerCase = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

            if (!hasUpperCase || !hasLowerCase || !hasDigit || !hasSpecialChar)
                return ValidationResult.Failure(
                    "Password must contain uppercase, lowercase, digit, and special character");
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate collection not empty
        /// </summary>
        public static ValidationResult ValidateNotEmpty<T>(IEnumerable<T> collection, string fieldName)
        {
            if (collection == null || !collection.Any())
                return ValidationResult.Failure($"{fieldName} cannot be empty");
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate all results
        /// </summary>
        public static ValidationResult ValidateAll(params ValidationResult[] results)
        {
            var failedResult = results.FirstOrDefault(r => !r.IsValid);
            return failedResult ?? ValidationResult.Success();
        }

        /// <summary>
        /// Validate all results and collect errors
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateAllWithErrors(
            params ValidationResult[] results)
        {
            var errors = results.Where(r => !r.IsValid)
                .Select(r => r.ErrorMessage)
                .ToList();
            return (errors.Count == 0, errors);
        }
    }
}
