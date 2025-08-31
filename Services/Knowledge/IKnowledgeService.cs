namespace finaid.Services.Knowledge;

/// <summary>
/// Interface for financial aid knowledge base and terminology services
/// </summary>
public interface IKnowledgeService
{
    /// <summary>
    /// Get definition and explanation for a financial aid term
    /// </summary>
    /// <param name="term">Term to define</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Term definition or null if not found</returns>
    Task<FinancialAidTerm?> GetTermDefinitionAsync(string term, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for terms matching a query
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="maxResults">Maximum results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching terms</returns>
    Task<List<FinancialAidTerm>> SearchTermsAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get contextual help based on user context and current situation
    /// </summary>
    /// <param name="context">Current context (form section, user type, etc.)</param>
    /// <param name="userProfile">User profile for personalized help</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Contextual help content or null if not available</returns>
    Task<ContextualHelp?> GetContextualHelpAsync(string context, string userProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get plain language explanation for complex terms
    /// </summary>
    /// <param name="term">Term to explain</param>
    /// <param name="audienceLevel">Target audience (beginner, intermediate, advanced)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plain language explanation</returns>
    Task<string> GetPlainLanguageExplanationAsync(string term, string audienceLevel = "beginner", CancellationToken cancellationToken = default);

    /// <summary>
    /// Get related terms and concepts
    /// </summary>
    /// <param name="term">Base term</param>
    /// <param name="maxResults">Maximum related terms to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of related terms</returns>
    Task<List<string>> GetRelatedTermsAsync(string term, int maxResults = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all terms in a specific category
    /// </summary>
    /// <param name="category">Category to retrieve (grants, loans, eligibility, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of terms in the category</returns>
    Task<List<FinancialAidTerm>> GetTermsByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get frequently asked questions for a topic
    /// </summary>
    /// <param name="topic">Topic or context</param>
    /// <param name="maxResults">Maximum FAQs to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of relevant FAQs</returns>
    Task<List<FrequentlyAskedQuestion>> GetFrequentlyAskedQuestionsAsync(string topic, int maxResults = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add or update a term in the knowledge base
    /// </summary>
    /// <param name="term">Term to add or update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success indicator</returns>
    Task<bool> AddOrUpdateTermAsync(FinancialAidTerm term, CancellationToken cancellationToken = default);
}

/// <summary>
/// Financial aid term with definition and metadata
/// </summary>
public class FinancialAidTerm
{
    public string Term { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string PlainLanguageDefinition { get; set; } = string.Empty;
    public List<string> RelatedTerms { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public List<string> Examples { get; set; } = new();
    public string SourceUrl { get; set; } = string.Empty;
    public List<string> Synonyms { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string? ImageUrl { get; set; }
    public bool IsOfficialTerm { get; set; } = true;
    public double PopularityScore { get; set; } = 0.0;
}

/// <summary>
/// Contextual help content for specific situations
/// </summary>
public class ContextualHelp
{
    public string Context { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
    public List<string> ActionableSteps { get; set; } = new();
    public List<string> RelatedResources { get; set; } = new();
    public string? VideoUrl { get; set; }
    public List<string> CommonMistakes { get; set; } = new();
    public string TargetAudience { get; set; } = string.Empty;
    public DateTime LastReviewed { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Frequently asked question with answer
/// </summary>
public class FrequentlyAskedQuestion
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> RelatedQuestions { get; set; } = new();
    public string? SourceUrl { get; set; }
    public int ViewCount { get; set; } = 0;
    public bool IsVerified { get; set; } = true;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}