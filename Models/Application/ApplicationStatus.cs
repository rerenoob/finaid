namespace finaid.Models.Application;

public enum ApplicationStatus
{
    Draft = 0,
    InProgress = 1,
    ReviewRequired = 2,
    Submitted = 3,
    Processed = 4,
    Approved = 5,
    Rejected = 6,
    Expired = 7
}