// User types
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
}

export interface UserProfile {
  userId: string;
  dateOfBirth?: string;
  ssn?: string;
  address?: string;
  phoneNumber?: string;
}

// FAFSA types
export interface FAFSAData {
  student: StudentInformation;
  family: FamilyInformation;
  financial: FinancialInformation;
  schools: SchoolInformation[];
  confirmation?: boolean;
}

export interface StudentInformation {
  firstName: string;
  lastName: string;
  ssn: string;
  dateOfBirth: string;
  email: string;
  phone: string;
  address: Address;
}

export interface Address {
  street: string;
  city: string;
  state: string;
  zipCode: string;
}

export interface FamilyInformation {
  maritalStatus: string;
  taxReturnStatus: string;
  householdSize: number;
  numberOfCollegeStudents: number;
  parents?: ParentInformation[];
}

export interface ParentInformation {
  firstName: string;
  lastName: string;
  ssn: string;
  dateOfBirth: string;
  employmentStatus: string;
}

export interface FinancialInformation {
  income: IncomeInformation;
  assets: AssetInformation;
  benefits: BenefitInformation;
}

export interface IncomeInformation {
  adjustedGrossIncome: number;
  wages: number;
  taxableInterest: number;
  taxExemptInterest: number;
}

export interface AssetInformation {
  cash: number;
  investments: number;
  realEstate: number;
}

export interface BenefitInformation {
  receivesSocialSecurity: boolean;
  receivesVeteransBenefits: boolean;
  receivesChildSupport: boolean;
}

export interface SchoolInformation {
  schoolId: string;
  schoolName: string;
  housingPlan: string;
  enrollmentStatus: string;
}

// Progress types
export interface ApplicationProgress {
  currentStep: number;
  totalSteps: number;
  completedSteps: number;
  currentProgress: number;
}

// Document types
export interface DocumentMetadata {
  id: string;
  fileName: string;
  fileType: string;
  fileSize: number;
  uploadDate: string;
  status: 'pending' | 'processing' | 'verified' | 'rejected';
}

// Chat types
export interface ChatMessage {
  id: string;
  content: string;
  sender: 'user' | 'assistant';
  timestamp: string;
}

export interface ConversationSession {
  id: string;
  messages: ChatMessage[];
  createdAt: string;
  updatedAt: string;
}

// API Response types
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

export interface SubmissionResponse {
  submissionId: string;
  status: 'submitted' | 'processing' | 'completed' | 'error';
  message?: string;
  estimatedCompletionTime?: string;
}

export interface EligibilityResult {
  isEligible: boolean;
  estimatedAid: number;
  reasons: string[];
  recommendedActions: string[];
}