using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVwUsersView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE VIEW [dbo].[VwUsers] AS
SELECT u.[Id], u.[Name], u.[Email], u.[ImageUser], u.[ActiveUser], r.[Name] AS [Role]
FROM [dbo].[AspNetUsers] AS u
LEFT JOIN [dbo].[AspNetUserRoles] AS ur ON ur.[UserId] = u.[Id]
LEFT JOIN [dbo].[AspNetRoles] AS r ON r.[Id] = ur.[RoleId];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW [dbo].[VwUsers];");
        }
    }
}
