namespace CrudApi.Models
{
    public class PasswordReset
    {   
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
    }

}