using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudApi.Data;
using CrudApi.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using CrudApi.Services;
using System.Text.Json;
using CrudApi.Implementations;

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
        private readonly RoleImplementation _roleImplementation;
        private readonly MenuImplementation _menuImplementation;

        public RoleController(ApplicationDbContext context, IConfiguration configuration, PermissionService permissionService, SecurityHeaderService securityHeaderService, RoleImplementation roleImplementation, MenuImplementation menuImplementation)
        {
            _context = context;
            _configuration = configuration;
            _secretKey = _configuration["ApiSettings:SecretKey"];
            _permissionService = permissionService;
            _securityHeaderService = securityHeaderService;
            _roleImplementation = roleImplementation;
            _menuImplementation = menuImplementation;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string searchQuery = "")
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

            var (roles, totalCount)  = await _roleImplementation.GetRolesFiltered(searchQuery, pageNumber, pageSize);

            if (roles == null || roles.Count == 0)
            {
                return NotFound("No roles found.");
            }

            var paginationResult = new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                Roles = roles
            };

            return Ok(paginationResult);
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

            var role = await _roleImplementation.GetRoleWithMenu(id);

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
            
            var newRole = new Role{
                Name = request.Name,
                Description = request.Description,
                CanCreate = request.CanCreate,
                CanDelete = request.CanDelete,
                CanUpdate = request.CanUpdate,
                CanView = request.CanView
            };

            var role = await _roleImplementation.CreateRole(newRole);
            
            if(role == null)
                return StatusCode(500, "Error creating role");

            if (request.MenuIds == null || !request.MenuIds.Any())
            {
                return Ok(role);
            }
            else
            {
                await _menuImplementation.SaveRoleMenusBatch(role.Id, request.MenuIds);
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

            var role = await _roleImplementation.GetRoleById(id);

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

            await _roleImplementation.UpdateRole(role);

            var deleteExistingMenus = _menuImplementation.DeleteExistingRoleMenus(role.Id);
            
            if (request.MenuIds != null && request.MenuIds.Any())
            {
                await _menuImplementation.SaveRoleMenusBatch(role.Id, request.MenuIds);
            }

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

            var role = await _roleImplementation.GetRoleById(id);

            if (role == null)
            {
                return NotFound();
            }

            var saveDelete = await _roleImplementation.DeleteRole(id);

            return NoContent();
        }
      
    }
}
