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
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly PermissionService _permissionService;
        private readonly SecurityHeaderService _securityHeaderService;
        private readonly UserImplementation _userImplementation;
        private readonly RoleImplementation _roleImplementation;

        public UserController(ApplicationDbContext context,  IConfiguration configuration, PermissionService permissionService, SecurityHeaderService securityHeaderService, UserImplementation userImplementation, RoleImplementation roleImplementation)
        {
            _context = context;
            _configuration = configuration;
            _secretKey = _configuration["ApiSettings:SecretKey"];
            _permissionService = permissionService;
            _securityHeaderService = securityHeaderService;
            _userImplementation = userImplementation;
            _roleImplementation = roleImplementation;
        }

        [HttpGet("Get")]
        public async Task<IActionResult> GetUsersOnly([FromQuery] string searchQuery = "")
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanView", "api/user");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to view user.");
            }

            if (!_securityHeaderService.VerifySignature("GET", "api/user", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var users = await _userImplementation.GetUsersWithRoles(searchQuery);
            return Ok(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string searchQuery = "")
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanView", "api/user");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to view user.");
            }

            if (!_securityHeaderService.VerifySignature("GET", "api/user", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest("Invalid pagination parameters.");
            }

            var (users, totalCount) = await _userImplementation.GetUsersFiltered(searchQuery, pageNumber, pageSize);

            if (users == null || users.Count == 0)
            {
                return NotFound("No users found.");
            }

            var paginationResult = new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                Users = users
            };

            return Ok(paginationResult);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanUpdate", "api/user");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to create user.");
            }

            if (!_securityHeaderService.VerifySignature("GET", $"api/user/{id}", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var user = await _userImplementation.GetUserWithRole(id);

            if (user == null)
            {
                return NotFound();
            }

            var RoleId = user.UserRoles.Select(ur => ur.Role.Id).FirstOrDefault();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.FullName,
                user.Email,
                user.Status,
                user.MaxRetry,
                user.Retry,
                RoleId
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateResponse request)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanCreate", "api/user");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to create user.");
            }

            var body = JsonSerializer.Serialize(request);

            if (!_securityHeaderService.VerifySignature("POST", "api/user", body, clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var user = new User
            {
                Username = request.Username,
                FullName = request.FullName,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password), 
                Status = request.Status,
                MaxRetry = request.MaxRetry,
                Retry = request.Retry
            };

            var userCreated = await _userImplementation.CreateUser(user);

            if (request.RoleId != 0 && request.RoleId != null)  
            {
                var userRole = new UserRoles
                {
                    UserId = user.Id,
                    RoleId = request.RoleId  
                };

                var roleCreated = await _roleImplementation.CreateUserRole(userRole);
            }

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserResponse request)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanUpdate", "api/user");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to update user.");
            }

            var body = JsonSerializer.Serialize(request);

            if (!_securityHeaderService.VerifySignature("PUT", $"api/user/{id}", body, clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var user = await _userImplementation.GetUserById(id);

            if (user == null)
            {
                return NotFound();
            }

            user.Username = request.Username;
            user.FullName = request.FullName;
            user.Email = request.Email;
            if (request.Password != "@#%empty021&^") 
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
            user.Status = request.Status;
            user.MaxRetry = request.MaxRetry;
            user.Retry = request.Retry;

            var isUpdated = await _userImplementation.UpdateUser(user);

            if (!isUpdated)
            {
                return StatusCode(500, "Error updating user");
            }

            var userRole = await _roleImplementation.GetUserRoleById(id);

            if (userRole != null)
            {
                if (userRole.RoleId != request.RoleId) 
                {
                    var deleteUserRole = await _roleImplementation.DeleteUserRole(userRole.UserId, userRole.RoleId);
                    
                    if(!deleteUserRole)
                        return StatusCode(500, "Error updating user role");

                    if (request.RoleId != 0)
                    {
                        var newUserRole = new UserRoles
                        {
                            UserId = id,
                            RoleId = request.RoleId
                        };
                        var roleCreated = await _roleImplementation.CreateUserRole(newUserRole);
                    }
                }
            }
            else if (request.RoleId != 0) 
            {
                var newUserRole = new UserRoles
                {
                    UserId = id,
                    RoleId = request.RoleId
                };
                var roleCreated = await _roleImplementation.CreateUserRole(newUserRole);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanDelete", "api/user");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to delete user.");
            }

            if (!_securityHeaderService.VerifySignature("DELETE", $"api/user/{id}", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var user = await _userImplementation.GetUserById(id);

            if (user == null)
            {
                return NotFound();
            }

            var deleteUser = await _userImplementation.DeleteUser(user);

            if(!deleteUser)
                return StatusCode(500, "Error updating user role");

            return NoContent();
        }
    }
}
