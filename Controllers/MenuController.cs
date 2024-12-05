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
    public class MenuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly PermissionService _permissionService;
        private readonly SecurityHeaderService _securityHeaderService;
        private readonly MenuImplementation _menuImplementation;

        public MenuController(ApplicationDbContext context, IConfiguration configuration, PermissionService permissionService, SecurityHeaderService securityHeaderService, MenuImplementation menuImplementation)
        {
            _context = context;
            _configuration = configuration;
            _secretKey = _configuration["ApiSettings:SecretKey"];
            _permissionService = permissionService;
            _securityHeaderService = securityHeaderService;
            _menuImplementation = menuImplementation;
        }

        [HttpGet]
        public async Task<IActionResult> GetMenus([FromQuery] string searchQuery = "")
        {
            // var clientId = Request.Headers["X-Client-ID"];
            // var timeStamp = Request.Headers["X-Time-Stamp"];
            // var signature = Request.Headers["X-Signature"];

            // var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanView", "api/menu");
            // if (!hasPermission)
            // {
            //     return Forbid("You do not have permission to view menu.");
            // }

            // if (!_securityHeaderService.VerifySignature("GET", "api/menu", "", clientId, timeStamp, signature))
            // {
            //     return Unauthorized("Invalid signature");
            // }

            var menus = await _menuImplementation.GetMenus(searchQuery);
            if (menus == null || menus.Count == 0)
            {
                return NotFound("No menus found");
            }

            return Ok(menus);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMenu(int id)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanUpdate", "api/menu");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to create menu.");
            }

            if (!_securityHeaderService.VerifySignature("GET", $"api/menu/{id}", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }
            
            var menu = await _menuImplementation.GetMenuById(id);

            if (menu == null)
            {
                return NotFound();
            }

            return Ok(menu);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMenu([FromBody] MenuRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanCreate", "api/menu");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to create menu.");
            }

            var body = JsonSerializer.Serialize(request);

            if (!_securityHeaderService.VerifySignature("POST", "api/menu", body, clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }
                
            var newMenu = new Menu{
                Name = request.Name,
                Description = request.Description,
                Level1 = string.IsNullOrEmpty(request.Level1) ? null : request.Level1,
                Level2 = string.IsNullOrEmpty(request.Level2) ? null : request.Level2,
                Level3 = string.IsNullOrEmpty(request.Level3) ? null : request.Level3,
                Level4 = string.IsNullOrEmpty(request.Level4) ? null : request.Level4,
                Icon = string.IsNullOrEmpty(request.Icon) ? null : request.Icon,
                Url = request.Url

            };

            var menu = await _menuImplementation.CreateMenu(newMenu);

            return Ok(menu);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMenu(int id, [FromBody] MenuRequest request)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanUpdate", "api/menu");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to update menu.");
            }

            var body = JsonSerializer.Serialize(request);

            if (!_securityHeaderService.VerifySignature("PUT", $"api/menu/{id}", body, clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var menu = await _menuImplementation.GetMenuById(id);

            if (menu == null)
            {
                return NotFound();
            }

            menu.Name = request.Name;
            menu.Description = request.Description;
            menu.Level1 = string.IsNullOrEmpty(request.Level1) ? null : request.Level1;
            menu.Level2 = string.IsNullOrEmpty(request.Level2) ? null : request.Level2;
            menu.Level3 = string.IsNullOrEmpty(request.Level3) ? null : request.Level3;
            menu.Level4 = string.IsNullOrEmpty(request.Level4) ? null : request.Level4;
            menu.Icon = string.IsNullOrEmpty(request.Icon) ? null : request.Icon;
            menu.Url = request.Url;

            await _menuImplementation.UpdateMenu(menu);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanDelete", "api/menu");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to delete menu.");
            }

            if (!_securityHeaderService.VerifySignature("DELETE", $"api/menu/{id}", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var menu = await _menuImplementation.GetMenuById(id);

            if (menu == null)
            {
                return NotFound();
            }

            await _menuImplementation.DeleteMenu(menu.Id);

            return NoContent();
        }
    }
}