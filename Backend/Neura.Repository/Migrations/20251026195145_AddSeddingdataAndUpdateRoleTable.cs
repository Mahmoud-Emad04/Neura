using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSeddingdataAndUpdateRoleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefualt",
                table: "AspNetRoles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AspNetRoles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "IsDefualt", "IsDeleted", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "019a1c20-390e-7fd8-9b20-cddc38906b5b", "019a1c20-390e-7fd8-9b20-cdddf127ba16", false, false, "Admin", "ADMIN" },
                    { "019a1c20-390e-7fd8-9b20-cde0cc78e33e", "019a1c20-390e-7fd8-9b20-cddf89d2a037", true, false, "Member", "MEMBER" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "DiscordHandle", "Email", "EmailConfirmed", "FirstName", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "019a1c20-390c-704d-92e1-7dcf93597854", 0, "019a1c20-390e-7fd8-9b20-cdde028f1737", "", "Admin@Neura.org", true, "System", "Admin", false, null, "ADMIN@NEURA.ORG", "ADMIN", "AQAAAAIAAYagAAAAEGt07e0ZLz2MrgscxS30ch7v/oMIEYa1cA9oGdi5913BKmxtKFhVVBwT7T3vWCJC7g==", null, false, "019a1c20390e7fd89b20cddb68eed9f5", false, "Admin" });

            migrationBuilder.InsertData(
                table: "AspNetRoleClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "RoleId" },
                values: new object[,]
                {
                    { 1, "permissions", "courses:read", "019a1c20-390e-7fd8-9b20-cddc38906b5b" },
                    { 2, "permissions", "courses:add", "019a1c20-390e-7fd8-9b20-cddc38906b5b" },
                    { 3, "permissions", "courses:update", "019a1c20-390e-7fd8-9b20-cddc38906b5b" },
                    { 4, "permissions", "courses:delete", "019a1c20-390e-7fd8-9b20-cddc38906b5b" },
                    { 5, "permissions", "users:read", "019a1c20-390e-7fd8-9b20-cddc38906b5b" },
                    { 6, "permissions", "users:add", "019a1c20-390e-7fd8-9b20-cddc38906b5b" },
                    { 7, "permissions", "users:update", "019a1c20-390e-7fd8-9b20-cddc38906b5b" },
                    { 8, "permissions", "roles:read", "019a1c20-390e-7fd8-9b20-cddc38906b5b" },
                    { 9, "permissions", "roles:add", "019a1c20-390e-7fd8-9b20-cddc38906b5b" },
                    { 10, "permissions", "roles:update", "019a1c20-390e-7fd8-9b20-cddc38906b5b" },
                    { 11, "permissions", "results:read", "019a1c20-390e-7fd8-9b20-cddc38906b5b" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "019a1c20-390e-7fd8-9b20-cddc38906b5b", "019a1c20-390c-704d-92e1-7dcf93597854" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "019a1c20-390e-7fd8-9b20-cde0cc78e33e");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "019a1c20-390e-7fd8-9b20-cddc38906b5b", "019a1c20-390c-704d-92e1-7dcf93597854" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "019a1c20-390e-7fd8-9b20-cddc38906b5b");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "019a1c20-390c-704d-92e1-7dcf93597854");

            migrationBuilder.DropColumn(
                name: "IsDefualt",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AspNetRoles");
        }
    }
}
