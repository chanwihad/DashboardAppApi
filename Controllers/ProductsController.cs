using Microsoft.AspNetCore.Mvc;
using CrudApi.Data;
using CrudApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CrudApi.Services;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using CrudApi.Implementations;

namespace CrudApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductImplementation _productImplementation;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly PermissionService _permissionService;
        private readonly SecurityHeaderService _securityHeaderService;

        public ProductsController(ApplicationDbContext context, IConfiguration configuration, ProductImplementation productImplementation, PermissionService permissionService, SecurityHeaderService securityHeaderService)
        {
            _context = context;
            _configuration = configuration;
            _secretKey = _configuration["ApiSettings:SecretKey"];
            _productImplementation = productImplementation;
            _permissionService = permissionService;
            _securityHeaderService = securityHeaderService;
        }


        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsOnly()
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanView", "api/product");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to view products.");
            }

            if (!_securityHeaderService.VerifySignature("GET", "/api/products", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            return await _productImplementation.GetProductsAsync();
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string searchQuery = "")
        {
            // var clientId = Request.Headers["X-Client-ID"];
            // var timeStamp = Request.Headers["X-Time-Stamp"];
            // var signature = Request.Headers["X-Signature"];

            // var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanView", "api/product");
            // if (!hasPermission)
            // {
            //     return Forbid("You do not have permission to view product.");
            // }

            // if (!_securityHeaderService.VerifySignature("GET", "api/product", "", clientId, timeStamp, signature))
            // {
            //     return Unauthorized("Invalid signature");
            // }

            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest("Invalid pagination parameters.");
            }

            var (products, totalCount) = await _productImplementation.GetProductsFiltered(searchQuery, pageNumber, pageSize);

            if (products == null || products.Count == 0)
            {
                return NotFound("No products found.");
            }

            var paginationResult = new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                Products = products
            };

            return Ok(paginationResult);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanUpdate", "api/product");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to create products.");
            }

            if (!_securityHeaderService.VerifySignature("GET", $"/api/products/{id}", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var product = await _productImplementation.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return product;
        }

        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanCreate", "api/product");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to create products.");
            }

            var body = JsonSerializer.Serialize(product);

            if (!_securityHeaderService.VerifySignature("POST", "/api/products", body, clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var createdProduct = await _productImplementation.CreateProductAsync(product);
            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanUpdate", "api/product");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to update products.");
            }

            var body = JsonSerializer.Serialize(product);

            if (!_securityHeaderService.VerifySignature("PUT", $"/api/products/{id}", body, clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            if (id != product.Id) return BadRequest();
            var updatedProduct = await _productImplementation.UpdateProductAsync(product);
            return Ok(updatedProduct);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var clientId = Request.Headers["X-Client-ID"];
            var timeStamp = Request.Headers["X-Time-Stamp"];
            var signature = Request.Headers["X-Signature"];

            var hasPermission = await _permissionService.HasPermissionAsync(clientId, "CanDelete", "api/product");
            if (!hasPermission)
            {
                return Forbid("You do not have permission to delete products.");
            }

            if (!_securityHeaderService.VerifySignature("DELETE", $"/api/products/{id}", "", clientId, timeStamp, signature))
            {
                return Unauthorized("Invalid signature");
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            await _productImplementation.DeleteProductAsync(id);
            return NoContent();
        }
    }

}