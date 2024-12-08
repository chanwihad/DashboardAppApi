using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CrudApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "can_menus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Level1 = table.Column<string>(type: "text", nullable: true),
                    Level2 = table.Column<string>(type: "text", nullable: true),
                    Level3 = table.Column<string>(type: "text", nullable: true),
                    Level4 = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_can_menus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "can_products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_can_products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "can_roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CanView = table.Column<bool>(type: "boolean", nullable: false),
                    CanCreate = table.Column<bool>(type: "boolean", nullable: false),
                    CanUpdate = table.Column<bool>(type: "boolean", nullable: false),
                    CanDelete = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_can_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "can_users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MaxRetry = table.Column<int>(type: "integer", nullable: false),
                    Retry = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_can_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "can_rolemenus",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    MenuId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_can_rolemenus", x => new { x.RoleId, x.MenuId });
                    table.ForeignKey(
                        name: "FK_can_rolemenus_can_menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "can_menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_can_rolemenus_can_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "can_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "can_userroles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_can_userroles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_can_userroles_can_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "can_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_can_userroles_can_users_UserId",
                        column: x => x.UserId,
                        principalTable: "can_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_can_rolemenus_MenuId",
                table: "can_rolemenus",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_can_userroles_RoleId",
                table: "can_userroles",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "can_products");

            migrationBuilder.DropTable(
                name: "can_rolemenus");

            migrationBuilder.DropTable(
                name: "can_userroles");

            migrationBuilder.DropTable(
                name: "can_menus");

            migrationBuilder.DropTable(
                name: "can_roles");

            migrationBuilder.DropTable(
                name: "can_users");
        }
    }
}
