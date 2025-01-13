using System.ComponentModel.DataAnnotations;

namespace PracticeApi.Domain.DTO
{
    public class UserLoginRequest
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
    public class UserLoginResponse
    {
        public bool Succeeded { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string Token { get; set; }
    }

    public class UserUpdateRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }


}
