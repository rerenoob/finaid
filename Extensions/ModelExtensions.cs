using finaid.Models.FAFSA;
using finaid.Models.Application;
using System.Text.Json;
using finaid.Models.Federal;

namespace finaid.Extensions;

/// <summary>
/// Extension methods for model conversion and mapping utilities
/// </summary>
public static class ModelExtensions
{
    /// <summary>
    /// Converts a FAFSA application to API submission format
    /// </summary>
    public static object ToApiSubmissionFormat(this FAFSAApplication application)
    {
        // Parse the stored JSON form data
        var formData = string.IsNullOrEmpty(application.FormDataJson) 
            ? new Dictionary<string, object>() 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(application.FormDataJson) ?? new Dictionary<string, object>();

        return new
        {
            ApplicationId = application.Id,
            AwardYear = application.AwardYear,
            UserId = application.UserId,
            SubmissionTimestamp = DateTime.UtcNow,
            FormData = formData,
            Status = application.Status.ToString()
        };
    }

    /// <summary>
    /// Converts StudentInformation to API format
    /// </summary>
    public static object ToApiFormat(this StudentInformation student)
    {
        return new
        {
            PersonalInfo = new
            {
                FirstName = student.FirstName,
                MiddleName = student.MiddleName,
                LastName = student.LastName,
                Suffix = student.Suffix,
                DateOfBirth = student.DateOfBirth.ToString("yyyy-MM-dd"),
                SSN = student.EncryptedSSN, // API expects encrypted format
                Sex = student.Sex,
                MaritalStatus = student.MaritalStatus,
                MaritalStatusDate = student.MaritalStatusDate?.ToString("yyyy-MM-dd")
            },
            CitizenshipInfo = new
            {
                CitizenshipStatus = student.CitizenshipStatus,
                AlienRegistrationNumber = student.AlienRegistrationNumber,
                StateOfLegalResidence = student.StateOfLegalResidence,
                ResidencyDate = student.ResidencyDate.ToString("yyyy-MM-dd")
            },
            ContactInfo = new
            {
                PermanentAddress = new
                {
                    Address = student.PermanentAddress,
                    City = student.PermanentCity,
                    State = student.PermanentState,
                    ZipCode = student.PermanentZipCode
                },
                MailingAddress = new
                {
                    Address = student.MailingAddress,
                    City = student.MailingCity,
                    State = student.MailingState,
                    ZipCode = student.MailingZipCode
                },
                PhoneNumber = student.PhoneNumber,
                Email = student.Email,
                AlternateEmail = student.AlternateEmail
            },
            EducationInfo = new
            {
                GradeLevel = student.GradeLevel,
                DegreeType = student.DegreeType,
                IsFirstGeneration = student.IsFirstGeneration,
                HighSchool = new
                {
                    Name = student.HighSchoolName,
                    State = student.HighSchoolState,
                    GraduationYear = student.HighSchoolGraduationYear
                },
                HasHighSchoolDiploma = student.HasHighSchoolDiploma,
                GEDDate = student.GEDDate?.ToString("yyyy-MM-dd")
            },
            SpecialCircumstances = new
            {
                IsOrphan = student.IsOrphan,
                IsWardOfCourt = student.IsWardOfCourt,
                WasInFosterCare = student.WasInFosterCare,
                IsEmancipatedMinor = student.IsEmancipatedMinor,
                IsHomeless = student.IsHomeless,
                AtRiskOfHomelessness = student.AtRiskOfHomelessness
            },
            MilitaryService = new
            {
                HasMilitaryService = student.HasMilitaryService,
                IsVeteran = student.IsVeteran,
                HasVeteranBenefits = student.HasVeteranBenefits
            }
        };
    }

