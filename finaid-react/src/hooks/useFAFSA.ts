import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { fafsaService } from '../services/api';
import type { FAFSAData } from '../types';

export const useFAFSAData = (applicationId: string) => {
  return useQuery({
    queryKey: ['fafsa', applicationId],
    queryFn: () => fafsaService.getApplication(applicationId).then(res => res.data.data),
    enabled: !!applicationId,
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