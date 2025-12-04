using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class EditAdminPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "019a1c20-390c-704d-92e1-7dcf93597854",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGt07e0ZLz2MrgscxS30ch7v/oMIEYa1cA9oGdi5913BKmxtKFhVVBwT7T3vWCJC7g==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "019a1c20-390c-704d-92e1-7dcf93597854",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEFA4YWPPxGVMatlhTmfO3c1bCnlplmdko15WPEqSPxfeKLPxLFzDTa7n+y73KPyLgQ==");
        }
    }
}
