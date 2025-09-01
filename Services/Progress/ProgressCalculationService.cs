using finaid.Models.Progress;

namespace finaid.Services.Progress;

public class ProgressCalculationService
{
    private readonly ILogger<ProgressCalculationService> _logger;

    public ProgressCalculationService(ILogger<ProgressCalculationService> logger)
    {
        _logger = logger;
    }

    public decimal CalculateOverallProgress(List<ProgressStep> steps)
    {
        if (!steps.Any())
            return 0;

        var totalWeight = steps.Count;
        var completedWeight = steps.Where(s => s.Status == StepStatus.Completed).Count();
        var inProgressWeight = steps.Where(s => s.Status == StepStatus.InProgress).Sum(s => s.Completion / 100);

        return Math.Round(((completedWeight + inProgressWeight) / totalWeight) * 100, 1);
    }

    public List<ProgressStep> GetFAFSAProgressSteps()
    {
        return new List<ProgressStep>
        {
            new ProgressStep
            {
                StepName = "student-demographics",
                DisplayName = "Student Information",
                Status = StepStatus.Completed,
                Completion = 100,
                IsOptional = false,
                CompletedAt = DateTime.UtcNow.AddDays(-5),
                Description = "Basic student demographic information",
                Order = 1
            },
            new ProgressStep
            {
                StepName = "parent-demographics",
                DisplayName = "Parent Information",
                Status = StepStatus.InProgress,
                Completion = 75,
                IsOptional = false,
                RequiredActions = new List<string> { "Complete parent contact information" },
                Description = "Parent demographic and contact information",
                Order = 2
            },
            new ProgressStep
            {
                StepName = "financial-information",
                DisplayName = "Financial Information",
                Status = StepStatus.InProgress,
                Completion = 40,
                IsOptional = false,
                RequiredActions = new List<string> 
                { 
                    "Enter tax information",
                    "Upload W-2 forms",
                    "Provide bank statements"
                },
                Description = "Student and parent financial information",
                Order = 3
            },
            new ProgressStep
            {
                StepName = "school-selection",
                DisplayName = "School Selection",
                Status = StepStatus.NotStarted,
                Completion = 0,
                IsOptional = false,
                RequiredActions = new List<string> { "Select schools to receive FAFSA" },
                Description = "Choose schools to receive your FAFSA information",
                Order = 4
            },
            new ProgressStep
            {
                StepName = "signatures",
                DisplayName = "Signatures",
                Status = StepStatus.NotStarted,
                Completion = 0,
                IsOptional = false,
                RequiredActions = new List<string> { "Sign FAFSA electronically" },
                Description = "Electronic signatures from student and parent",
                Order = 5
            },
            new ProgressStep
            {
                StepName = "review-submit",
                DisplayName = "Review & Submit",
                Status = StepStatus.NotStarted,
                Completion = 0,
                IsOptional = false,
                RequiredActions = new List<string> { "Review all information and submit" },
                Description = "Final review and submission of FAFSA",
                Order = 6
            }
        };
    }

    public List<ProgressStep> GetStateAidProgressSteps()
    {
        return new List<ProgressStep>
        {
            new ProgressStep
            {
                StepName = "eligibility-check",
                DisplayName = "Eligibility Check",
                Status = StepStatus.Completed,
                Completion = 100,
                IsOptional = false,
                CompletedAt = DateTime.UtcNow.AddDays(-3),
                Description = "Verify state aid eligibility requirements",
                Order = 1
            },
            new ProgressStep
            {
                StepName = "application-form",
                DisplayName = "Application Form",
                Status = StepStatus.NotStarted,
                Completion = 0,
                IsOptional = false,
                RequiredActions = new List<string> { "Complete state aid application" },
                Description = "Fill out state-specific financial aid application",
                Order = 2
            },
            new ProgressStep
            {
                StepName = "income-verification",
                DisplayName = "Income Verification",
                Status = StepStatus.RequiresAction,
                Completion = 0,
                IsOptional = false,
                RequiredActions = new List<string> 
                { 
                    "Upload tax returns",
                    "Provide income verification letter"
                },
                Description = "Submit required income documentation",
                Order = 3
            },
            new ProgressStep
            {
                StepName = "residency-proof",
                DisplayName = "Residency Proof",
                Status = StepStatus.NotStarted,
                Completion = 0,
                IsOptional = false,
                RequiredActions = new List<string> { "Provide residency documentation" },
                Description = "Submit proof of state residency",
                Order = 4
            }
        };
    }

    public ProgressApplicationStatus DetermineApplicationStatus(List<ProgressStep> steps, decimal overallProgress)
    {
        if (overallProgress == 0)
            return ProgressApplicationStatus.NotStarted;

        if (steps.Any(s => s.Status == StepStatus.RequiresAction || s.Status == StepStatus.Blocked))
            return ProgressApplicationStatus.RequiresAction;

        if (overallProgress == 100)
        {
            if (steps.All(s => s.Status == StepStatus.Completed))
                return ProgressApplicationStatus.Submitted;
            else
                return ProgressApplicationStatus.AwaitingReview;
        }

        return ProgressApplicationStatus.InProgress;
    }

    public List<string> GetBlockingIssues(List<ProgressStep> steps)
    {
        var issues = new List<string>();

        foreach (var step in steps.Where(s => s.Status == StepStatus.RequiresAction || s.Status == StepStatus.Blocked))
        {
            issues.AddRange(step.RequiredActions);
        }

        return issues.Distinct().ToList();
    }

    public DateTime? GetNextDeadline(string applicationType)
    {
        // Mock deadlines - in a real app, these would come from configuration or database
        return applicationType switch
        {
            "FAFSA" => DateTime.UtcNow.AddDays(45),
            "StateAid" => DateTime.UtcNow.AddDays(30),
            "Scholarship" => DateTime.UtcNow.AddDays(60),
            _ => null
        };
    }
}