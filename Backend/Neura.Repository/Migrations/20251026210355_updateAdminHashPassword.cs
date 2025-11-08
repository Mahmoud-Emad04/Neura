using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neura.Repository.Migrations
{
    /// <inheritdoc />
    public partial class updateAdminHashPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "019a1c20-390c-704d-92e1-7dcf93597854",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEBDYTWQhmZWLhUtSPh1TISSaEknFYx24VlXXgkZFBH2u5xMwIe/y/EIxlg4wCpZQoQ==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "019a1c20-390c-704d-92e1-7dcf93597854",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGt07e0ZLz2MrgscxS30ch7v/oMIEYa1cA9oGdi5913BKmxtKFhVVBwT7T3vWCJC7g==");
        }
    }
}
