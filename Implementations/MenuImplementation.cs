using System.Data;
using Dapper;
using CrudApi.Models;
using CrudApi.Data;
using Microsoft.EntityFrameworkCore;

namespace CrudApi.Implementations
{
    public class MenuImplementation
    {
        private readonly IDbConnection _dbConnection;
        private readonly ApplicationDbContext _context;

        public MenuImplementation(IDbConnection dbConnection, ApplicationDbContext context)
        {
            _dbConnection = dbConnection;
            _context = context;
        }

        public async Task<List<Menu>> GetMenus(string search)
        {
            var sql = @"SELECT * FROM can_menus";

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += @" WHERE ""Name"" ILIKE @Search OR ""Description"" ILIKE @Search";
            }

            var menus = await _dbConnection.QueryAsync<Menu>(sql, new { Search = $"%{search}%" });
            
            return menus.ToList();
        }


        public async Task<Menu> GetMenuById(int id)
        {
            var sql = @"SELECT * FROM can_menus WHERE ""Id"" = @Id"; 
            var menu = await _dbConnection.QueryFirstOrDefaultAsync<Menu>(sql, new { Id = id });
            return menu;
        }

        public async Task<Menu> CreateMenu(Menu model)
        {
            var sql = @"
                INSERT INTO can_menus 
                    (""Name"", ""Description"", ""Level1"", ""Level2"", ""Level3"", ""Level4"", ""Icon"", ""Url"") 
                VALUES 
                    (@Name, @Description, @Level1, @Level2, @Level3, @Level4, @Icon, @Url) 
                RETURNING ""Id"""; 
            var id = await _dbConnection.ExecuteScalarAsync<int>(sql, model);
            model.Id = id;
            return model;
        }

        public async Task UpdateMenu(Menu menu)
        {
            var sql = @"
                UPDATE can_menus
                SET ""Name"" = @Name,
                    ""Description"" = @Description,
                    ""Level1"" = @Level1,
                    ""Level2"" = @Level2,
                    ""Level3"" = @Level3,
                    ""Level4"" = @Level4,
                    ""Icon"" = @Icon,
                    ""Url"" = @Url
                WHERE ""Id"" = @Id"; 

            await _dbConnection.ExecuteAsync(sql, menu);
        }

        public async Task DeleteMenu(int id)
        {
            var sql = @"DELETE FROM can_menus WHERE ""Id"" = @Id"; 
            await _dbConnection.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<RoleMenus> RoleMenuSave(RoleMenus model)
        {
            var sql = @"
                INSERT INTO can_rolemenus (""RoleId"", ""MenuId"") 
                VALUES (@RoleId, @MenuId);"; 

            await _dbConnection.ExecuteAsync(sql, model);
            return model;
        }

        public async Task SaveRoleMenusBatch(int roleId, IEnumerable<int> menuIds)
        {
            var sql = @"INSERT INTO can_rolemenus (""RoleId"", ""MenuId"") VALUES (@RoleId, @MenuId)"; 
            var parameters = menuIds.Select(menuId => new { RoleId = roleId, MenuId = menuId }).ToArray();
            await _dbConnection.ExecuteAsync(sql, parameters);
        }

        public async Task DeleteExistingRoleMenus(int roleId)
        {
            var sql = @"DELETE FROM can_rolemenus WHERE ""RoleId"" = @RoleId"; 
            await _dbConnection.ExecuteAsync(sql, new { RoleId = roleId });
        }
    }
}
