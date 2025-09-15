import axios from 'axios';
import type { 
  FAFSAData, 
  SubmissionResponse, 
  EligibilityResult, 
  DocumentMetadata,
  ApplicationProgress,
  UserProfile,
  ApiResponse 
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5033';

const api = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor for auth tokens if needed
api.interceptors.request.use((config) => {
  // Add authentication token if available
  const token = localStorage.getItem('authToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// FAFSA services
export const fafsaService = {
  getApplication: (applicationId: string) => 
    api.get<ApiResponse<FAFSAData>>(`/fafsa/${applicationId}`),
    
  submitApplication: (data: FAFSAData) => 
    api.post<ApiResponse<SubmissionResponse>>('/fafsa/submit', data),
    
  validateApplication: (data: FAFSAData) => 
    api.post<ApiResponse<string[]>>('/fafsa/validate', data),
};

// Eligibility services
export const eligibilityService = {
  checkEligibility: (data: Partial<FAFSAData>) => 
    api.post<ApiResponse<EligibilityResult>>('/eligibility/check', data),
    
  getAidEstimate: (data: Partial<FAFSAData>) => 
    api.post<ApiResponse<{ estimatedAid: number }>>('/eligibility/estimate', data),
};

// Document services
export const documentService = {
  getDocuments: () => 
    api.get<ApiResponse<DocumentMetadata[]>>('/documents'),
    
  uploadDocument: (formData: FormData) => 
    api.post<ApiResponse<DocumentMetadata>>('/documents/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }),
    
  deleteDocument: (documentId: string) => 
    api.delete<ApiResponse<void>>(`/documents/${documentId}`),
};

// Progress services
export const progressService = {
  getProgress: () => 
    api.get<ApiResponse<ApplicationProgress>>('/progress'),
    
  updateStep: (step: number) => 
    api.post<ApiResponse<ApplicationProgress>>('/progress/step', { step }),
};

// User services
export const userService = {
  getProfile: () => 
    api.get<ApiResponse<UserProfile>>('/user/profile'),
    
  updateProfile: (profile: Partial<UserProfile>) => 
    api.put<ApiResponse<UserProfile>>('/user/profile', profile),
};

// Chat services
export const chatService = {
  sendMessage: (message: string, sessionId?: string) => 
    api.post<ApiResponse<{ response: string; sessionId: string }>>('/chat/message', { 
      message, 
      sessionId 
    }),
    
  getConversationHistory: (sessionId: string) => 
    api.get<ApiResponse<any>>(`/chat/history/${sessionId}`),
};

export default api;