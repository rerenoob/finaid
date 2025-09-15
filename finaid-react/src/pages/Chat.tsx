import React from 'react';

const Chat: React.FC = () => {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">AI Assistant</h1>
        <p className="text-gray-600">Get help with your financial aid questions</p>
      </div>

      <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
        <h2 className="text-xl font-semibold text-blue-800 mb-2">Coming Soon</h2>
        <p className="text-blue-600">
          The AI chat assistant is being migrated from Blazor to React. 
          This will include real-time messaging and contextual assistance.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-800 mb-4">Capabilities</h3>
          <ul className="space-y-2 text-gray-600">
            <li>• Form field explanations</li>
            <li>• Document requirements</li>
            <li>• Eligibility questions</li>
            <li>• Deadline information</li>
            <li>• Technical support</li>
          </ul>
        </div>

        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-800 mb-4">Features</h3>
          <ul className="space-y-2 text-gray-600">
            <li>• Real-time messaging</li>
            <li>• Context-aware responses</li>
            <li>• Form integration</li>
            <li>• Conversation history</li>
            <li>• Multi-language support</li>
          </ul>
        </div>
      </div>
    </div>
  );
};

export default Chat;