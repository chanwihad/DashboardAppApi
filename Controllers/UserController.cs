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
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly PermissionService _permissionService;
        private readonly SecurityHeaderService _securityHeaderService;

        public UserController(ApplicationDbContext context,  IConfiguration configuration, PermissionService permissionService, SecurityHeaderService securityHeaderService)
        {
            _context = context;
            _configuration = configuration;
            _secretKey = _configuration["ApiSettings:SecretKey"];
            _permissionService = permissionService;
            _securityHeaderService = securityHeaderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
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

            var users = await _context.Users
                .Include(u => u.UserRoles) 
                .ThenInclude(ur => ur.Role) 
                .ToListAsync();

            var userList = users.Select(user => new
            {
                user.Id,
                user.Username,
                user.FullName,
                user.Email,
                user.Status,
                user.MaxRetry,
                user.Retry,
                RoleName = user.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault()
            });

            return Ok(userList);

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

            var user = await _context.Users
                .Include(r => r.UserRoles)
                .ThenInclude(rp => rp.Role)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var RoleId = user.UserRoles.Select(ur => ur.Role.Id).FirstOrDefault();

            // return Ok(user);
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

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (request.RoleId != 0 || request.RoleId != null)  
            {
                var userRole = new UserRoles
                {
                    UserId = user.Id,
                    RoleId = request.RoleId  
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            // return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
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

            var user = await _context.Users.FindAsync(id);

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

            // _context.Users.Update(user);

            var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == id);

            if (userRole != null)
            {
                if (userRole.RoleId != request.RoleId) 
                {
                    _context.UserRoles.Remove(userRole);
                    await _context.SaveChangesAsync(); 

                    if (request.RoleId != 0)
                    {
                        var newUserRole = new UserRoles
                        {
                            UserId = id,
                            RoleId = request.RoleId
                        };
                        _context.UserRoles.Add(newUserRole);
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
                _context.UserRoles.Add(newUserRole);
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

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
