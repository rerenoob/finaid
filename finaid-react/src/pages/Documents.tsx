import React from 'react';

const Documents: React.FC = () => {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Document Management</h1>
        <p className="text-gray-600">Upload and manage your financial aid documents</p>
      </div>

      <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
        <h2 className="text-xl font-semibold text-blue-800 mb-2">Coming Soon</h2>
        <p className="text-blue-600">
          The document management system is being migrated from Blazor to React. 
          This will include file upload, OCR processing, and verification features.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-800 mb-4">Document Types</h3>
          <ul className="space-y-2 text-gray-600">
            <li>• Tax Returns (W-2, 1099)</li>
            <li>• Identification Documents</li>
            <li>• Income Verification</li>
            <li>• Bank Statements</li>
            <li>• School Records</li>
          </ul>
        </div>

        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-800 mb-4">Features</h3>
          <ul className="space-y-2 text-gray-600">
            <li>• Drag & drop upload</li>
            <li>• Automatic OCR processing</li>
            <li>• Document verification</li>
            <li>• Progress tracking</li>
            <li>• Secure storage</li>
          </ul>
        </div>
      </div>
    </div>
  );
};

export default Documents;