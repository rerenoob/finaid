using finaid.Models.Document;
using finaid.Models.Documents;
using finaid.Models.OCR;
using finaid.Services.OCR;
using finaid.Services.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace finaid.Tests.Unit.Services;

public class DocumentClassificationServiceTests
{
    private readonly Mock<IOCRService> _mockOcrService;
    private readonly Mock<IDocumentStorageService> _mockDocumentStorageService;
    private readonly Mock<ILogger<DocumentClassificationService>> _mockLogger;
    private readonly DocumentClassificationService _service;

    public DocumentClassificationServiceTests()
    {
        _mockOcrService = new Mock<IOCRService>();
        _mockDocumentStorageService = new Mock<IDocumentStorageService>();
        _mockLogger = new Mock<ILogger<DocumentClassificationService>>();
        
        _service = new DocumentClassificationService(
            _mockOcrService.Object,
            _mockDocumentStorageService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ClassifyDocumentAsync_WithTaxReturnContent_ReturnsTaxReturnClassification()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        
        var documentMetadata = new DocumentMetadata
        {
            Id = documentId,
            FileName = "tax_return.pdf",
            ContentType = "application/pdf"
        };

        var ocrResult = new OCRResult
        {
            DocumentId = documentId,
            RawText = "Form 1040 U.S. Individual Income Tax Return adjusted gross income taxable income",
            OverallConfidence = 0.9m
        };

        _mockDocumentStorageService
            .Setup(x => x.GetDocumentMetadataAsync(documentId, cancellationToken))
            .ReturnsAsync(documentMetadata);

        _mockOcrService
            .Setup(x => x.ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken))
            .ReturnsAsync(ocrResult);

        // Act
        var result = await _service.ClassifyDocumentAsync(documentId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(DocumentType.TaxReturn, result.ClassifiedType);
        Assert.True(result.Confidence > 0);
        Assert.Contains(DocumentType.TaxReturn, result.AllScores.Keys);
    }

    [Fact]
    public async Task ClassifyDocumentAsync_WithW2Content_ReturnsW2FormClassification()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        
        var documentMetadata = new DocumentMetadata
        {
            Id = documentId,
            FileName = "w2_form.pdf",
            ContentType = "application/pdf"
        };

        var ocrResult = new OCRResult
        {
            DocumentId = documentId,
            RawText = "Form W-2 Wage and Tax Statement wages tips federal income tax withheld",
            OverallConfidence = 0.95m
        };

        _mockDocumentStorageService
            .Setup(x => x.GetDocumentMetadataAsync(documentId, cancellationToken))
            .ReturnsAsync(documentMetadata);

        _mockOcrService
            .Setup(x => x.ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken))
            .ReturnsAsync(ocrResult);

        // Act
        var result = await _service.ClassifyDocumentAsync(documentId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(DocumentType.W2Form, result.ClassifiedType);
        Assert.True(result.Confidence > 0);
        Assert.Contains(DocumentType.W2Form, result.AllScores.Keys);
    }

    [Fact]
    public async Task ClassifyDocumentAsync_WithBankStatementContent_ReturnsBankStatementClassification()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        
        var documentMetadata = new DocumentMetadata
        {
            Id = documentId,
            FileName = "bank_statement.pdf",
            ContentType = "application/pdf"
        };

        var ocrResult = new OCRResult
        {
            DocumentId = documentId,
            RawText = "bank statement account statement beginning balance ending balance deposits withdrawals",
            OverallConfidence = 0.85m
        };

        _mockDocumentStorageService
            .Setup(x => x.GetDocumentMetadataAsync(documentId, cancellationToken))
            .ReturnsAsync(documentMetadata);

