using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCheck.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetDrugIdToSearchHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SearchHistories_Drugs_DrugId",
                table: "SearchHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_SearchHistories_Users_UserId",
                table: "SearchHistories");

            migrationBuilder.AddColumn<int>(
                name: "DiseaseId",
                table: "SearchHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetDrugId",
                table: "SearchHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "SearchHistories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_DiseaseId",
                table: "SearchHistories",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_TargetDrugId",
                table: "SearchHistories",
                column: "TargetDrugId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_UserId1",
                table: "SearchHistories",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchHistories_Diseases_DiseaseId",
                table: "SearchHistories",
                column: "DiseaseId",
                principalTable: "Diseases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SearchHistories_Drugs_DrugId",
                table: "SearchHistories",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SearchHistories_Drugs_TargetDrugId",
                table: "SearchHistories",
                column: "TargetDrugId",
                principalTable: "Drugs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SearchHistories_Users_UserId",
                table: "SearchHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SearchHistories_Users_UserId1",
                table: "SearchHistories",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SearchHistories_Diseases_DiseaseId",
                table: "SearchHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_SearchHistories_Drugs_DrugId",
                table: "SearchHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_SearchHistories_Drugs_TargetDrugId",
                table: "SearchHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_SearchHistories_Users_UserId",
                table: "SearchHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_SearchHistories_Users_UserId1",
                table: "SearchHistories");

            migrationBuilder.DropIndex(
                name: "IX_SearchHistories_DiseaseId",
                table: "SearchHistories");

            migrationBuilder.DropIndex(
                name: "IX_SearchHistories_TargetDrugId",
                table: "SearchHistories");

            migrationBuilder.DropIndex(
                name: "IX_SearchHistories_UserId1",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "DiseaseId",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "TargetDrugId",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "SearchHistories");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchHistories_Drugs_DrugId",
                table: "SearchHistories",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SearchHistories_Users_UserId",
                table: "SearchHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
