namespace finaid.Models.Application;

public enum ApplicationStatus
{
    Draft = 0,
    InProgress = 1,
    ReviewRequired = 2,
    Submitted = 3,
    Processing = 4,
    Processed = 5,
    Approved = 6,
    Rejected = 7,
    Completed = 8,
    Failed = 9,
    VerificationRequired = 10,
    OnHold = 11,
    Expired = 12
}