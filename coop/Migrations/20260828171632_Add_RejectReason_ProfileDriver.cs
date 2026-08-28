using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace coop.Migrations
{
    /// <inheritdoc />
    public partial class Add_RejectReason_ProfileDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "DriverProfiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "DriverProfiles");
        }
    }
}
