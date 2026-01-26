using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class updateRoleAndUpdateCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "019aeef9-ea10-7594-a042-ebc8958f1366");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "019aeef9-ea10-7594-a042-ebca472ee63f");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "019aeef9-ea10-7594-a042-ebccf58eb683");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "019aeef9-ea10-7594-a042-ebce4c1dec9b");

            migrationBuilder.DropColumn(
                name: "PermissionMask",
                table: "CourseUsers");

            migrationBuilder.AddColumn<string>(
                name: "InstructorName",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Price",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CourseRoleMasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PermissionsMask = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRoleMasks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseRoleMasks");

            migrationBuilder.DropColumn(
                name: "InstructorName",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Courses");

            migrationBuilder.AddColumn<int>(
                name: "PermissionMask",
                table: "CourseUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "IsDefualt", "IsDeleted", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "019aeef9-ea10-7594-a042-ebc8958f1366", "019aeef9-ea10-7594-a042-ebc9c5b329bd", false, false, "CourseOwner", "COURSEOWNER" },
                    { "019aeef9-ea10-7594-a042-ebca472ee63f", "019aeef9-ea10-7594-a042-ebcbaa20d23d", false, false, "CoInstructor", "COINSTRUCTOR" },
                    { "019aeef9-ea10-7594-a042-ebccf58eb683", "019aeef9-ea10-7594-a042-ebcdfbd1c0ff", false, false, "TeachingAssistant", "TEACHINGASSISTANT" },
                    { "019aeef9-ea10-7594-a042-ebce4c1dec9b", "019aeef9-ea10-7594-a042-ebcf7ddf3644", false, false, "Student", "STUDENT" }
                });

            migrationBuilder.InsertData(
                table: "AspNetRoleClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "RoleId" },
                values: new object[,]
                {
                    { 111, "permissions", "courses:read", "019aeef9-ea10-7594-a042-ebc8958f1366" },
                    { 112, "permissions", "courses:delete", "019aeef9-ea10-7594-a042-ebc8958f1366" },
                    { 113, "permissions", "courses:update", "019aeef9-ea10-7594-a042-ebc8958f1366" },
                    { 114, "permissions", "courses:read", "019aeef9-ea10-7594-a042-ebca472ee63f" },
                    { 115, "permissions", "courses:update", "019aeef9-ea10-7594-a042-ebca472ee63f" },
                    { 116, "permissions", "courses:read", "019aeef9-ea10-7594-a042-ebccf58eb683" },
                    { 117, "permissions", "courses:read", "019aeef9-ea10-7594-a042-ebce4c1dec9b" }
                });
        }
    }
}
