using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCheck.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtToDrugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DrugDiseaseContraindications",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DrugDiseaseContraindications");
        }
    }
}
