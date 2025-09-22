import React, { useState } from 'react';
import FAFSAForm from '../components/forms/FAFSAForm';
import ChatInterface from '../components/chat/ChatInterface';

const FAFSA: React.FC = () => {
  const [currentSection, setCurrentSection] = useState('student');

  const handleSectionChange = (section: string) => {
    setCurrentSection(section);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">FAFSA Application</h1>
        <p className="text-gray-600">Complete your Free Application for Federal Student Aid</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* FAFSA Form */}
        <div className="lg:col-span-2">
          <FAFSAForm onSectionChange={handleSectionChange} />
        </div>

        {/* AI Assistant Sidebar */}
        <div className="space-y-6">
          <div className="bg-white rounded-lg shadow-md border border-gray-200 h-[600px]">
            <ChatInterface formContext={{ currentSection }} />
          </div>
          
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <h3 className="font-semibold text-blue-800 mb-2">AI Assistant Tips</h3>
            <ul className="text-sm text-blue-700 space-y-1">
              <li>• Ask about specific form fields</li>
              <li>• Get help with document requirements</li>
              <li>• Understand eligibility criteria</li>
              <li>• Learn about deadlines</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
};

export default FAFSA;