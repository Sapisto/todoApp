namespace PracticeApi.Domain.DTO
{
    public class GetAllUsersDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
    public class GetAllUsersResponseDto
    {
        public bool Succeeded { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    public class GetUserByIdResponseDto
    {
        public bool Succeeded { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public GetAllUsersDto Data { get; set; } // Single user data
    }
}
