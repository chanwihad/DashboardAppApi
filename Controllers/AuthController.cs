using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CrudApi.Models;
using CrudApi.Data;
using CrudApi.Services;
using BCrypt.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

using System.IdentityModel.Tokens.Jwt;  
using System.Security.Claims;  
using Microsoft.IdentityModel.Tokens;  
using System.Text;  
using CrudApi.Implementations;


namespace CrudApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService; 
        private readonly ApplicationDbContext _context; 
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly PermissionService _permissionService;
        private readonly SecurityHeaderService _securityHeaderService;
        private readonly UserImplementation _userImplementation;

        public AuthController(ITokenService tokenService, ApplicationDbContext context, IConfiguration configuration, PermissionService permissionService, SecurityHeaderService securityHeaderService, UserImplementation userImplementation)
        {
            _tokenService = tokenService;
            _context = context;
            _configuration = configuration;
            _secretKey = _configuration["ApiSettings:SecretKey"];
            _permissionService = permissionService;
            _securityHeaderService = securityHeaderService;
            _userImplementation = userImplementation;
        }

        // [HttpPost("register")]
        // public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        // {
        //     if (await _context.Users.AnyAsync(u => u.Username == model.Username))
        //         return BadRequest("Username already exists.");

        //     var user = new User
        //     {
        //         Username = model.Username,
        //         FullName = model.FullName,
        //         Email = model.Email,
        //         Password = BCrypt.Net.BCrypt.HashPassword(model.Password), 
        //         Status = "Active",
        //         MaxRetry = 10,
        //         Retry = 0
        //     };

        //     _context.Users.Add(user);
        //     await _context.SaveChangesAsync();

        //     return Ok(new { Message = "Registration successful." });

        // }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (model == null)
                return BadRequest("Invalid credentials.");

            var user = await _userImplementation.GetUserForLogin(model.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))  
                return Unauthorized("Invalid username or password.");

            var role = user.UserRoles.FirstOrDefault()?.Role;

            if (role == null)
                return Unauthorized("User does not have a role assigned.");

            var token = _tokenService.GenerateToken(user, role);

            var menus = role.RoleMenus
                .Select(rm => new
                {
                    rm.Menu.Name,
                    rm.Menu.Url
                }).ToList();

            var permissions = new
            {
                CanCreate = role.CanCreate,
                CanView = role.CanView,
                CanUpdate = role.CanUpdate,
                CanDelete = role.CanDelete
            };

            return Ok(new { 
                Token = token, 
                Username = user.Username, 
                ClientId = $"{user.Id}", 
                Menus = menus,
                Permissions = permissions 
            });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel request)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var body = JsonSerializer.Serialize(request);

            if (!_securityHeaderService.VerifySignature("POST", "api/auth/change-password", body, clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var user = await _userImplementation.GetUserById(int.Parse(clientId));

            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            if(!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                return BadRequest("Old password is incorrect.");

            var updatePassword = await _userImplementation.UpdatePassword(int.Parse(clientId), BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
            
            if(!updatePassword)
                return BadRequest("Cannot change password");

            return Ok(new { Message = "Password changed successfully." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            // var resetToken = Guid.NewGuid().ToString();
            // user.PasswordResetToken = resetToken;

            user.Password = BCrypt.Net.BCrypt.HashPassword("password");  
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Password changed successfully." });
        }
    }
}
