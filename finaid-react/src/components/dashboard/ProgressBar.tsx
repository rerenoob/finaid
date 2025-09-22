interface ProgressBarProps {
  progress: number; // 0-100
  label?: string;
  size?: "sm" | "md" | "lg";
  showPercentage?: boolean;
}

export default function ProgressBar({ 
  progress, 
  label, 
  size = "md", 
  showPercentage = true 
}: ProgressBarProps) {
  const sizeClasses = {
    sm: "h-2",
    md: "h-3",
    lg: "h-4"
  };

  const percentageClasses = {
    sm: "text-xs",
    md: "text-sm",
    lg: "text-base"
  };

  return (
    <div className="space-y-2">
      {(label || showPercentage) && (
        <div className="flex justify-between items-center">
          {label && <span className="text-sm font-medium text-gray-700">{label}</span>}
          {showPercentage && (
            <span className={`font-medium text-gray-600 ${percentageClasses[size]}`}>
              {Math.round(progress)}%
            </span>
          )}
        </div>
      )}
      
      <div className={`w-full bg-gray-200 rounded-full ${sizeClasses[size]}`}>
        <div 
          className={`bg-blue-600 h-full rounded-full transition-all duration-500 ease-out ${
            progress === 100 ? "bg-green-600" : ""
          }`}
          style={{ width: `${progress}%` }}
        ></div>
      </div>
    </div>
  );
}
