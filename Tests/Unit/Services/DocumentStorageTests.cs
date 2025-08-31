using Azure.Storage.Blobs;
using Azure.Storage.Blobs;
using finaid.Configuration;
using finaid.Data;
using finaid.Models.Documents;
using finaid.Models.Document;
using finaid.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;

namespace finaid.Tests.Unit.Services;

public class DocumentStorageTests
{
    private readonly Mock<BlobServiceClient> _blobServiceClientMock;
    private readonly Mock<IOptions<DocumentStorageSettings>> _settingsMock;
    private readonly Mock<ILogger<DocumentStorageService>> _loggerMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly DocumentStorageService _service;
    private readonly DocumentStorageSettings _settings;

    public DocumentStorageTests()
    {
        _blobServiceClientMock = new Mock<BlobServiceClient>();
        _settingsMock = new Mock<IOptions<DocumentStorageSettings>>();
        _loggerMock = new Mock<ILogger<DocumentStorageService>>();
        
        _settings = new DocumentStorageSettings
        {
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "test-documents",
            MaxFileSizeBytes = 50 * 1024 * 1024,
            AllowedFileTypes = new[] { ".pdf", ".jpg", ".png" },
            DocumentRetentionDays = 2555
        };
        
        _settingsMock.Setup(x => x.Value).Returns(_settings);

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        _service = new DocumentStorageService(
            _blobServiceClientMock.Object,
            _settingsMock.Object,
            _loggerMock.Object,
            _dbContext);
    }

    [Fact]
    public async Task ValidateDocumentAsync_ValidPdfFile_ReturnsTrue()
    {
        // Arrange
        var pdfHeader = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        var stream = new MemoryStream(pdfHeader);
        
        // Act
        var result = await _service.ValidateDocumentAsync(stream, "test.pdf", 1024);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateDocumentAsync_FileTooLarge_ReturnsFalse()
    {
        // Arrange
        var stream = new MemoryStream(new byte[1024]);
        var fileSizeBytes = _settings.MaxFileSizeBytes + 1;
        
        // Act
        var result = await _service.ValidateDocumentAsync(stream, "test.pdf", fileSizeBytes);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("exceeds maximum allowed size", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateDocumentAsync_InvalidFileType_ReturnsFalse()
    {
        // Arrange
        var stream = new MemoryStream(new byte[1024]);
        
        // Act
        var result = await _service.ValidateDocumentAsync(stream, "test.exe", 1024);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("is not allowed", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateDocumentAsync_ValidJpgFile_ReturnsTrue()
    {
        // Arrange
        var jpgHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPG header with 4 bytes
        var stream = new MemoryStream(jpgHeader);
        
        // Act
        var result = await _service.ValidateDocumentAsync(stream, "test.jpg", 1024);
        
        // Assert
        Assert.True(result.IsValid, result.ErrorMessage ?? "No error message");
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateDocumentAsync_ValidPngFile_ReturnsTrue()
    {
        // Arrange
        var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var stream = new MemoryStream(pngHeader);
        
        // Act
        var result = await _service.ValidateDocumentAsync(stream, "test.png", 1024);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task GetDocumentMetadataAsync_ExistingDocument_ReturnsMetadata()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var userDocument = new finaid.Data.Entities.UserDocument
        {
            Id = documentId,
            UserId = userId,
            DocumentType = DocumentType.TaxReturn,
            FileName = "test.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            BlobPath = "test/path",
            IsEncrypted = true,
            UploadedAt = DateTime.UtcNow
        };
        
        _dbContext.UserDocuments.Add(userDocument);
        await _dbContext.SaveChangesAsync();
        
        // Act
        var result = await _service.GetDocumentMetadataAsync(documentId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("test.pdf", result.FileName);
        Assert.Equal(DocumentType.TaxReturn, result.Type);
    }

    [Fact]
    public async Task GetDocumentMetadataAsync_NonExistentDocument_ReturnsNull()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        
        // Act
        var result = await _service.GetDocumentMetadataAsync(documentId);
        
        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}