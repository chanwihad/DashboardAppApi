using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using CrudApi.Models;
using CrudApi.Data;
using Microsoft.EntityFrameworkCore;

namespace CrudApi.Implementations
{
    public class UserImplementation
    {
        private readonly IDbConnection _dbConnection;
        private readonly ApplicationDbContext _context;

        public UserImplementation(IDbConnection dbConnection, ApplicationDbContext context)
        {
            _dbConnection = dbConnection;
            _context = context;
        }

        public async Task<List<User>> GetUsers()
        {
            var sql = @"SELECT * FROM can_users";
            var users = await _dbConnection.QueryAsync<User>(sql);
            return users.ToList();
        }

        public async Task<(List<UserRequest>, int)> GetUsersFiltered(string search, int pageNumber, int pageSize)
        {
            var sql = @"SELECT u.""Id"", u.""Username"", u.""FullName"", u.""Email"", u.""Status"", u.""MaxRetry"", u.""Retry"", r.""Id"" AS RoleId, r.""Name"" AS RoleName
                FROM can_users u
                INNER JOIN can_userroles ur ON u.""Id"" = ur.""UserId""
                INNER JOIN can_roles r ON ur.""RoleId"" = r.""Id""";
            var countSql = @"SELECT u.""Id"", u.""Username"", u.""FullName"", u.""Email"", u.""Status"", u.""MaxRetry"", u.""Retry"", r.""Id"" AS RoleId, r.""Name"" AS RoleName
                FROM can_users u
                INNER JOIN can_userroles ur ON u.""Id"" = ur.""UserId""
                INNER JOIN can_roles r ON ur.""RoleId"" = r.""Id""";

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += @" WHERE ""Username"" ILIKE @Search OR ""FullName"" ILIKE @Search OR ""Email"" ILIKE @Search OR ""Status"" ILIKE @Search";

                countSql += @" WHERE ""Username"" ILIKE @Search OR ""FullName"" ILIKE @Search OR ""Email"" ILIKE @Search OR ""Status"" ILIKE @Search";
            }

            sql += " ORDER BY \"Id\" Desc LIMIT @PageSize OFFSET @Offset";

            var offset = (pageNumber - 1) * pageSize;

            var totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, new { Search = $"%{search}%" });
            var users = await _dbConnection.QueryAsync<UserRequest>(sql, new { Search = $"%{search}%", PageSize = pageSize, Offset = offset });

