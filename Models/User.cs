using System.Text.Json.Serialization;

namespace CrudApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } 
        public string Status { get; set; }
        public int MaxRetry { get; set; }
        public int Retry { get; set; }

        [JsonIgnore]
        public ICollection<UserRoles> UserRoles { get; set; }
    }
}
