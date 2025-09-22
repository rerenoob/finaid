import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { fafsaService } from '../services/api';
import type { FAFSAData } from '../types';

export const useFAFSAData = (applicationId?: string) => {
  return useQuery({
    queryKey: ['fafsa', applicationId],
    queryFn: () => {
      if (applicationId) {
        return fafsaService.getApplication(applicationId).then(res => res.data.data);
      }
      // Return empty form data for new applications
      return Promise.resolve({
        student: {
          firstName: '',
          lastName: '',
          ssn: '',
          dateOfBirth: '',
          email: '',
          phone: '',
          address: {
            street: '',
            city: '',
            state: '',
            zipCode: ''
          }
        },
        family: {
          maritalStatus: '',
          taxReturnStatus: '',
          householdSize: 1,
          numberOfCollegeStudents: 0
        },
        financial: {
          income: {
            adjustedGrossIncome: 0,
            wages: 0,
            taxableInterest: 0,
            taxExemptInterest: 0
          },
          assets: {
            cash: 0,
            investments: 0,
            realEstate: 0
          },
          benefits: {
            receivesSocialSecurity: false,
            receivesVeteransBenefits: false,
            receivesChildSupport: false
          }
        },
        schools: []
      } as FAFSAData);
    },
    enabled: true,
  });
};

export const useSubmitFAFSA = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (data: FAFSAData) => 
      fafsaService.submitApplication(data).then(res => res.data.data),
    onSuccess: () => {
      // Invalidate related queries
      queryClient.invalidateQueries({ queryKey: ['progress'] });
      queryClient.invalidateQueries({ queryKey: ['fafsa'] });
    },
  });
};

export const useValidateFAFSA = () => {
  return useMutation({
    mutationFn: (data: FAFSAData) => 
      fafsaService.validateApplication(data).then(res => res.data.data),
  });
};