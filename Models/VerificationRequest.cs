namespace CrudApi.Models
{
    public class VerificationRequest
    {   
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

}