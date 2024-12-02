using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudApi.Data;
using CrudApi.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using CrudApi.Services;
using System.Text.Json;

namespace CrudApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly PermissionService _permissionService;
        private readonly SecurityHeaderService _securityHeaderService;

        public RoleController(ApplicationDbContext context, IConfiguration configuration, PermissionService permissionService, SecurityHeaderService securityHeaderService)
        {
            _context = context;
            _configuration = configuration;
            _secretKey = _configuration["ApiSettings:SecretKey"];
            _permissionService = permissionService;
            _securityHeaderService = securityHeaderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanView", "api/role");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to view role.");
            }

            if (!_securityHeaderService.VerifySignature("GET", "api/role", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var roles = await _context.Roles.ToListAsync();
            if (roles == null || roles.Count == 0)
            {
                return NotFound("No roles found");
            }

            return Ok(roles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRole(int id)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanUpdate", "api/role");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to create role.");
            }

            if (!_securityHeaderService.VerifySignature("GET", $"api/role/{id}", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var role = await _context.Roles
                .Include(r => r.RoleMenus)
                .ThenInclude(rp => rp.Menu)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                role.Id,
                role.Name,
                role.Description,
                role.CanCreate,
                role.CanDelete,
                role.CanUpdate,
                role.CanView,
                MenuIds = role.RoleMenus.Select(rp => rp.Menu.Id).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] RoleRequest request)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanCreate", "api/role");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to create role.");
            }

            var body = JsonSerializer.Serialize(request);

            if (!_securityHeaderService.VerifySignature("POST", "api/role", body, clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            if (!ModelState.IsValid)
            return BadRequest(ModelState);
            
            var role = new Role{
                Name = request.Name,
                Description = request.Description,
                CanCreate = request.CanCreate,
                CanDelete = request.CanDelete,
                CanUpdate = request.CanUpdate,
                CanView = request.CanView
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            if (request.MenuIds == null || !request.MenuIds.Any())
            {
                return Ok(role);
            }
            else
            {
                foreach (var menuId in request.MenuIds)
                {
                    _context.RoleMenus.Add(new RoleMenus
                    {
                        RoleId = role.Id,
                        MenuId = menuId
                    });
                }

                await _context.SaveChangesAsync();
                return Ok(role);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] RoleRequest request)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanUpdate", "api/role");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to update role.");
            }

            var body = JsonSerializer.Serialize(request);

            if (!_securityHeaderService.VerifySignature("PUT", $"api/role/{id}", body, clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var role = await _context.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            role.Name = request.Name;
            role.Description = request.Description;
            role.CanCreate = request.CanCreate;
            role.CanDelete = request.CanDelete;
            role.CanUpdate = request.CanUpdate;
            role.CanView = request.CanView;

            var existingMenus = _context.RoleMenus.Where(rp => rp.RoleId == role.Id);
            _context.RoleMenus.RemoveRange(existingMenus);
            
            if (request.MenuIds != null && request.MenuIds.Any())
            {
                foreach (var menuId in request.MenuIds)
                {
                    _context.RoleMenus.Add(new RoleMenus
                    {
                        RoleId = role.Id,
                        MenuId = menuId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanDelete", "api/role");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to delete role.");
            }

            if (!_securityHeaderService.VerifySignature("DELETE", $"api/role/{id}", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var role = await _context.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return NoContent();
        }
      
    }
}
