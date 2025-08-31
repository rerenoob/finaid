namespace finaid.Models.Document;

public enum DocumentType
{
    TaxReturn = 0,
    W2Form = 1,
    BankStatement = 2,
    SocialSecurityCard = 3,
    DriversLicense = 4,
    Passport = 5,
    BirthCertificate = 6,
    HighSchoolTranscript = 7,
    CollegeTranscript = 8,
    VerificationWorksheet = 9,
    Other = 10
}

public enum DocumentStatus
{
    Uploaded = 0,
    Processing = 1,
    Verified = 2,
    Rejected = 3,
    RequiresAction = 4
}