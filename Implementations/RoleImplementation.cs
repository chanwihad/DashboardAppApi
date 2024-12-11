using System.Data;
using Dapper;
using CrudApi.Models;

namespace CrudApi.Implementations
{
    public class RoleImplementation
    {
        private readonly IDbConnection _dbConnection;

        public RoleImplementation(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<List<Role>> GetRoles(string search)
        {
            var sql = @"SELECT * FROM can_roles"; 
            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += @" WHERE ""Name"" ILIKE @Search OR ""Description"" ILIKE @Search";
            }
            var roles = await _dbConnection.QueryAsync<Role>(sql, new { Search = $"%{search}%" });
            return roles.ToList();
        }

        public async Task<(List<Role>, int)> GetRolesFiltered(string search, int pageNumber, int pageSize)
        {
            var sql = @"SELECT * FROM can_roles";
            var countSql = @"SELECT COUNT(*) FROM can_roles";

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += @" WHERE ""Name"" ILIKE @Search OR ""Description"" ILIKE @Search";
                countSql += @" WHERE ""Name"" ILIKE @Search OR ""Description"" ILIKE @Search";
            }

            sql += " ORDER BY \"Id\" Desc LIMIT @PageSize OFFSET @Offset";

            var offset = (pageNumber - 1) * pageSize;

            var totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, new { Search = $"%{search}%" });
            var roles = await _dbConnection.QueryAsync<Role>(sql, new { Search = $"%{search}%", PageSize = pageSize, Offset = offset });

            return (roles.ToList(), totalCount);
        }

        public async Task<Role> GetRoleById(int id)
        {
            var sql = @"SELECT * FROM can_roles WHERE ""Id"" = @Id"; 
            var role = await _dbConnection.QueryFirstOrDefaultAsync<Role>(sql, new { Id = id });
            return role;
        }

        public async Task<Role> GetRoleWithMenu(int id)
        {
            var sql = @"
                SELECT r.*, m.*
                FROM can_roles r
                INNER JOIN can_rolemenus rm ON r.""Id"" = rm.""RoleId""
                INNER JOIN can_menus m ON rm.""MenuId"" = m.""Id""
                WHERE r.""Id"" = @Id"; 
                
            if(sql == null)
                sql = @"SELECT * FROM can_roles WHERE Id = @Id";
            
            var roleWithMenus = await _dbConnection.QueryAsync<Role, Menu, Role>(
                sql,
                (role, menu) =>
                {
                    role.RoleMenus = role.RoleMenus ?? new List<RoleMenus>();
                    role.RoleMenus.Add(new RoleMenus
                    {
                        Menu = menu
                    });
                    return role;
                },
                new { Id = id },
                splitOn: "Id"
            );

            return roleWithMenus.FirstOrDefault();
        }

        public async Task<Role> CreateRole(Role model)
        {
            var sql = @"
                INSERT INTO can_roles 
                    (""Name"", ""Description"", ""CanCreate"", ""CanDelete"", ""CanUpdate"", ""CanView"") 
                VALUES 
                    (@Name, @Description, @CanCreate, @CanDelete, @CanUpdate, @CanView)
                RETURNING ""Id""";

            var id = await _dbConnection.ExecuteScalarAsync<int>(sql, model);
            model.Id = id;
            return model;
        }

        public async Task UpdateRole(Role role)
        {
            var sql = @"
                UPDATE can_roles
                SET 
                    ""Name"" = @Name,
                    ""Description"" = @Description,
                    ""CanCreate"" = @CanCreate,
                    ""CanDelete"" = @CanDelete,
                    ""CanUpdate"" = @CanUpdate,
                    ""CanView"" = @CanView
                WHERE ""Id"" = @Id";

            await _dbConnection.ExecuteAsync(sql, role);
        }

        public async Task<bool> DeleteRole(int id)
        {
            var sql = @"DELETE FROM can_roles WHERE ""Id"" = @Id"; 
            var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = id });
            return affectedRows > 0;
        }

        public async Task<UserRoles> GetUserRoleById(int id)
        {
            var sql = @"SELECT * FROM can_userroles WHERE ""UserId"" = @Id"; 
            var userRole = await _dbConnection.QueryFirstOrDefaultAsync<UserRoles>(sql, new { Id = id });
            return userRole;
        }

        public async Task<UserRoles> CreateUserRole(UserRoles model)
        {
            var sql = @"
                INSERT INTO can_userroles (""UserId"", ""RoleId"") 
                VALUES (@UserId, @RoleId)";
            
            await _dbConnection.ExecuteAsync(sql, model);
            return model;
        }

        public async Task<bool> DeleteUserRole(int userId, int roleId)
        {
            var sql = @"DELETE FROM can_userroles WHERE ""UserId"" = @UserId AND ""RoleId"" = @RoleId"; 
            var affectedRows = await _dbConnection.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId });
            return affectedRows > 0;
        }
    }
}
