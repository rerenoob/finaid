import React from 'react';

interface Step {
  id: string;
  label: string;
  completed: boolean;
  current?: boolean;
}

interface StepIndicatorProps {
  steps: Step[];
  onStepClick?: (stepId: string) => void;
}

export default function StepIndicator({ steps, onStepClick }: StepIndicatorProps) {
  return (
    <div className="flex items-center justify-between">
      {steps.map((step, index) => (
        <React.Fragment key={step.id}>
          {/* Step */}
          <div className="flex flex-col items-center">
            <button
              onClick={() => onStepClick?.(step.id)}
              className={`flex items-center justify-center w-10 h-10 rounded-full border-2 transition-all ${
                step.completed
                  ? 'bg-green-500 border-green-500 text-white'
                  : step.current
                  ? 'bg-blue-600 border-blue-600 text-white'
                  : 'bg-white border-gray-300 text-gray-400'
              } ${onStepClick ? 'cursor-pointer hover:scale-110' : 'cursor-default'}`}
            >
              {step.completed ? (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
              ) : (
                <span className="font-medium">{index + 1}</span>
              )}
            </button>
            <span className={`mt-2 text-xs font-medium text-center max-w-20 ${
              step.current ? 'text-blue-600' : step.completed ? 'text-green-600' : 'text-gray-500'
            }`}>
              {step.label}
            </span>
          </div>

          {/* Connector */}
          {index < steps.length - 1 && (
            <div 
              className={`flex-1 h-0.5 mx-2 ${
                step.completed ? 'bg-green-500' : 'bg-gray-300'
              }`}
            />
          )}
        </React.Fragment>
      ))}
    </div>
  );
}