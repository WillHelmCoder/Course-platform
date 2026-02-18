using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddContentRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContentReads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentReads_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentReads_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    FormType = table.Column<int>(type: "INTEGER", nullable: false),
                    Trigger = table.Column<int>(type: "INTEGER", nullable: false),
                    TriggerCourseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PassingScore = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Forms_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Forms_Courses_TriggerCourseId",
                        column: x => x.TriggerCourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Forms_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FormId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    QuestionType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormQuestions_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormQuestions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FormId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Passed = table.Column<bool>(type: "INTEGER", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormResponses_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormResponses_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormResponses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormAnswerOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuestionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormAnswerOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormAnswerOptions_FormQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "FormQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormAnswerOptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormResponseAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ResponseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuestionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TextAnswer = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedOptionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SelectedOptionIds = table.Column<string>(type: "TEXT", nullable: true),
                    PointsEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormResponseAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormResponseAnswers_FormAnswerOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "FormAnswerOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormResponseAnswers_FormQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "FormQuestions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormResponseAnswers_FormResponses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "FormResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormResponseAnswers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentReads_ContentId",
                table: "ContentReads",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReads_UserId_ContentId",
                table: "ContentReads",
                columns: new[] { "UserId", "ContentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormAnswerOptions_QuestionId",
                table: "FormAnswerOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAnswerOptions_TenantId",
                table: "FormAnswerOptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormQuestions_FormId",
                table: "FormQuestions",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormQuestions_TenantId",
                table: "FormQuestions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponseAnswers_QuestionId",
                table: "FormResponseAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponseAnswers_ResponseId",
                table: "FormResponseAnswers",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponseAnswers_SelectedOptionId",
                table: "FormResponseAnswers",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponseAnswers_TenantId",
                table: "FormResponseAnswers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_FormId_UserId",
                table: "FormResponses",
                columns: new[] { "FormId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_TenantId",
                table: "FormResponses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_UserId",
                table: "FormResponses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_ChannelId",
                table: "Forms",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_TenantId",
                table: "Forms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_TriggerCourseId",
                table: "Forms",
                column: "TriggerCourseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentReads");

            migrationBuilder.DropTable(
                name: "FormResponseAnswers");

            migrationBuilder.DropTable(
                name: "FormAnswerOptions");

            migrationBuilder.DropTable(
                name: "FormResponses");

            migrationBuilder.DropTable(
                name: "FormQuestions");

            migrationBuilder.DropTable(
                name: "Forms");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");
        }
    }
}
