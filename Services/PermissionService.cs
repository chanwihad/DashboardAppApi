using Microsoft.EntityFrameworkCore;
using CrudApi.Data;
using CrudApi.Models;
using System.Text;  

namespace CrudApi.Services
{
    public class PermissionService
    {
        private readonly ApplicationDbContext _context;

        public PermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPermissionAsync(string clientId, string action, string url)
        {
            var idUser = int.Parse(clientId);

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RoleMenus)
                .ThenInclude(rm => rm.Menu)
                .FirstOrDefaultAsync(u => u.Id == idUser);

            if (user == null)
                return false;

            var role = user.UserRoles.FirstOrDefault()?.Role;

            if (role == null)
                return false;

            var hasActionPermission = action switch
            {
                "CanCreate" => role.CanCreate,
                "CanView" => role.CanView,
                "CanUpdate" => role.CanUpdate,
                "CanDelete" => role.CanDelete,
                _ => false
            };

            var hasMenuPermission = role.RoleMenus.Any(rm => rm.Menu.Url.Equals(url, StringComparison.OrdinalIgnoreCase));

            return hasActionPermission && hasMenuPermission;

            // return hasActionPermission;
        }
    }
}
