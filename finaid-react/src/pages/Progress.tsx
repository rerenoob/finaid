import React from 'react';

const Progress: React.FC = () => {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Application Progress</h1>
        <p className="text-gray-600">Track your financial aid application status</p>
      </div>

      <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
        <h2 className="text-xl font-semibold text-blue-800 mb-2">Coming Soon</h2>
        <p className="text-blue-600">
          The progress tracking system is being migrated from Blazor to React. 
          This will include real-time updates and detailed status information.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-800 mb-4">Progress Metrics</h3>
          <ul className="space-y-2 text-gray-600">
            <li>• Form completion status</li>
            <li>• Document verification</li>
            <li>• Submission status</li>
            <li>• Eligibility assessment</li>
            <li>• Award estimation</li>
          </ul>
        </div>

        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-800 mb-4">Features</h3>
          <ul className="space-y-2 text-gray-600">
            <li>• Real-time progress updates</li>
            <li>• Step-by-step guidance</li>
            <li>• Deadline tracking</li>
            <li>• Notification system</li>
            <li>• Historical tracking</li>
          </ul>
        </div>
      </div>
    </div>
  );
};

export default Progress;