    /// <summary>
    /// Converts FinancialInformation to API format
    /// </summary>
    public static object ToApiFormat(this FinancialInformation financial)
    {
        return new
        {
            TaxYear = financial.TaxYear,
            StudentFinancialInfo = new
            {
                AdjustedGrossIncome = financial.StudentAdjustedGrossIncome,
                IncomeTax = financial.StudentIncomeTax,
                UntaxedIncome = financial.StudentUntaxedIncome,
                AdditionalFinancialInfo = financial.StudentAdditionalFinancialInfo,
                CashSavingsChecking = financial.StudentCashSavingsChecking,
                InvestmentValue = financial.StudentInvestmentValue,
                BusinessFarmValue = financial.StudentBusinessFarmValue,
                TaxFilingStatus = new
                {
                    FiledTaxReturn = financial.StudentFiledTaxReturn,
                    WillFileTaxReturn = financial.StudentWillFileTaxReturn,
                    EligibleToFile1040EZ = financial.StudentEligibleToFile1040EZ,
                    ReceivedW2 = financial.StudentReceivedW2
                },
                Benefits = new
                {
                    SocialSecurityBenefits = financial.StudentSocialSecurityBenefits,
                    WelfareTemporaryAssistance = financial.StudentWelfareTemporaryAssistance,
                    VeteransBenefits = financial.StudentVeteransBenefits,
                    WorkStudy = financial.StudentWorkStudy,
                    AssistantshipStipend = financial.StudentAssistantshipStipend,
                    OtherUntaxedIncome = financial.StudentOtherUntaxedIncome
                }
            },
            SpouseFinancialInfo = new
            {
                AdjustedGrossIncome = financial.SpouseAdjustedGrossIncome,
                IncomeTax = financial.SpouseIncomeTax,
                UntaxedIncome = financial.SpouseUntaxedIncome,
                TaxFilingStatus = new
                {
                    FiledTaxReturn = financial.SpouseFiledTaxReturn,
                    WillFileTaxReturn = financial.SpouseWillFileTaxReturn
                }
            },
            ParentFinancialInfo = new
            {
                AdjustedGrossIncome = financial.ParentAdjustedGrossIncome,
                IncomeTax = financial.ParentIncomeTax,
                UntaxedIncome = financial.ParentUntaxedIncome,
                AdditionalFinancialInfo = financial.ParentAdditionalFinancialInfo,
                CashSavingsChecking = financial.ParentCashSavingsChecking,
                InvestmentValue = financial.ParentInvestmentValue,
                BusinessFarmValue = financial.ParentBusinessFarmValue,
                TaxFilingStatus = new
                {
                    FiledTaxReturn = financial.ParentFiledTaxReturn,
                    WillFileTaxReturn = financial.ParentWillFileTaxReturn,
                    EligibleToFile1040EZ = financial.ParentEligibleToFile1040EZ
                },
                Benefits = new
                {
                    SocialSecurityBenefits = financial.ParentSocialSecurityBenefits,
                    WelfareTemporaryAssistance = financial.ParentWelfareTemporaryAssistance,
                    DisabilityBenefits = financial.ParentDisabilityBenefits,
                    OtherUntaxedIncome = financial.ParentOtherUntaxedIncome
                }
            },
            HouseholdInfo = new
            {
                HouseholdSize = financial.HouseholdSize,
                NumberInCollege = financial.NumberInCollege
            },
            SpecialCircumstances = new
            {
                HasSpecialCircumstances = financial.HasSpecialCircumstances,
                Explanation = financial.SpecialCircumstancesExplanation
            },
            VerificationInfo = new
            {
                RequiresVerification = financial.RequiresVerification,
                TaxTranscriptRequested = financial.TaxTranscriptRequested,
                DocumentsSubmitted = financial.VerificationDocumentsSubmitted,
                Deadline = financial.VerificationDeadline?.ToString("yyyy-MM-dd")
            }
        };
    }

    /// <summary>
    /// Converts FamilyInformation to API format
    /// </summary>
    public static object ToApiFormat(this FamilyInformation family)
    {
        return new
        {
            RelationshipType = family.RelationshipType,
            PersonalInfo = new
            {
                FirstName = family.FirstName,
                MiddleName = family.MiddleName,
                LastName = family.LastName,
                Suffix = family.Suffix,
                DateOfBirth = family.DateOfBirth?.ToString("yyyy-MM-dd"),
                EncryptedSSN = family.EncryptedSSN,
                Sex = family.Sex,
                MaritalStatus = family.MaritalStatus
            },
            ContactInfo = new
            {
                Address = family.Address,
                City = family.City,
                State = family.State,
                ZipCode = family.ZipCode,
                PhoneNumber = family.PhoneNumber,
                Email = family.Email
            },
            EducationInfo = new
            {
                IsInCollege = family.IsInCollege,
                CollegeName = family.CollegeName,
                CollegeCode = family.CollegeCode,
                GradeLevel = family.GradeLevel,
                DegreeType = family.DegreeType,
                EnrollmentStartDate = family.EnrollmentStartDate?.ToString("yyyy-MM-dd"),
                ExpectedGraduationDate = family.ExpectedGraduationDate?.ToString("yyyy-MM-dd")
            },
            EmploymentInfo = new
            {
                EducationLevel = family.EducationLevel,
                EmploymentStatus = family.EmploymentStatus,
                Employer = family.Employer,
                JobTitle = family.JobTitle
            },
            SpecialCircumstances = new
            {
                HasSpecialNeeds = family.HasSpecialNeeds,
                SpecialNeedsDescription = family.SpecialNeedsDescription,
                HasDisability = family.HasDisability,
                DisabilityDescription = family.DisabilityDescription,
                IsDeceased = family.IsDeceased,
                DateOfDeath = family.DateOfDeath?.ToString("yyyy-MM-dd")
            },
            CitizenshipInfo = new
            {
                CitizenshipStatus = family.CitizenshipStatus,
                CountryOfOrigin = family.CountryOfOrigin
            }
        };
    }

