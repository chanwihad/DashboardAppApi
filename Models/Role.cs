using System.Text.Json.Serialization;

namespace CrudApi.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool CanView { get; set; }    
        public bool CanCreate { get; set; } 
        public bool CanUpdate { get; set; } 
        public bool CanDelete { get; set; } 

        [JsonIgnore]
        public ICollection<UserRoles> UserRoles { get; set; }

        [JsonIgnore]
        public ICollection<RoleMenus> RoleMenus { get; set; } 
    }
}
