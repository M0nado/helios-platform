using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HELIOS.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfilesSettingsAndBackupTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackupJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SourcePath = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    DestinationPath = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SizeMb = table.Column<long>(type: "INTEGER", nullable: false),
                    ProgressPercent = table.Column<double>(type: "REAL", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    IsIncremental = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    TaskType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CronExpression = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastExecuted = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextScheduledRun = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TaskData = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Bio = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    AvatarUrl = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Language = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    EnableNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableEmailNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SettingKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    SettingValue = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackupJobs_StartTime",
                table: "BackupJobs",
                column: "StartTime",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTasks_CreatedAt",
                table: "ScheduledTasks",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackupJobs");

            migrationBuilder.DropTable(
                name: "ScheduledTasks");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "UserSettings");
        }
    }
}
