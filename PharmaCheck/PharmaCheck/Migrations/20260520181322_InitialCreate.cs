using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCheck.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Diseases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symptoms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Causes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TreatmentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diseases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Drugs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActiveIngredient = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Function = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SideEffects = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Contraindications = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drugs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DrugDiseaseContraindications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrugId = table.Column<int>(type: "int", nullable: false),
                    DiseaseId = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    Warning = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Risk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugDiseaseContraindications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrugDiseaseContraindications_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrugDiseaseContraindications_Drugs_DrugId",
                        column: x => x.DrugId,
                        principalTable: "Drugs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrugInteractions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceDrugId = table.Column<int>(type: "int", nullable: false),
                    TargetDrugId = table.Column<int>(type: "int", nullable: false),
                    SeverityLevel = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrugInteractions_Drugs_SourceDrugId",
                        column: x => x.SourceDrugId,
                        principalTable: "Drugs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugInteractions_Drugs_TargetDrugId",
                        column: x => x.TargetDrugId,
                        principalTable: "Drugs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SearchHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    DrugId = table.Column<int>(type: "int", nullable: true),
                    SearchType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SearchQuery = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchHistories_Drugs_DrugId",
                        column: x => x.DrugId,
                        principalTable: "Drugs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SearchHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseaseContraindications_DiseaseId",
                table: "DrugDiseaseContraindications",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugDiseaseContraindications_DrugId",
                table: "DrugDiseaseContraindications",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugInteractions_SourceDrugId",
                table: "DrugInteractions",
                column: "SourceDrugId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugInteractions_TargetDrugId",
                table: "DrugInteractions",
                column: "TargetDrugId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_DrugId",
                table: "SearchHistories",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_UserId",
                table: "SearchHistories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugDiseaseContraindications");

            migrationBuilder.DropTable(
                name: "DrugInteractions");

            migrationBuilder.DropTable(
                name: "SearchHistories");

            migrationBuilder.DropTable(
                name: "Diseases");

            migrationBuilder.DropTable(
                name: "Drugs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
