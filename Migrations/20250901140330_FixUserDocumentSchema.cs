using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finaid.Migrations
{
    /// <inheritdoc />
    public partial class FixUserDocumentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationDocuments_UserDocuments_DocumentId",
                table: "ApplicationDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDocuments_Users_UserId",
                table: "UserDocuments");

            migrationBuilder.DropIndex(
                name: "IX_UserDocuments_UserId",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "UserDocuments");

            migrationBuilder.RenameColumn(
                name: "FileSize",
                table: "UserDocuments",
                newName: "RetryCount");

            migrationBuilder.RenameColumn(
                name: "ExpirationDate",
                table: "UserDocuments",
                newName: "ProcessingStartedAt");

            migrationBuilder.AddColumn<string>(
                name: "BlobPath",
                table: "UserDocuments",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ClassificationConfidence",
                table: "UserDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "UserDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "UserDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsEncrypted",
                table: "UserDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "UserDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OCRConfidence",
                table: "UserDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OCRJobId",
                table: "UserDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OCRResultId",
                table: "UserDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingCompletedAt",
                table: "UserDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingError",
                table: "UserDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "UserDocuments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "VirusScanResult",
                table: "UserDocuments",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    VerificationType = table.Column<string>(type: "TEXT", nullable: false),
                    OverallScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    VerifierUserId = table.Column<string>(type: "TEXT", nullable: true),
                    VerifierNotes = table.Column<string>(type: "TEXT", nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", nullable: true),
                    RequiredCorrections = table.Column<string>(type: "TEXT", nullable: false),
                    Issues = table.Column<string>(type: "TEXT", nullable: false),
                    VerificationChecks = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVerifications_UserDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "UserDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OCRProcessingResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OverallConfidence = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: false),
                    RawText = table.Column<string>(type: "TEXT", nullable: true),
                    ExtractedFields = table.Column<string>(type: "TEXT", nullable: true),
                    ValidationErrors = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "TEXT", nullable: false),
                    ClassifiedType = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessingJobId = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessingTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    ModelVersion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCRProcessingResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OCRProcessingResults_UserDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "UserDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDocument",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ExtractedText = table.Column<string>(type: "TEXT", nullable: true),
                    ExtractedDataJson = table.Column<string>(type: "TEXT", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VerifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ProcessingNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDocument_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVerifications_DocumentId",
                table: "DocumentVerifications",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVerifications_Status",
                table: "DocumentVerifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVerifications_UserId_Status",
                table: "DocumentVerifications",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OCRProcessingResults_DocumentId",
                table: "OCRProcessingResults",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OCRProcessingResults_ProcessingJobId",
                table: "OCRProcessingResults",
                column: "ProcessingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocument_UserId",
                table: "UserDocument",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDocuments_UserDocument_DocumentId",
                table: "ApplicationDocuments",
                column: "DocumentId",
                principalTable: "UserDocument",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationDocuments_UserDocument_DocumentId",
                table: "ApplicationDocuments");

            migrationBuilder.DropTable(
                name: "DocumentVerifications");

            migrationBuilder.DropTable(
                name: "OCRProcessingResults");

            migrationBuilder.DropTable(
                name: "UserDocument");

            migrationBuilder.DropColumn(
                name: "BlobPath",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "ClassificationConfidence",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "IsEncrypted",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "OCRConfidence",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "OCRJobId",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "OCRResultId",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "ProcessingCompletedAt",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "ProcessingError",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "UserDocuments");

            migrationBuilder.DropColumn(
                name: "VirusScanResult",
                table: "UserDocuments");

            migrationBuilder.RenameColumn(
                name: "RetryCount",
                table: "UserDocuments",
                newName: "FileSize");

            migrationBuilder.RenameColumn(
                name: "ProcessingStartedAt",
                table: "UserDocuments",
                newName: "ExpirationDate");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "UserDocuments",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocuments_UserId",
                table: "UserDocuments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDocuments_UserDocuments_DocumentId",
                table: "ApplicationDocuments",
                column: "DocumentId",
                principalTable: "UserDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserDocuments_Users_UserId",
                table: "UserDocuments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
