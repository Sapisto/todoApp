namespace PracticeApi.Core.Constants
{
    public static class ErrorMessages
    {
        public const string RequiredFields = "All fields are required.";
        public const string InvalidEmailFormat = "Invalid email format.";
        public const string InvalidPasswordFormat = "Password must be at least 8 characters long, contain one uppercase letter, one lowercase letter, one digit, and one special character.";
        public const string PasswordsDoNotMatch = "Passwords do not match.";
        public const string EmailAlreadyInUse = "Email already in use.";
        public const string UsernameAlreadyInUse = "Username already in use.";
    }
}
