import React from 'react';
import DashboardCard from '../components/common/DashboardCard';
import LoadingSpinner from '../components/common/LoadingSpinner';
import ProgressBar from '../components/dashboard/ProgressBar';
import StepIndicator from '../components/dashboard/StepIndicator';
import DeadlineList from '../components/dashboard/DeadlineList';
import { useProgress } from '../hooks/useProgress';

const Dashboard: React.FC = () => {
  const { data: progress, isLoading, error } = useProgress();

  // Mock data for demonstration
  const steps = [
    { id: 'personal', label: 'Personal Info', completed: true, current: false },
    { id: 'financial', label: 'Financial Info', completed: false, current: true },
    { id: 'school', label: 'School Selection', completed: false, current: false },
    { id: 'review', label: 'Review', completed: false, current: false },
    { id: 'submit', label: 'Submit', completed: false, current: false },
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

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-64">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <h3 className="text-red-800 font-semibold">Error loading dashboard</h3>
        <p className="text-red-600">Please try again later.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Dashboard</h1>
        <p className="text-gray-600">Welcome to your financial aid application dashboard</p>
      </div>

      {/* Progress Overview */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <DashboardCard title="Application Progress">
          <div className="space-y-6">
            <ProgressBar 
              progress={(progress?.currentProgress || 0) * 100} 
              size="lg"
              showPercentage={true}
            />
            <StepIndicator steps={steps} />
            <div className="flex justify-between text-sm text-gray-600">
              <span>{Math.round((progress?.currentProgress || 0) * 100)}% Complete</span>
              <span>Step {progress?.currentStep} of {progress?.totalSteps}</span>
            </div>
          </div>
        </DashboardCard>

        <DashboardCard title="Upcoming Deadlines">
          <DeadlineList deadlines={deadlines} />
        </DashboardCard>
      </div>

      {/* Quick Actions */}
      <DashboardCard title="Quick Actions">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <button className="bg-primary hover:bg-primary-dark text-white px-4 py-3 rounded-lg text-center transition-colors">
            <div className="text-2xl mb-2">📝</div>
            <div className="font-semibold">Continue FAFSA</div>
            <div className="text-sm opacity-90">Complete your application</div>
          </button>

          <button className="bg-green-600 hover:bg-green-700 text-white px-4 py-3 rounded-lg text-center transition-colors">
            <div className="text-2xl mb-2">📁</div>
            <div className="font-semibold">Upload Documents</div>
            <div className="text-sm opacity-90">Submit required files</div>
          </button>

          <button className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-3 rounded-lg text-center transition-colors">
            <div className="text-2xl mb-2">💬</div>
            <div className="font-semibold">Get Help</div>
            <div className="text-sm opacity-90">Chat with assistant</div>
          </button>
        </div>
      </DashboardCard>

      {/* Recent Activity */}
      <DashboardCard title="Recent Activity">
        <div className="space-y-3">
          <div className="flex items-center space-x-3 p-3 bg-gray-50 rounded-lg">
            <div className="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center">
              <span className="text-green-600">✓</span>
            </div>
            <div>
              <p className="font-medium">Personal information completed</p>
              <p className="text-sm text-gray-500">2 hours ago</p>
            </div>
          </div>

          <div className="flex items-center space-x-3 p-3 bg-gray-50 rounded-lg">
            <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
              <span className="text-blue-600">📄</span>
            </div>
            <div>
              <p className="font-medium">Tax document uploaded</p>
              <p className="text-sm text-gray-500">1 day ago</p>
            </div>
          </div>
        </div>
      </DashboardCard>

      {/* Next Steps */}
      <DashboardCard title="Next Steps">
        <div className="space-y-3">
          <div className="flex items-center justify-between p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
            <div>
              <p className="font-medium text-yellow-800">Complete financial information</p>
              <p className="text-sm text-yellow-600">Required for aid calculation</p>
            </div>
            <button className="bg-yellow-500 hover:bg-yellow-600 text-white px-3 py-1 rounded text-sm">
              Start
            </button>
          </div>

          <div className="flex items-center justify-between p-3 bg-blue-50 border border-blue-200 rounded-lg">
            <div>
              <p className="font-medium text-blue-800">Review school selection</p>
              <p className="text-sm text-blue-600">Add up to 10 schools</p>
            </div>
            <button className="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm">
              Review
            </button>
          </div>
        </div>
      </DashboardCard>
    </div>
  );
};

export default Dashboard;