    /// <summary>
    /// Converts SchoolSelection to API format
    /// </summary>
    public static object ToApiFormat(this SchoolSelection school)
    {
        return new
        {
            SchoolInfo = new
            {
                FederalSchoolCode = school.FederalSchoolCode,
                SchoolName = school.SchoolName,
                Address = new
                {
                    Address = school.Address,
                    City = school.City,
                    State = school.State,
                    ZipCode = school.ZipCode
                },
                ContactInfo = new
                {
                    PhoneNumber = school.PhoneNumber,
                    Website = school.Website,
                    FinancialAidEmail = school.FinancialAidEmail
                }
            },
            AcademicPlans = new
            {
                IntendedGradeLevel = school.IntendedGradeLevel,
                IntendedDegreeType = school.IntendedDegreeType,
                IntendedMajor = school.IntendedMajor,
                IntendedMinor = school.IntendedMinor,
                EnrollmentStatus = school.EnrollmentStatus,
                HousingPreference = school.HousingPreference,
                IntendedStartDate = school.IntendedStartDate?.ToString("yyyy-MM-dd")
            },
            SchoolCharacteristics = new
            {
                SchoolType = school.SchoolType,
                ControlType = school.ControlType,
                IsInState = school.IsInState,
                IsTwoYearCollege = school.IsTwoYearCollege,
                IsFourYearCollege = school.IsFourYearCollege,
                IsGraduateSchool = school.IsGraduateSchool
            },
            CostInformation = new
            {
                EstimatedCostOfAttendance = school.EstimatedCostOfAttendance,
                TuitionAndFees = school.TuitionAndFees,
                RoomAndBoard = school.RoomAndBoard,
                BooksAndSupplies = school.BooksAndSupplies,
                PersonalExpenses = school.PersonalExpenses,
                TransportationCosts = school.TransportationCosts
            },
            ApplicationInfo = new
            {
                IsFirstChoice = school.IsFirstChoice,
                SelectionOrder = school.SelectionOrder,
                ApplicationStatus = school.ApplicationStatus,
                ApplicationSubmittedDate = school.ApplicationSubmittedDate?.ToString("yyyy-MM-dd"),
                AcceptanceDate = school.AcceptanceDate?.ToString("yyyy-MM-dd")
            }
        };
    }

    /// <summary>
    /// Creates a FAFSA application from API response
    /// </summary>
    public static FAFSAApplication FromApiResponse(ApiResponse<object> response, Guid userId, int awardYear)
    {
        var application = new FAFSAApplication
        {
            UserId = userId,
            AwardYear = awardYear,
            Status = ApplicationStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
            FormDataJson = JsonSerializer.Serialize(response.Data)
        };

        return application;
    }

    /// <summary>
    /// Validates if all required FAFSA sections are complete
    /// </summary>
    public static bool IsComplete(this FAFSAApplication application)
    {
        return application.CompletionPercentage >= 100;
    }

    /// <summary>
    /// Gets the missing sections for a FAFSA application
    /// </summary>
    public static List<string> GetMissingSections(this FAFSAApplication application)
    {
        var missingSections = new List<string>();

        if (string.IsNullOrEmpty(application.FormDataJson))
        {
            missingSections.AddRange(new[] {
                "Student Information",
                "Financial Information", 
                "Family Information",
                "School Selections"
            });
        }

        return missingSections;
    }

    /// <summary>
    /// Converts a SubmissionRequest to API submission format
    /// </summary>
    public static object ToApiSubmissionFormat(this SubmissionRequest request)
    {
        return new
        {
            ApplicationId = request.ApplicationId,
            AwardYear = request.AwardYear,
            StudentInfo = request.StudentInfo?.ToApiFormat(),
            FinancialInfo = request.FinancialInfo?.ToApiFormat(),
            FamilyMembers = request.FamilyMembers?.Select(f => f.ToApiFormat()),
            SchoolSelections = request.SchoolSelections?.Select(s => s.ToApiFormat()),
            Signatures = new
            {
                Student = request.StudentSignature,
                Parent = request.ParentSignature,
                Spouse = request.SpouseSignature
            },
            Authentication = new
            {
                FSAIdUsername = request.FSAIdUsername,
                SubmissionTimestamp = request.SubmissionTimestamp,
                IPAddress = request.SubmitterIPAddress,
                UserAgent = request.SubmitterUserAgent
            },
            Correction = new
            {
                IsCorrection = request.IsCorrection,
                PreviousConfirmationNumber = request.PreviousConfirmationNumber,
                Changes = request.ChangeSummary
            },
            SupportingDocuments = request.SupportingDocuments
        };
    }
}