        _mockOcrService
            .Setup(x => x.ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken))
            .ReturnsAsync(ocrResult);

        // Act
        var result = await _service.ClassifyDocumentAsync(documentId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(DocumentType.BankStatement, result.ClassifiedType);
        Assert.True(result.Confidence > 0);
        Assert.Contains(DocumentType.BankStatement, result.AllScores.Keys);
    }

    [Fact]
    public async Task ClassifyDocumentAsync_WithDriversLicenseContent_ReturnsDriversLicenseClassification()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        
        var documentMetadata = new DocumentMetadata
        {
            Id = documentId,
            FileName = "drivers_license.jpg",
            ContentType = "image/jpeg"
        };

        var ocrResult = new OCRResult
        {
            DocumentId = documentId,
            RawText = "driver license license number expiration date date of birth department of motor vehicles",
            OverallConfidence = 0.88m
        };

        _mockDocumentStorageService
            .Setup(x => x.GetDocumentMetadataAsync(documentId, cancellationToken))
            .ReturnsAsync(documentMetadata);

        _mockOcrService
            .Setup(x => x.ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken))
            .ReturnsAsync(ocrResult);

        // Act
        var result = await _service.ClassifyDocumentAsync(documentId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(DocumentType.DriversLicense, result.ClassifiedType);
        Assert.True(result.Confidence > 0);
        Assert.Contains(DocumentType.DriversLicense, result.AllScores.Keys);
    }

    [Fact]
    public async Task ClassifyDocumentAsync_WithTranscriptContent_ReturnsCollegeTranscriptClassification()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        
        var documentMetadata = new DocumentMetadata
        {
            Id = documentId,
            FileName = "college_transcript.pdf",
            ContentType = "application/pdf"
        };

        var ocrResult = new OCRResult
        {
            DocumentId = documentId,
            RawText = "college university transcript bachelor degree cumulative gpa credit hours semester course",
            OverallConfidence = 0.92m
        };

        _mockDocumentStorageService
            .Setup(x => x.GetDocumentMetadataAsync(documentId, cancellationToken))
            .ReturnsAsync(documentMetadata);

        _mockOcrService
            .Setup(x => x.ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken))
            .ReturnsAsync(ocrResult);

        // Act
        var result = await _service.ClassifyDocumentAsync(documentId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(DocumentType.CollegeTranscript, result.ClassifiedType);
        Assert.True(result.Confidence > 0);
        Assert.Contains(DocumentType.CollegeTranscript, result.AllScores.Keys);
    }

    [Fact]
    public async Task ClassifyDocumentAsync_WithUnknownContent_ReturnsOtherClassification()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        
        var documentMetadata = new DocumentMetadata
        {
            Id = documentId,
            FileName = "unknown_document.pdf",
            ContentType = "application/pdf"
        };

        var ocrResult = new OCRResult
        {
            DocumentId = documentId,
            RawText = "some random text that doesn't match any known document patterns",
            OverallConfidence = 0.7m
        };

        _mockDocumentStorageService
            .Setup(x => x.GetDocumentMetadataAsync(documentId, cancellationToken))
            .ReturnsAsync(documentMetadata);

        _mockOcrService
            .Setup(x => x.ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken))
            .ReturnsAsync(ocrResult);

        // Act
        var result = await _service.ClassifyDocumentAsync(documentId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(DocumentType.Other, result.ClassifiedType);
        Assert.True(result.Confidence >= 0);
    }

    [Fact]
    public async Task ClassifyDocumentAsync_WithMissingMetadata_ReturnsErrorResult()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _mockDocumentStorageService
            .Setup(x => x.GetDocumentMetadataAsync(documentId, cancellationToken))
            .ReturnsAsync((DocumentMetadata?)null);

        // Act
        var result = await _service.ClassifyDocumentAsync(documentId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(DocumentType.Other, result.ClassifiedType);
        Assert.Equal(0, result.Confidence);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task GetSuggestedTypesAsync_ReturnsTopClassifications()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var maxSuggestions = 3;
        var cancellationToken = CancellationToken.None;
        
        var documentMetadata = new DocumentMetadata
        {
            Id = documentId,
            FileName = "document.pdf",
            ContentType = "application/pdf"
        };

        var ocrResult = new OCRResult
        {
            DocumentId = documentId,
            RawText = "form tax return income wages bank statement",
            OverallConfidence = 0.8m
        };

        _mockDocumentStorageService
            .Setup(x => x.GetDocumentMetadataAsync(documentId, cancellationToken))
            .ReturnsAsync(documentMetadata);

        _mockOcrService
            .Setup(x => x.ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken))
            .ReturnsAsync(ocrResult);

        // Act
        var result = await _service.GetSuggestedTypesAsync(documentId, maxSuggestions, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count <= maxSuggestions);
        Assert.True(result.Count > 0);
    }

    [Theory]
    [InlineData("tax_return.pdf", DocumentType.TaxReturn)]
    [InlineData("w2_2023.pdf", DocumentType.W2Form)]
    [InlineData("bank_statement.pdf", DocumentType.BankStatement)]
    [InlineData("drivers_license.jpg", DocumentType.DriversLicense)]
    [InlineData("passport.jpg", DocumentType.Passport)]
    [InlineData("high_school_transcript.pdf", DocumentType.HighSchoolTranscript)]
    [InlineData("college_transcript.pdf", DocumentType.CollegeTranscript)]
    public async Task ClassifyDocumentAsync_WithMatchingFilename_BoostsConfidence(string filename, DocumentType expectedType)
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        
        var documentMetadata = new DocumentMetadata
        {
            Id = documentId,
            FileName = filename,
            ContentType = "application/pdf"
        };

        // Create OCR result with some matching content for the expected type
        var contentMap = new Dictionary<DocumentType, string>
        {
            { DocumentType.TaxReturn, "tax return form 1040" },
            { DocumentType.W2Form, "w2 wage tax statement" },
            { DocumentType.BankStatement, "bank statement balance" },
            { DocumentType.DriversLicense, "driver license" },
            { DocumentType.Passport, "passport" },
            { DocumentType.HighSchoolTranscript, "high school transcript" },
            { DocumentType.CollegeTranscript, "college transcript university" }
        };

        var ocrResult = new OCRResult
        {
            DocumentId = documentId,
            RawText = contentMap.GetValueOrDefault(expectedType, "some content"),
            OverallConfidence = 0.8m
        };

        _mockDocumentStorageService
            .Setup(x => x.GetDocumentMetadataAsync(documentId, cancellationToken))
            .ReturnsAsync(documentMetadata);

        _mockOcrService
            .Setup(x => x.ProcessDocumentAsync(documentId, DocumentType.Other, cancellationToken))
            .ReturnsAsync(ocrResult);

        // Act
        var result = await _service.ClassifyDocumentAsync(documentId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedType, result.ClassifiedType);
        Assert.True(result.Confidence > 0);
    }
}