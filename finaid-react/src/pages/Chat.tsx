import React from 'react';
import ChatInterface from '../components/chat/ChatInterface';

const Chat: React.FC = () => {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">AI Assistant</h1>
        <p className="text-gray-600">Get help with your financial aid questions</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Chat Interface */}
        <div className="lg:col-span-2">
          <div className="bg-white rounded-lg shadow-md border border-gray-200 h-[600px]">
            <ChatInterface />
          </div>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
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
            <h3 className="text-lg font-semibold text-gray-800 mb-4">Quick Actions</h3>
            <div className="space-y-2">
              <button className="w-full text-left p-3 bg-blue-50 hover:bg-blue-100 rounded-lg transition-colors">
                <span className="text-blue-700 font-medium">Help with FAFSA form</span>
              </button>
              <button className="w-full text-left p-3 bg-green-50 hover:bg-green-100 rounded-lg transition-colors">
                <span className="text-green-700 font-medium">Document requirements</span>
              </button>
              <button className="w-full text-left p-3 bg-purple-50 hover:bg-purple-100 rounded-lg transition-colors">
                <span className="text-purple-700 font-medium">Deadline information</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Chat;