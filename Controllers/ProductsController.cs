using Microsoft.AspNetCore.Mvc;
using CrudApi.Data;
using CrudApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CrudApi.Services;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;

namespace CrudApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly PermissionService _permissionService;
        private readonly SecurityHeaderService _securityHeaderService;

        public ProductsController(ApplicationDbContext context, IConfiguration configuration, ProductService productService, PermissionService permissionService, SecurityHeaderService securityHeaderService)
        {
            _context = context;
            _configuration = configuration;
            _secretKey = _configuration["ApiSettings:SecretKey"];
            _productService = productService;
            _permissionService = permissionService;
            _securityHeaderService = securityHeaderService;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
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

            return await _productService.GetProductsAsync();
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

            var product = await _productService.GetProductByIdAsync(id);
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

            var createdProduct = await _productService.CreateProductAsync(product);
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
            var updatedProduct = await _productService.UpdateProductAsync(product);
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
            await _productService.DeleteProductAsync(id);
            return NoContent();
        }
    }

}