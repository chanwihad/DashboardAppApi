using System.Text.Json.Serialization;

namespace CrudApi.Models
{
    public class RoleMenus
    {
        public int RoleId { get; set; }

        [JsonIgnore]
        public Role Role { get; set; }

        public int MenuId { get; set; }

        [JsonIgnore]
        public Menu Menu { get; set; }
    }
}
