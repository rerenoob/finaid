using finaid.Services.AI;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace finaid.Services.Knowledge;

/// <summary>
/// Implementation of knowledge service for financial aid terms and help
/// </summary>
public class FinancialAidKnowledgeService : IKnowledgeService
{
    private readonly IAIAssistantService _aiService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FinancialAidKnowledgeService> _logger;
    private readonly Dictionary<string, FinancialAidTerm> _termsDatabase;
    private readonly Dictionary<string, List<FrequentlyAskedQuestion>> _faqDatabase;
    
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(4);

    public FinancialAidKnowledgeService(
        IAIAssistantService aiService,
        IMemoryCache cache,
        ILogger<FinancialAidKnowledgeService> logger)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _termsDatabase = InitializeTermsDatabase();
        _faqDatabase = InitializeFaqDatabase();
    }

    public async Task<FinancialAidTerm?> GetTermDefinitionAsync(string term, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
            return null;

        var cacheKey = $"term:{term.ToLowerInvariant()}";
        
        if (_cache.TryGetValue(cacheKey, out FinancialAidTerm? cachedTerm))
        {
            return cachedTerm;
        }

        // First check our predefined database
        var normalizedTerm = term.ToLowerInvariant().Trim();
        if (_termsDatabase.ContainsKey(normalizedTerm))
        {
            var dbTerm = _termsDatabase[normalizedTerm];
            _cache.Set(cacheKey, dbTerm, _cacheExpiration);
            return dbTerm;
        }

        // If not found, use AI to generate definition
        try
        {
            var explanation = await _aiService.ExplainFinancialAidTermAsync(term, cancellationToken);
            if (!string.IsNullOrEmpty(explanation))
            {
                var aiGeneratedTerm = new FinancialAidTerm
                {
                    Term = term,
                    Definition = explanation,
                    PlainLanguageDefinition = explanation,
                    Category = "general",
                    IsOfficialTerm = false,
                    LastUpdated = DateTime.UtcNow
                };
                
                _cache.Set(cacheKey, aiGeneratedTerm, _cacheExpiration);
                return aiGeneratedTerm;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get AI explanation for term: {Term}", term);
        }

        return null;
    }

    public Task<List<FinancialAidTerm>> SearchTermsAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult(new List<FinancialAidTerm>());

        var cacheKey = $"search:{query.ToLowerInvariant()}:{maxResults}";
        
        if (_cache.TryGetValue(cacheKey, out List<FinancialAidTerm>? cachedResults))
        {
            return Task.FromResult(cachedResults ?? new List<FinancialAidTerm>());
        }

        var normalizedQuery = query.ToLowerInvariant();
        var results = _termsDatabase.Values
            .Where(term => 
                term.Term.ToLowerInvariant().Contains(normalizedQuery) ||
                term.Definition.ToLowerInvariant().Contains(normalizedQuery) ||
                term.Synonyms.Any(s => s.ToLowerInvariant().Contains(normalizedQuery)) ||
                term.Tags.Any(t => t.ToLowerInvariant().Contains(normalizedQuery)))
            .OrderByDescending(term => CalculateRelevanceScore(term, normalizedQuery))
            .Take(maxResults)
            .ToList();

        _cache.Set(cacheKey, results, _cacheExpiration);
        return Task.FromResult(results);
    }

    public async Task<ContextualHelp?> GetContextualHelpAsync(string context, string userProfile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context))
            return null;

        var cacheKey = $"contexthelp:{context.ToLowerInvariant()}:{userProfile.ToLowerInvariant()}";
        
        if (_cache.TryGetValue(cacheKey, out ContextualHelp? cachedHelp))
        {
            return cachedHelp;
        }

        try
        {
            // Use AI to generate contextual help
            var prompt = $"Provide contextual help for a {userProfile} user working on: {context}. " +
                        "Include key points, actionable steps, and common mistakes to avoid.";
            
            var response = await _aiService.GetChatCompletionAsync(new List<finaid.Models.AI.ChatMessage>
            {
                finaid.Models.AI.ChatMessageFactory.CreateSystemMessage("You are a helpful financial aid assistant providing contextual help."),
                finaid.Models.AI.ChatMessageFactory.CreateUserMessage(prompt)
            }, cancellationToken);

            if (response.IsSuccess)
            {
                var help = new ContextualHelp
                {
                    Context = context,
                    Title = $"Help with {context}",
                    Content = response.Content,
                    TargetAudience = userProfile,
                    LastReviewed = DateTime.UtcNow
                };

                // Parse structured content if possible
                ParseStructuredHelp(help, response.Content);
                
                _cache.Set(cacheKey, help, _cacheExpiration);
                return help;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get contextual help for context: {Context}", context);
        }

        return null;
    }

    public async Task<string> GetPlainLanguageExplanationAsync(string term, string audienceLevel = "beginner", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
            return "No term provided.";

        var cacheKey = $"plainlang:{term.ToLowerInvariant()}:{audienceLevel.ToLowerInvariant()}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedExplanation))
        {
            return cachedExplanation ?? "No explanation available.";
        }

        try
        {
            var prompt = $"Explain the financial aid term '{term}' in simple, plain language for a {audienceLevel} level audience. " +
                        "Use everyday words and provide a practical example if helpful.";
            
            var explanation = await _aiService.ExplainFinancialAidTermAsync(prompt, cancellationToken);
            
            _cache.Set(cacheKey, explanation, _cacheExpiration);
            return explanation;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get plain language explanation for term: {Term}", term);
            return $"I couldn't explain '{term}' right now. Please try again later.";
        }
    }

    public Task<List<string>> GetRelatedTermsAsync(string term, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Task.FromResult(new List<string>());

        var cacheKey = $"related:{term.ToLowerInvariant()}:{maxResults}";
        
        if (_cache.TryGetValue(cacheKey, out List<string>? cachedRelated))
        {
            return Task.FromResult(cachedRelated ?? new List<string>());
        }

        var normalizedTerm = term.ToLowerInvariant();
        
        // Check if we have the term in our database
        if (_termsDatabase.ContainsKey(normalizedTerm))
        {
            var termData = _termsDatabase[normalizedTerm];
            var related = termData.RelatedTerms.Take(maxResults).ToList();
            _cache.Set(cacheKey, related, _cacheExpiration);
            return Task.FromResult(related);
        }

        // Find terms with similar categories or content
        var relatedTerms = _termsDatabase.Values
            .Where(t => t.Term.ToLowerInvariant() != normalizedTerm)
            .Where(t => 
                t.Definition.ToLowerInvariant().Contains(normalizedTerm) ||
                t.RelatedTerms.Any(rt => rt.ToLowerInvariant().Contains(normalizedTerm)) ||
                t.Tags.Any(tag => tag.ToLowerInvariant().Contains(normalizedTerm)))
            .Select(t => t.Term)
            .Take(maxResults)
            .ToList();

        _cache.Set(cacheKey, relatedTerms, _cacheExpiration);
        return Task.FromResult(relatedTerms);
    }

    public Task<List<FinancialAidTerm>> GetTermsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Task.FromResult(new List<FinancialAidTerm>());

        var cacheKey = $"category:{category.ToLowerInvariant()}";
        
        if (_cache.TryGetValue(cacheKey, out List<FinancialAidTerm>? cachedTerms))
        {
            return Task.FromResult(cachedTerms ?? new List<FinancialAidTerm>());
        }

        var categoryTerms = _termsDatabase.Values
            .Where(term => term.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(term => term.Term)
            .ToList();

        _cache.Set(cacheKey, categoryTerms, _cacheExpiration);
        return Task.FromResult(categoryTerms);
    }

    public Task<List<FrequentlyAskedQuestion>> GetFrequentlyAskedQuestionsAsync(string topic, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return Task.FromResult(new List<FrequentlyAskedQuestion>());

        var cacheKey = $"faq:{topic.ToLowerInvariant()}:{maxResults}";
        
        if (_cache.TryGetValue(cacheKey, out List<FrequentlyAskedQuestion>? cachedFaqs))
        {
            return Task.FromResult(cachedFaqs ?? new List<FrequentlyAskedQuestion>());
        }

        var normalizedTopic = topic.ToLowerInvariant();
        var relevantFaqs = new List<FrequentlyAskedQuestion>();

        // Check if we have FAQs for this topic
        if (_faqDatabase.ContainsKey(normalizedTopic))
        {
            relevantFaqs.AddRange(_faqDatabase[normalizedTopic]);
        }

        // Also search across all FAQs for relevant content
        var additionalFaqs = _faqDatabase.Values
            .SelectMany(faqList => faqList)
            .Where(faq => 
                faq.Question.ToLowerInvariant().Contains(normalizedTopic) ||
                faq.Answer.ToLowerInvariant().Contains(normalizedTopic) ||
                faq.Tags.Any(tag => tag.ToLowerInvariant().Contains(normalizedTopic)))
            .Where(faq => !relevantFaqs.Any(rf => rf.Question == faq.Question))
            .OrderByDescending(faq => faq.ViewCount)
            .Take(maxResults - relevantFaqs.Count);

        relevantFaqs.AddRange(additionalFaqs);

        var result = relevantFaqs.Take(maxResults).ToList();
        _cache.Set(cacheKey, result, _cacheExpiration);
        return Task.FromResult(result);
    }

    public Task<bool> AddOrUpdateTermAsync(FinancialAidTerm term, CancellationToken cancellationToken = default)
    {
        if (term == null || string.IsNullOrWhiteSpace(term.Term))
            return Task.FromResult(false);

        try
        {
            var normalizedTerm = term.Term.ToLowerInvariant().Trim();
            _termsDatabase[normalizedTerm] = term;
            
            // Clear related cache entries
            var keysToRemove = _cache.GetType()
                .GetField("_coherentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_cache);
            
            _logger.LogInformation("Added/updated term: {Term}", term.Term);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add/update term: {Term}", term.Term);
            return Task.FromResult(false);
        }
    }

    private static Dictionary<string, FinancialAidTerm> InitializeTermsDatabase()
    {
        var terms = new Dictionary<string, FinancialAidTerm>();

        // Common financial aid terms
        var commonTerms = new[]
        {
            new FinancialAidTerm
            {
                Term = "FAFSA",
                Definition = "Free Application for Federal Student Aid - the form students must complete to apply for federal financial aid.",
                PlainLanguageDefinition = "A free form you fill out to apply for college financial aid from the government.",
                Category = "applications",
                Examples = new List<string> { "Complete your FAFSA by the deadline to qualify for federal grants and loans." },
                RelatedTerms = new List<string> { "Federal Aid", "Pell Grant", "Student Loans", "EFC" },
                Synonyms = new List<string> { "Free Application for Federal Student Aid" },
                Tags = new List<string> { "form", "federal", "application", "required" },
                IsOfficialTerm = true,
                PopularityScore = 10.0
            },
            new FinancialAidTerm
            {
                Term = "EFC",
                Definition = "Expected Family Contribution - the amount of money your family is expected to contribute toward your college education.",
                PlainLanguageDefinition = "How much money the government thinks your family can afford to pay for college each year.",
                Category = "calculations",
                Examples = new List<string> { "If your EFC is $5,000, you're expected to pay $5,000 toward college costs." },
                RelatedTerms = new List<string> { "FAFSA", "Financial Need", "Cost of Attendance" },
                Synonyms = new List<string> { "Expected Family Contribution" },
                Tags = new List<string> { "calculation", "family", "contribution", "financial need" },
                IsOfficialTerm = true,
                PopularityScore = 8.0
            },
            new FinancialAidTerm
            {
                Term = "Pell Grant",
                Definition = "A federal grant awarded to undergraduate students with exceptional financial need that does not need to be repaid.",
                PlainLanguageDefinition = "Free money from the government for college that you don't have to pay back, given to students whose families have low income.",
                Category = "grants",
                Examples = new List<string> { "Received a $6,000 Pell Grant to help pay for tuition and books." },
                RelatedTerms = new List<string> { "Federal Grant", "FAFSA", "Financial Need", "Undergraduate" },
                Synonyms = new List<string> { "Federal Pell Grant" },
                Tags = new List<string> { "grant", "federal", "need-based", "undergraduate", "free money" },
                IsOfficialTerm = true,
                PopularityScore = 9.0
            }
        };

        foreach (var term in commonTerms)
        {
            terms[term.Term.ToLowerInvariant()] = term;
        }

        return terms;
    }

    private static Dictionary<string, List<FrequentlyAskedQuestion>> InitializeFaqDatabase()
    {
        var faqs = new Dictionary<string, List<FrequentlyAskedQuestion>>();

        faqs["fafsa"] = new List<FrequentlyAskedQuestion>
        {
            new FrequentlyAskedQuestion
            {
                Question = "When is the FAFSA deadline?",
                Answer = "The federal deadline is June 30th, but many states and schools have earlier deadlines. It's best to submit as early as possible after October 1st.",
                Category = "deadlines",
                Tags = new List<string> { "deadline", "timing", "submission" },
                IsVerified = true,
                ViewCount = 1500
            },
            new FrequentlyAskedQuestion
            {
                Question = "What documents do I need for FAFSA?",
                Answer = "You'll need tax returns, bank statements, investment records, and Social Security cards for you and your parents if you're a dependent student.",
                Category = "documents",
                Tags = new List<string> { "documents", "requirements", "preparation" },
                IsVerified = true,
                ViewCount = 1200
            }
        };

        faqs["grants"] = new List<FrequentlyAskedQuestion>
        {
            new FrequentlyAskedQuestion
            {
                Question = "Do I have to pay back grants?",
                Answer = "No, grants are gift aid that you don't have to repay, as long as you meet the program requirements and stay enrolled.",
                Category = "grants",
                Tags = new List<string> { "grants", "repayment", "gift aid" },
                IsVerified = true,
                ViewCount = 800
            }
        };

        return faqs;
    }

    private static double CalculateRelevanceScore(FinancialAidTerm term, string query)
    {
        var score = 0.0;
        
        // Exact match in term name
        if (term.Term.ToLowerInvariant() == query)
            score += 10.0;
        else if (term.Term.ToLowerInvariant().Contains(query))
            score += 5.0;
        
        // Match in definition
        if (term.Definition.ToLowerInvariant().Contains(query))
            score += 2.0;
        
        // Match in synonyms
        if (term.Synonyms.Any(s => s.ToLowerInvariant().Contains(query)))
            score += 3.0;
        
        // Match in tags
        if (term.Tags.Any(t => t.ToLowerInvariant().Contains(query)))
            score += 1.0;
        
        // Boost popular terms
        score += term.PopularityScore * 0.1;
        
        return score;
    }

    private static void ParseStructuredHelp(ContextualHelp help, string content)
    {
        try
        {
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var currentSection = "";
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("Key Points:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "keypoints";
                }
                else if (trimmedLine.StartsWith("Steps:", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.StartsWith("Action", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "steps";
                }
                else if (trimmedLine.StartsWith("Common Mistakes:", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.StartsWith("Avoid:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "mistakes";
                }
                else if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("• "))
                {
                    var item = trimmedLine.Substring(2).Trim();
                    switch (currentSection)
                    {
                        case "keypoints":
                            help.KeyPoints.Add(item);
                            break;
                        case "steps":
                            help.ActionableSteps.Add(item);
                            break;
                        case "mistakes":
                            help.CommonMistakes.Add(item);
                            break;
                    }
                }
            }
        }
        catch (Exception)
        {
            // If parsing fails, just use the content as-is
        }
    }
}