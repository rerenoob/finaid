using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using finaid.Configuration;
using finaid.Models.Document;
using finaid.Models.Documents;
using finaid.Models.OCR;
using finaid.Services.OCR;
using finaid.Services.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace finaid.Tests.Unit.Services;

public class FormRecognizerServiceTests
{
    private readonly Mock<DocumentAnalysisClient> _mockDocumentAnalysisClient;
    private readonly Mock<IDocumentStorageService> _mockDocumentStorageService;
    private readonly Mock<ILogger<FormRecognizerService>> _mockLogger;
    private readonly Mock<IOptions<FormRecognizerSettings>> _mockOptions;
    private readonly FormRecognizerSettings _settings;
    private readonly FormRecognizerService _service;

    public FormRecognizerServiceTests()
    {
        _mockDocumentAnalysisClient = new Mock<DocumentAnalysisClient>();
        _mockDocumentStorageService = new Mock<IDocumentStorageService>();
        _mockLogger = new Mock<ILogger<FormRecognizerService>>();
        _mockOptions = new Mock<IOptions<FormRecognizerSettings>>();
        
        _settings = new FormRecognizerSettings
        {
            Endpoint = "https://test.cognitiveservices.azure.com",
            ApiKey = "test-key",
            ConfidenceThreshold = 0.7f,
            MaxProcessingTimeSeconds = 120,
            MaxRetryAttempts = 3,
            EnableLogging = true
        };
        
        _mockOptions.Setup(x => x.Value).Returns(_settings);
        
        _service = new FormRecognizerService(
            _mockDocumentAnalysisClient.Object,
            _mockDocumentStorageService.Object,
            _mockOptions.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessDocumentAsync_WithValidDocument_ReturnsSuccessfulResult()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var documentType = DocumentType.TaxReturn;
        var documentStream = new MemoryStream();
        var cancellationToken = CancellationToken.None;

        var documentMetadata = new DocumentMetadata
        {
            Id = documentId,
            FileName = "test-tax-return.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024
        };

        _mockDocumentStorageService
            .Setup(x => x.DownloadDocumentAsync(documentId, cancellationToken))
            .ReturnsAsync(documentStream);

        _mockDocumentStorageService
            .Setup(x => x.GetDocumentMetadataAsync(documentId, cancellationToken))
            .ReturnsAsync(documentMetadata);

        // Create mock analyzed document
        var mockAnalyzedDocument = CreateMockAnalyzedDocument();
        var mockOperation = CreateMockAnalyzeDocumentOperation(mockAnalyzedDocument);

        _mockDocumentAnalysisClient
            .Setup(x => x.AnalyzeDocumentAsync(
                It.IsAny<WaitUntil>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<AnalyzeDocumentOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockOperation.Object);

        // Act
        var result = await _service.ProcessDocumentAsync(documentId, documentType, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(documentType, result.ClassifiedType);
        Assert.Equal(ProcessingStatus.Completed, result.Status);
        Assert.True(result.OverallConfidence >= 0);
        Assert.NotEmpty(result.Fields);
    }

    [Fact]
    public async Task ProcessDocumentAsync_WithMissingMetadata_ReturnsFailedResult()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var documentType = DocumentType.TaxReturn;
        var cancellationToken = CancellationToken.None;

        _mockDocumentStorageService
            .Setup(x => x.GetDocumentMetadataAsync(documentId, cancellationToken))
            .ReturnsAsync((DocumentMetadata?)null);

        // Act
        var result = await _service.ProcessDocumentAsync(documentId, documentType, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(ProcessingStatus.Failed, result.Status);
        Assert.NotEmpty(result.ValidationErrors);
    }

    [Fact]
    public async Task ClassifyDocumentAsync_WithTaxDocument_ReturnsTaxReturnType()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var documentStream = new MemoryStream();
        var cancellationToken = CancellationToken.None;

        _mockDocumentStorageService
            .Setup(x => x.DownloadDocumentAsync(documentId, cancellationToken))
            .ReturnsAsync(documentStream);

        var mockAnalyzedDocument = CreateMockAnalyzedDocumentWithContent("Form 1040 Tax Return");
        var mockOperation = CreateMockAnalyzeDocumentOperation(mockAnalyzedDocument);

        _mockDocumentAnalysisClient
            .Setup(x => x.AnalyzeDocumentAsync(
                It.IsAny<WaitUntil>(),
                "prebuilt-document",
                It.IsAny<Stream>(),
                It.IsAny<AnalyzeDocumentOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockOperation.Object);

        // Act
        var result = await _service.ClassifyDocumentAsync(documentId, cancellationToken);

        // Assert
        Assert.Equal(DocumentType.TaxReturn, result);
    }

    [Fact]
    public async Task ClassifyDocumentAsync_WithW2Document_ReturnsW2FormType()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var documentStream = new MemoryStream();
        var cancellationToken = CancellationToken.None;

        _mockDocumentStorageService
            .Setup(x => x.DownloadDocumentAsync(documentId, cancellationToken))
            .ReturnsAsync(documentStream);

        var mockAnalyzedDocument = CreateMockAnalyzedDocumentWithContent("Form W-2 Wage and Tax Statement");
        var mockOperation = CreateMockAnalyzeDocumentOperation(mockAnalyzedDocument);

        _mockDocumentAnalysisClient
            .Setup(x => x.AnalyzeDocumentAsync(
                It.IsAny<WaitUntil>(),
                "prebuilt-document",
                It.IsAny<Stream>(),
                It.IsAny<AnalyzeDocumentOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockOperation.Object);

        // Act
        var result = await _service.ClassifyDocumentAsync(documentId, cancellationToken);

        // Assert
        Assert.Equal(DocumentType.W2Form, result);
    }

    [Fact]
    public async Task ClassifyDocumentAsync_WithUnknownDocument_ReturnsOtherType()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var documentStream = new MemoryStream();
        var cancellationToken = CancellationToken.None;

        _mockDocumentStorageService
            .Setup(x => x.DownloadDocumentAsync(documentId, cancellationToken))
            .ReturnsAsync(documentStream);

        var mockAnalyzedDocument = CreateMockAnalyzedDocumentWithContent("Some unknown document content");
        var mockOperation = CreateMockAnalyzeDocumentOperation(mockAnalyzedDocument);

        _mockDocumentAnalysisClient
            .Setup(x => x.AnalyzeDocumentAsync(
                It.IsAny<WaitUntil>(),
                "prebuilt-document",
                It.IsAny<Stream>(),
                It.IsAny<AnalyzeDocumentOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockOperation.Object);

        // Act
        var result = await _service.ClassifyDocumentAsync(documentId, cancellationToken);

        // Assert
        Assert.Equal(DocumentType.Other, result);
    }

    [Fact]
    public async Task ValidateExtractedDataAsync_WithValidData_ReturnsValidResult()
    {
        // Arrange
        var ocrResult = new OCRResult
        {
            DocumentId = Guid.NewGuid(),
            Fields = new List<ExtractedField>
            {
                new() { FieldName = "Amount", Value = "1000.50", DataType = DataTypes.Currency },
                new() { FieldName = "Date", Value = "2023-12-01", DataType = DataTypes.Date },
                new() { FieldName = "Count", Value = "5", DataType = DataTypes.Number }
            }
        };

        // Act
        var result = await _service.ValidateExtractedDataAsync(ocrResult, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateExtractedDataAsync_WithInvalidData_ReturnsInvalidResult()
    {
        // Arrange
        var ocrResult = new OCRResult
        {
            DocumentId = Guid.NewGuid(),
            Fields = new List<ExtractedField>
            {
                new() { FieldName = "Amount", Value = "invalid-currency", DataType = DataTypes.Currency },
                new() { FieldName = "Date", Value = "invalid-date", DataType = DataTypes.Date }
            }
        };

        // Act
        var result = await _service.ValidateExtractedDataAsync(ocrResult, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    private Mock<AnalyzedDocument> CreateMockAnalyzedDocument()
    {
        var mockDocument = new Mock<AnalyzedDocument>();
        mockDocument.SetupGet(x => x.Content).Returns("Sample tax document content");
        mockDocument.SetupGet(x => x.Confidence).Returns(0.95f);
        
        // Mock fields
        var mockFields = new Dictionary<string, DocumentField>();
        var mockField = new Mock<DocumentField>();
        mockField.SetupGet(x => x.Content).Returns("John Doe");
        mockField.SetupGet(x => x.Confidence).Returns(0.9f);
        mockField.SetupGet(x => x.FieldType).Returns(DocumentFieldType.String);
        
        mockFields["Name"] = mockField.Object;
        mockDocument.SetupGet(x => x.Fields).Returns(mockFields);
        
        return mockDocument;
    }

    private Mock<AnalyzedDocument> CreateMockAnalyzedDocumentWithContent(string content)
    {
        var mockDocument = new Mock<AnalyzedDocument>();
        mockDocument.SetupGet(x => x.Content).Returns(content);
        mockDocument.SetupGet(x => x.Confidence).Returns(0.95f);
        mockDocument.SetupGet(x => x.Documents).Returns(new List<AnalyzedDocument>());
        mockDocument.SetupGet(x => x.KeyValuePairs).Returns(new List<DocumentKeyValuePair>());
        
        return mockDocument;
    }

    private Mock<AnalyzeResult> CreateMockAnalyzeResult(Mock<AnalyzedDocument> mockDocument)
    {
        var mockResult = new Mock<AnalyzeResult>();
        mockResult.SetupGet(x => x.Content).Returns(mockDocument.Object.Content);
        mockResult.SetupGet(x => x.Documents).Returns(new List<AnalyzedDocument> { mockDocument.Object });
        mockResult.SetupGet(x => x.KeyValuePairs).Returns(new List<DocumentKeyValuePair>());
        
        return mockResult;
    }

    private Mock<Operation<AnalyzeResult>> CreateMockAnalyzeDocumentOperation(Mock<AnalyzedDocument> mockDocument)
    {
        var mockResult = CreateMockAnalyzeResult(mockDocument);
        var mockOperation = new Mock<Operation<AnalyzeResult>>();
        var mockResponse = new Mock<Response<AnalyzeResult>>();
        
        mockResponse.SetupGet(x => x.Value).Returns(mockResult.Object);
        mockOperation.Setup(x => x.WaitForCompletionAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockResponse.Object);
        
        return mockOperation;
    }
}