import React, { useState } from 'react';
import DocumentUpload from '../components/documents/DocumentUpload';
import DocumentList from '../components/documents/DocumentList';

interface Document {
  id: string;
  name: string;
  type: string;
  size: number;
  uploadedAt: Date;
  status: 'pending' | 'verified' | 'rejected';
  verificationResult?: string;
}

const Documents: React.FC = () => {
  const [documents, setDocuments] = useState<Document[]>([]);

  const handleUploadComplete = (files: File[]) => {
    const newDocuments: Document[] = files.map(file => ({
      id: Date.now().toString() + Math.random().toString(36).substr(2, 9),
      name: file.name,
      type: file.type || 'document',
      size: file.size,
      uploadedAt: new Date(),
      status: 'pending'
    }));
    
    setDocuments(prev => [...prev, ...newDocuments]);
  };

  const handleDocumentSelect = (document: Document) => {
    console.log('Selected document:', document);
    // TODO: Implement document viewer
  };

  const handleDocumentDelete = (documentId: string) => {
    setDocuments(prev => prev.filter(doc => doc.id !== documentId));
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Document Management</h1>
        <p className="text-gray-600">Upload and manage your financial aid documents</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Upload Section */}
        <div className="space-y-6">
          <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
            <h2 className="text-xl font-semibold text-gray-800 mb-4">Upload Documents</h2>
            <DocumentUpload onUploadComplete={handleUploadComplete} />
          </div>

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
        </div>

        {/* Document List */}
        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold text-gray-800">Your Documents</h2>
            <span className="text-sm text-gray-500">{documents.length} documents</span>
          </div>
          <DocumentList 
            documents={documents}
            onDocumentSelect={handleDocumentSelect}
            onDocumentDelete={handleDocumentDelete}
          />
        </div>
      </div>
    </div>
  );
};

export default Documents;