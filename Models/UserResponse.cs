namespace CrudApi.Models
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Status { get; set; }
        public int MaxRetry { get; set; }
        public int Retry { get; set; }
        public int RoleId { get; set; }
    }
}
