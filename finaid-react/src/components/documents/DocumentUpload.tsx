import React, { useState, useRef } from 'react';

interface UploadProgress {
  file: File;
  progress: number;
  status: 'uploading' | 'completed' | 'error';
  error?: string;
}

interface DocumentUploadProps {
  onUploadComplete?: (files: File[]) => void;
  maxFileSize?: number; // in bytes
  acceptedFileTypes?: string[];
}

export default function DocumentUpload({ 
  onUploadComplete, 
  maxFileSize = 50 * 1024 * 1024, // 50MB default
  acceptedFileTypes = ['.pdf', '.jpg', '.jpeg', '.png', '.tiff'] 
}: DocumentUploadProps) {
  const [uploads, setUploads] = useState<UploadProgress[]>([]);
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const validateFile = (file: File): string | null => {
    if (file.size > maxFileSize) {
      return `File size exceeds ${maxFileSize / (1024 * 1024)}MB limit`;
    }
    
    const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!acceptedFileTypes.includes(fileExtension || '')) {
      return `File type not supported. Accepted types: ${acceptedFileTypes.join(', ')}`;
    }
    
    return null;
  };

  const handleFiles = (files: FileList) => {
    const newUploads: UploadProgress[] = [];
    
    Array.from(files).forEach(file => {
      const error = validateFile(file);
      if (error) {
        newUploads.push({
          file,
          progress: 0,
          status: 'error',
          error
        });
      } else {
        newUploads.push({
          file,
          progress: 0,
          status: 'uploading'
        });
      }
    });

    setUploads(prev => [...prev, ...newUploads]);

    // Simulate upload progress
    newUploads.forEach((upload, index) => {
      if (upload.status === 'uploading') {
        simulateUpload(index + uploads.length);
      }
    });
  };

  const simulateUpload = (uploadIndex: number) => {
    let progress = 0;
    const interval = setInterval(() => {
      progress += Math.random() * 10;
      if (progress >= 100) {
        progress = 100;
        clearInterval(interval);
        
        setUploads(prev => prev.map((upload, index) => 
          index === uploadIndex ? { ...upload, progress: 100, status: 'completed' } : upload
        ));
        
        // Call completion callback
        setTimeout(() => {
          const completedFiles = uploads.filter(u => u.status === 'completed').map(u => u.file);
          if (completedFiles.length > 0 && onUploadComplete) {
            onUploadComplete(completedFiles);
          }
        }, 500);
      } else {
        setUploads(prev => prev.map((upload, index) => 
          index === uploadIndex ? { ...upload, progress } : upload
        ));
      }
    }, 200);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    handleFiles(e.dataTransfer.files);
  };

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      handleFiles(e.target.files);
    }
  };

  const removeUpload = (index: number) => {
    setUploads(prev => prev.filter((_, i) => i !== index));
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <div className="space-y-4">
      {/* Drop Zone */}
      <div
        className={`border-2 border-dashed rounded-lg p-8 text-center transition-colors ${
          isDragging 
            ? 'border-blue-400 bg-blue-50' 
            : 'border-gray-300 bg-gray-50 hover:border-gray-400'
        }`}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
      >
        <div className="text-gray-600">
          <svg className="mx-auto h-12 w-12 mb-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
          </svg>
          <p className="text-lg font-medium mb-2">Drop files here or click to upload</p>
          <p className="text-sm mb-4">
            Supported formats: {acceptedFileTypes.join(', ')} (Max {maxFileSize / (1024 * 1024)}MB)
          </p>
          <button
            type="button"
            onClick={() => fileInputRef.current?.click()}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors"
          >
            Select Files
          </button>
        </div>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept={acceptedFileTypes.join(',')}
          onChange={handleFileInput}
          className="hidden"
        />
      </div>

      {/* Upload Progress */}
      {uploads.length > 0 && (
        <div className="space-y-2">
          <h3 className="font-medium text-gray-900">Upload Progress</h3>
          {uploads.map((upload, index) => (
            <div key={index} className="flex items-center justify-between p-3 bg-white border rounded-lg">
              <div className="flex-1 min-w-0">
                <div className="flex items-center space-x-3">
                  <div className="flex-shrink-0">
                    {upload.status === 'completed' ? (
                      <div className="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center">
                        <svg className="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                        </svg>
                      </div>
                    ) : upload.status === 'error' ? (
                      <div className="w-8 h-8 bg-red-100 rounded-full flex items-center justify-center">
                        <svg className="w-4 h-4 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </div>
                    ) : (
                      <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
                        <svg className="w-4 h-4 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                        </svg>
                      </div>
                    )}
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium text-gray-900 truncate">{upload.file.name}</p>
                    <p className="text-sm text-gray-500">{formatFileSize(upload.file.size)}</p>
                    {upload.error && (
                      <p className="text-sm text-red-600">{upload.error}</p>
                    )}
                  </div>
                </div>
                
                {upload.status === 'uploading' && (
                  <div className="mt-2">
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div 
                        className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                        style={{ width: `${upload.progress}%` }}
                      ></div>
                    </div>
                    <p className="text-xs text-gray-500 mt-1">{Math.round(upload.progress)}%</p>
                  </div>
                )}
              </div>
              
              <button
                onClick={() => removeUpload(index)}
                className="ml-4 text-gray-400 hover:text-gray-600 transition-colors"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}