using CrudApi.Data;
using CrudApi.Models;
using BCrypt.Net;

namespace CrudApi.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider, ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Roles.Any())
            {
                return; 
            }

            var adminRole = new Role { Name = "Admin", Description = "Administrator Role", CanView = true, CanCreate = true, CanUpdate = true, CanDelete = true };
            var userRole = new Role { Name = "User", Description = "User Role", CanView = true, CanCreate = true, CanUpdate = true };
            context.Roles.AddRange(adminRole, userRole);
            context.SaveChanges();  

            var userManage = new Menu { Name = "Manage User", Description = "Can manage user.", Level1 = "User Management", Level2 = "User", Url = "api/user" };
            var roleManage = new Menu { Name = "Manage Role", Description = "Can manage a role.", Level1 = "User Management", Level2 = "Role", Url = "api/role" };
            var menuManage = new Menu { Name = "Manage Menu", Description = "Can manage a menu.", Level1 = "User Management", Level2 = "Menu", Url = "api/menu"  };            
            var productManage = new Menu { Name = "Manage Product", Description = "Can manage a product.", Level1 = "Products", Level2 = "List", Url = "api/product"  };

            context.Menus.AddRange(
                userManage, roleManage, menuManage, productManage
            );
            context.SaveChanges();

            var admin = new User
            {
                Username = "admin",
                FullName = "Admin User",
                Email = "admin@admin.com",
                Password = BCrypt.Net.BCrypt.HashPassword("@dgunner3636X"), 
                Status = "Active",
                MaxRetry = 100,
                Retry = 0
            };

            var user = new User
            {
                Username = "user",
                FullName = "Regular User",
                Email = "user@admin.com",
                Password = BCrypt.Net.BCrypt.HashPassword("@dgunner3636X"), 
                Status = "Active",
                MaxRetry = 10,
                Retry = 0
            };

            context.Users.AddRange(admin, user);
            context.SaveChanges(); 

            context.UserRoles.AddRange(
                new UserRoles { UserId = admin.Id, RoleId = adminRole.Id },
                new UserRoles { UserId = user.Id, RoleId = userRole.Id }
            );
            context.SaveChanges();

            context.RoleMenus.AddRange(
                new RoleMenus { RoleId = adminRole.Id, MenuId = userManage.Id },
                new RoleMenus { RoleId = adminRole.Id, MenuId = roleManage.Id },
                new RoleMenus { RoleId = adminRole.Id, MenuId = menuManage.Id },
                new RoleMenus { RoleId = adminRole.Id, MenuId = productManage.Id }
            );
            context.SaveChanges();
        }

        
    }

}
