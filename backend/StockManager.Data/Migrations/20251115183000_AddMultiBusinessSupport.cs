using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiBusinessSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create UserBusinesses junction table
            migrationBuilder.CreateTable(
                name: "UserBusinesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBusinesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBusinesses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBusinesses_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBusinesses_BusinessId",
                table: "UserBusinesses",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBusinesses_UserId_BusinessId",
                table: "UserBusinesses",
                columns: new[] { "UserId", "BusinessId" },
                unique: true);

            // Migrate existing user-business relationships to UserBusinesses table
            migrationBuilder.Sql(@"
                INSERT INTO UserBusinesses (UserId, BusinessId, Role, IsActive, CreatedAt, UpdatedAt)
                SELECT Id, BusinessId,
                    CASE Role
                        WHEN 0 THEN 'Admin'
                        WHEN 1 THEN 'Manager'
                        WHEN 2 THEN 'Staff'
                        WHEN 3 THEN 'Viewer'
                        ELSE 'Viewer'
                    END,
                    1,
                    GETUTCDATE(),
                    GETUTCDATE()
                FROM AspNetUsers
                WHERE BusinessId IS NOT NULL
            ");

            // Add CurrentBusinessId column
            migrationBuilder.AddColumn<int>(
                name: "CurrentBusinessId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            // Copy BusinessId to CurrentBusinessId
            migrationBuilder.Sql(@"
                UPDATE AspNetUsers
                SET CurrentBusinessId = BusinessId
                WHERE BusinessId IS NOT NULL
            ");

            // Create foreign key for CurrentBusinessId
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CurrentBusinessId",
                table: "AspNetUsers",
                column: "CurrentBusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Businesses_CurrentBusinessId",
                table: "AspNetUsers",
                column: "CurrentBusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Drop old BusinessId foreign key and column
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Businesses_BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "AspNetUsers");

            // Drop old Role column
            migrationBuilder.DropColumn(
                name: "Role",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back BusinessId column
            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Add back Role column
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Viewer");

            // Copy CurrentBusinessId back to BusinessId
            migrationBuilder.Sql(@"
                UPDATE AspNetUsers
                SET BusinessId = CurrentBusinessId
                WHERE CurrentBusinessId IS NOT NULL
            ");

            // Copy role from UserBusinesses back to AspNetUsers
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.Role = ub.Role
                FROM AspNetUsers u
                INNER JOIN UserBusinesses ub ON u.Id = ub.UserId AND u.CurrentBusinessId = ub.BusinessId
                WHERE ub.IsActive = 1
            ");

            // Drop CurrentBusinessId foreign key and column
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Businesses_CurrentBusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CurrentBusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CurrentBusinessId",
                table: "AspNetUsers");

            // Recreate BusinessId foreign key
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BusinessId",
                table: "AspNetUsers",
                column: "BusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Businesses_BusinessId",
                table: "AspNetUsers",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Drop UserBusinesses table
            migrationBuilder.DropTable(
                name: "UserBusinesses");
        }
    }
}