            return (users.ToList(), totalCount);
        }

        public async Task<User> GetUserById(int id)
        {
            var sql = @"SELECT * FROM can_users WHERE ""Id"" = @Id";
            var user = await _dbConnection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
            return user;
        }

        public async Task<User> GetUserByEmail(string email)
        {
            var sql = @"SELECT * FROM can_users WHERE ""Email"" = @Email";
            var user = await _dbConnection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
            return user;
        }

        public async Task<User> GetUserForLogin(string username)
        {
            // var sql = @"
            //     SELECT u.*, ur.""RoleId"", r.""Name"" AS RoleName
            //     FROM can_users u
            //     INNER JOIN can_userroles ur ON u.""Id"" = ur.""UserId""
            //     INNER JOIN can_roles r ON ur.""RoleId"" = r.""Id""
            //     WHERE u.""Username"" = @Username";
            
            // var users = await _dbConnection.QueryAsync<User, Role, User>(
            //     sql,
            //     (user, role) =>
            //     {
            //         user.UserRoles = user.UserRoles ?? new List<UserRoles>();
            //         user.UserRoles.Add(new UserRoles
            //         {
            //             Role = role
            //         });
            //         return user;
            //     },
            //     new { Username = username },
            //     splitOn: "RoleId"
            // );

            // return users.FirstOrDefault();

            var user =  await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RoleMenus)
                .ThenInclude(rm => rm.Menu)
                .FirstOrDefaultAsync(u => u.Username == username);
            
            return user;
        }

        public async Task<bool> UpdatePassword(int id, string password)
        {
            var sql = @"UPDATE can_users SET ""Password"" = @Password WHERE ""Id"" = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = id, Password = password });
            return affectedRows > 0;
        }

        public async Task<IEnumerable<object>> GetUsersWithRoles(string search)
        {
            var sql = @"
                SELECT u.""Id"", u.""Username"", u.""FullName"", u.""Email"", u.""Status"", u.""MaxRetry"", u.""Retry"", r.""Name"" AS RoleName
                FROM can_users u
                INNER JOIN can_userroles ur ON u.""Id"" = ur.""UserId""
                INNER JOIN can_roles r ON ur.""RoleId"" = r.""Id""";

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += @" WHERE ""Username"" ILIKE @Search OR ""FullName"" ILIKE @Search OR ""Email"" ILIKE @Search OR ""Status"" ILIKE @Search";
            }
            
            var userList = await _dbConnection.QueryAsync(sql, new { Search = $"%{search}%" });
            return userList;
        }

        public async Task<User> GetUserWithRole(int id)
        {
            var sql = @"
                SELECT u.*, ur.""RoleId"", r.""Name"" AS RoleName
                FROM can_users u
                INNER JOIN can_userroles ur ON u.""Id"" = ur.""UserId""
                INNER JOIN can_roles r ON ur.""RoleId"" = r.""Id""
                WHERE u.""Id"" = @Id";
            
            var users = await _dbConnection.QueryAsync<User, Role, User>(
                sql,
                (user, role) =>
                {
                    user.UserRoles = user.UserRoles ?? new List<UserRoles>();
                    user.UserRoles.Add(new UserRoles
                    {
                        Role = role
                    });
                    return user;
                },
                new { Id = id },
                splitOn: "RoleId"
            );

            return users.FirstOrDefault();
        }

        public async Task<User> CreateUser(User model)
        {
            var sql = @"
                INSERT INTO can_users (""Username"", ""Password"", ""FullName"", ""Email"", ""Status"", ""MaxRetry"", ""Retry"")
                VALUES (@Username, @Password, @FullName, @Email, @Status, @MaxRetry, @Retry)
                RETURNING ""Id""";

            var id = await _dbConnection.ExecuteScalarAsync<int>(sql, model);
            model.Id = id;
            return model;
        }

        public async Task<bool> UpdateUser(User model)
        {
            var sql = @"
                UPDATE can_users
                SET ""Username"" = @Username, ""Password"" = @Password, ""FullName"" = @FullName, 
                    ""Email"" = @Email, ""Status"" = @Status, ""MaxRetry"" = @MaxRetry, ""Retry"" = @Retry
                WHERE ""Id"" = @Id";
            
            var affectedRows = await _dbConnection.ExecuteAsync(sql, model);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteUser(User model)
        {
            var sql = @"DELETE FROM can_users WHERE ""Id"" = @Id";
            var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = model.Id });
            return affectedRows > 0;
        }

        public async Task<PasswordReset> NewPasswordReset(PasswordReset model)
        {
            var sql = @"
                INSERT INTO can_passwordresets (""Email"", ""VerificationCode"", ""ExpiryTime"")
                VALUES (@Email, @VerificationCode, @ExpiryTime)
                RETURNING ""Id""";

            var id = await _dbConnection.ExecuteScalarAsync<int>(sql, model);
            model.Id = id;
            return model;
        }

        public async Task<PasswordReset> GetPasswordReset(VerificationRequest model)
        {
            var sql = @"SELECT * FROM can_passwordresets WHERE ""Email"" = @Email AND ""VerificationCode"" = @Code";
            var query = await _dbConnection.QueryFirstOrDefaultAsync<PasswordReset>(sql, new { Email = model.Email, Code = model.Code });
            return query;
        }

        public async Task DeleteAllByEmail(string email)
        {
            var sql = @"DELETE FROM can_passwordresets WHERE ""Email"" = @Email";
            var query = await _dbConnection.ExecuteAsync(sql, new {Email = email});
        }
    }
}
