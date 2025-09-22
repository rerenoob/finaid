import React from 'react';
import DashboardCard from '../components/common/DashboardCard';
import ProgressBar from '../components/dashboard/ProgressBar';
import StepIndicator from '../components/dashboard/StepIndicator';
import DeadlineList from '../components/dashboard/DeadlineList';
import { useProgress } from '../hooks/useProgress';

const Progress: React.FC = () => {
  const { data: progress } = useProgress();

  // Mock data for detailed progress tracking
  const applicationSteps = [
    { id: 'personal', label: 'Personal Information', completed: true, current: false },
    { id: 'financial', label: 'Financial Information', completed: false, current: true },
    { id: 'school', label: 'School Selection', completed: false, current: false },
    { id: 'review', label: 'Review & Sign', completed: false, current: false },
    { id: 'submit', label: 'Submission', completed: false, current: false },
  ];

  const verificationSteps = [
    { id: 'documents', label: 'Documents Uploaded', completed: true, current: false },
    { id: 'processing', label: 'Processing', completed: false, current: true },
    { id: 'verification', label: 'Verification', completed: false, current: false },
    { id: 'complete', label: 'Complete', completed: false, current: false },
  ];

  const deadlines = [
    {
      id: '1',
      title: 'FAFSA Submission Deadline',
      dueDate: new Date('2025-06-30'),
      type: 'fafsa' as const,
      priority: 'high' as const,
    },
    {
      id: '2', 
      title: 'Document Verification',
      dueDate: new Date('2025-05-15'),
      type: 'document' as const,
      priority: 'medium' as const,
    },
    {
      id: '3',
      title: 'State Aid Deadline',
      dueDate: new Date('2025-04-01'),
      type: 'institution' as const,
      priority: 'high' as const,
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Application Progress</h1>
        <p className="text-gray-600">Track your financial aid application status</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Overall Progress */}
        <DashboardCard title="Overall Progress" className="lg:col-span-2">
          <div className="space-y-6">
            <ProgressBar 
              progress={(progress?.currentProgress || 0) * 100} 
              label="Application Completion"
              size="lg"
            />
            
            <div>
              <h4 className="font-medium text-gray-700 mb-3">Application Steps</h4>
              <StepIndicator steps={applicationSteps} />
            </div>

            <div>
              <h4 className="font-medium text-gray-700 mb-3">Document Verification</h4>
              <StepIndicator steps={verificationSteps} />
            </div>
          </div>
        </DashboardCard>

        {/* Deadlines */}
        <DashboardCard title="Upcoming Deadlines">
          <DeadlineList deadlines={deadlines} maxItems={10} />
        </DashboardCard>
      </div>

      {/* Detailed Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <DashboardCard title="Form Completion">
          <div className="text-center">
            <div className="text-3xl font-bold text-blue-600 mb-2">
              {Math.round((progress?.currentProgress || 0) * 100)}%
            </div>
            <div className="text-sm text-gray-600">Form Completion</div>
          </div>
        </DashboardCard>

        <DashboardCard title="Documents">
          <div className="text-center">
            <div className="text-3xl font-bold text-green-600 mb-2">2/5</div>
            <div className="text-sm text-gray-600">Documents Uploaded</div>
          </div>
        </DashboardCard>

        <DashboardCard title="Deadline">
          <div className="text-center">
            <div className="text-3xl font-bold text-yellow-600 mb-2">15</div>
            <div className="text-sm text-gray-600">Days Remaining</div>
          </div>
        </DashboardCard>

        <DashboardCard title="Schools">
          <div className="text-center">
            <div className="text-3xl font-bold text-purple-600 mb-2">3</div>
            <div className="text-sm text-gray-600">Schools Selected</div>
          </div>
        </DashboardCard>
      </div>

      {/* Recent Activity */}
      <DashboardCard title="Recent Activity">
        <div className="space-y-4">
          <div className="flex items-center space-x-4 p-3 bg-green-50 rounded-lg">
            <div className="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center">
              <span className="text-green-600">✓</span>
            </div>
            <div>
              <p className="font-medium">Personal information completed</p>
              <p className="text-sm text-gray-500">Completed 2 hours ago</p>
            </div>
          </div>

          <div className="flex items-center space-x-4 p-3 bg-blue-50 rounded-lg">
            <div className="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center">
              <span className="text-blue-600">📄</span>
            </div>
            <div>
              <p className="font-medium">Tax document uploaded</p>
              <p className="text-sm text-gray-500">Uploaded 1 day ago</p>
            </div>
          </div>

          <div className="flex items-center space-x-4 p-3 bg-yellow-50 rounded-lg">
            <div className="w-10 h-10 bg-yellow-100 rounded-full flex items-center justify-center">
              <span className="text-yellow-600">⏳</span>
            </div>
            <div>
              <p className="font-medium">Financial information in progress</p>
              <p className="text-sm text-gray-500">Started 3 days ago</p>
            </div>
          </div>
        </div>
      </DashboardCard>
    </div>
  );
};

export default Progress;