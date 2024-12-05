using System.Data;
using Dapper;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CrudApi.Models;

namespace CrudApi.Implementations
{
    public class ProductImplementation
    {
        private readonly IDbConnection _dbConnection;

        public ProductImplementation(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            var sql = "SELECT * FROM can_products"; 
            var products = await _dbConnection.QueryAsync<Product>(sql);
            return products.ToList();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            var sql = @"SELECT * FROM can_products WHERE ""Id"" = @Id"; 
            var product = await _dbConnection.QueryFirstOrDefaultAsync<Product>(sql, new { Id = id });
            return product;
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            var sql = @"
                INSERT INTO can_products (""Name"", ""Price"") 
                VALUES (@Name, @Price)
                RETURNING ""Id""";

            var id = await _dbConnection.ExecuteScalarAsync<int>(sql, product);
            product.Id = id;
            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            var sql = @"
                UPDATE can_products
                SET 
                    ""Name"" = @Name,
                    ""Price"" = @Price
                WHERE ""Id"" = @Id";

            var affectedRows = await _dbConnection.ExecuteAsync(sql, product);
            if (affectedRows > 0)
            {
                return product;
            }
            return null;
        }

        public async Task DeleteProductAsync(int id)
        {
            var sql = @"DELETE FROM can_products WHERE ""Id"" = @Id"; 
            await _dbConnection.ExecuteAsync(sql, new { Id = id });
        }
    }
}
