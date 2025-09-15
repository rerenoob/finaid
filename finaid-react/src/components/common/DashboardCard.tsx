import React from 'react';

interface DashboardCardProps {
  title: string;
  children: React.ReactNode;
  className?: string;
}

const DashboardCard: React.FC<DashboardCardProps> = ({ 
  title, 
  children, 
  className = '' 
}) => {
  return (
    <div className={`bg-white rounded-lg shadow-md p-6 border border-gray-200 ${className}`}>
      <h3 className="text-lg font-semibold text-gray-800 mb-4">
        {title}
      </h3>
      {children}
    </div>
  );
};

export default DashboardCard;