import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { progressService } from '../services/api';
import type { ApplicationProgress } from '../types';

export const useProgress = () => {
  return useQuery({
    queryKey: ['progress'],
    queryFn: () => progressService.getProgress().then(res => res.data.data),
    staleTime: 1000 * 60 * 5, // 5 minutes
  });
};

export const useUpdateProgressStep = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (step: number) => 
      progressService.updateStep(step).then(res => res.data.data),
    onSuccess: (updatedProgress: ApplicationProgress) => {
      // Update the progress query with new data
      queryClient.setQueryData(['progress'], updatedProgress);
    },
  });
};