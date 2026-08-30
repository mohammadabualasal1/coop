using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace coop.Migrations
{
    /// <inheritdoc />
    public partial class AddPostGisLocationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "MerchantBranches",
                type: "geography (point)",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "CurrentLocation",
                table: "DriverProfiles",
                type: "geography (point)",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "CustomerAddresses",
                type: "geography (point)",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""MerchantBranches""
                SET ""Location"" = ST_SetSRID(ST_MakePoint(""Longitude"", ""Latitude""), 4326)::geography;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""CustomerAddresses""
                SET ""Location"" = ST_SetSRID(ST_MakePoint(""Longitude"", ""Latitude""), 4326)::geography;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""DriverProfiles""
                SET ""CurrentLocation"" = ST_SetSRID(ST_MakePoint(""CurrentLongitude"", ""CurrentLatitude""), 4326)::geography
                WHERE ""CurrentLatitude"" IS NOT NULL AND ""CurrentLongitude"" IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_MerchantBranches_Location""
                ON ""MerchantBranches"" USING GIST (""Location"");
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_CustomerAddresses_Location""
                ON ""CustomerAddresses"" USING GIST (""Location"");
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_DriverProfiles_CurrentLocation""
                ON ""DriverProfiles"" USING GIST (""CurrentLocation"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_MerchantBranches_Location"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_CustomerAddresses_Location"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_DriverProfiles_CurrentLocation"";");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "MerchantBranches");

            migrationBuilder.DropColumn(
                name: "CurrentLocation",
                table: "DriverProfiles");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "CustomerAddresses");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}