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
  studentInformation: StudentInformation;
  familyInformation: FamilyInformation;
  financialInformation: FinancialInformation;
  schoolInformation: SchoolInformation;
}

export interface StudentInformation {
  firstName: string;
  lastName: string;
  ssn: string;
  dateOfBirth: string;
  citizenshipStatus: string;
}

export interface FamilyInformation {
  maritalStatus: string;
  taxFilingStatus: string;
  householdSize: number;
  numberOfCollegeStudents: number;
}

export interface FinancialInformation {
  income: number;
  assets: number;
  expenses: number;
}

export interface SchoolInformation {
  schoolCode: string;
  schoolName: string;
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