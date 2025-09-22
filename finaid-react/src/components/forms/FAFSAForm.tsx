import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useFAFSAData, useSubmitFAFSA } from '../../hooks/useFAFSA';
import LoadingSpinner from '../common/LoadingSpinner';
import StudentInformation from './sections/StudentInformation';
import FamilyInformation from './sections/FamilyInformation';
import FinancialInformation from './sections/FinancialInformation';
import SchoolInformation from './sections/SchoolInformation';
import ReviewSection from './sections/ReviewSection';

// FAFSA form schema based on .NET models
const fafsaSchema = z.object({
  student: z.object({
    firstName: z.string().min(1, 'First name is required'),
    lastName: z.string().min(1, 'Last name is required'),
    ssn: z.string().regex(/^\d{3}-\d{2}-\d{4}$/, 'Invalid SSN format'),
    dateOfBirth: z.string().min(1, 'Date of birth is required'),
    email: z.string().email('Invalid email address'),
    phone: z.string().min(10, 'Phone number is required'),
    address: z.object({
      street: z.string().min(1, 'Street address is required'),
      city: z.string().min(1, 'City is required'),
      state: z.string().min(2, 'State is required'),
      zipCode: z.string().regex(/^\d{5}(-\d{4})?$/, 'Invalid ZIP code'),
    }),
  }),
  family: z.object({
    maritalStatus: z.string().min(1, 'Marital status is required'),
    taxReturnStatus: z.string().min(1, 'Tax return status is required'),
    householdSize: z.number().min(1, 'Household size must be at least 1'),
    numberOfCollegeStudents: z.number().min(0, 'Number of college students cannot be negative'),
    parents: z.array(z.object({
      firstName: z.string().min(1, 'Parent first name is required'),
      lastName: z.string().min(1, 'Parent last name is required'),
      ssn: z.string().regex(/^\d{3}-\d{2}-\d{4}$/, 'Invalid SSN format'),
      dateOfBirth: z.string().min(1, 'Date of birth is required'),
      employmentStatus: z.string().min(1, 'Employment status is required'),
    })).optional(),
  }),
  financial: z.object({
    income: z.object({
      adjustedGrossIncome: z.number().min(0, 'Income cannot be negative'),
      wages: z.number().min(0, 'Wages cannot be negative'),
      taxableInterest: z.number().min(0, 'Taxable interest cannot be negative'),
      taxExemptInterest: z.number().min(0, 'Tax-exempt interest cannot be negative'),
    }),
    assets: z.object({
      cash: z.number().min(0, 'Cash assets cannot be negative'),
      investments: z.number().min(0, 'Investments cannot be negative'),
      realEstate: z.number().min(0, 'Real estate value cannot be negative'),
    }),
    benefits: z.object({
      receivesSocialSecurity: z.boolean(),
      receivesVeteransBenefits: z.boolean(),
      receivesChildSupport: z.boolean(),
    }),
  }),
  schools: z.array(z.object({
    schoolId: z.string().min(1, 'School ID is required'),
    schoolName: z.string().min(1, 'School name is required'),
    housingPlan: z.string().min(1, 'Housing plan is required'),
    enrollmentStatus: z.string().min(1, 'Enrollment status is required'),
  })).min(1, 'At least one school is required'),
});

type FAFSAFormData = z.infer<typeof fafsaSchema>;

interface FAFSAFormProps {
  onSectionChange?: (section: string) => void;
}

const FAFSAForm: React.FC<FAFSAFormProps> = ({ onSectionChange }) => {
  const [currentStep, setCurrentStep] = useState(0);
  const { data: existingData, isLoading, error } = useFAFSAData();
  const { mutate: submitFAFSA, isPending: isSubmitting } = useSubmitFAFSA();
  
  const {
    register,
    handleSubmit,
    formState: { errors, isValid },
    watch,
    setValue,
    trigger,
  } = useForm<FAFSAFormData>({
    resolver: zodResolver(fafsaSchema),
    defaultValues: existingData,
    mode: 'onChange',
  });

  const formData = watch();

  const steps = [
    { title: 'Student Information', component: StudentInformation },
    { title: 'Family Information', component: FamilyInformation },
    { title: 'Financial Information', component: FinancialInformation },
    { title: 'School Selection', component: SchoolInformation },
    { title: 'Review & Submit', component: ReviewSection },
  ];

  const CurrentStepComponent = steps[currentStep].component;

  const handleNext = async () => {
    const isStepValid = await trigger();
    if (isStepValid) {
      const nextStep = Math.min(currentStep + 1, steps.length - 1);
      setCurrentStep(nextStep);
      onSectionChange?.(steps[nextStep].title.toLowerCase().replace(/\s+/g, '-'));
    }
  };

  const handlePrevious = () => {
    const prevStep = Math.max(currentStep - 1, 0);
    setCurrentStep(prevStep);
    onSectionChange?.(steps[prevStep].title.toLowerCase().replace(/\s+/g, '-'));
  };

  const onSubmit = (data: FAFSAFormData) => {
    submitFAFSA(data, {
      onSuccess: () => {
        // Handle successful submission
        console.log('FAFSA submitted successfully');
      },
      onError: (error) => {
        // Handle submission error
        console.error('FAFSA submission failed:', error);
      },
    });
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-64">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-6">
        <h2 className="text-red-800 font-semibold text-lg mb-2">Error Loading FAFSA Data</h2>
        <p className="text-red-600">Please try again later or contact support.</p>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto">
      {/* Progress Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-4">FAFSA Application</h1>
        
        {/* Step Progress */}
        <div className="flex justify-between mb-6">
          {steps.map((step, index) => (
            <div key={step.title} className="flex flex-col items-center">
              <div
                className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold ${
                  index === currentStep
                    ? 'bg-primary text-white'
                    : index < currentStep
                    ? 'bg-green-500 text-white'
                    : 'bg-gray-200 text-gray-600'
                }`}
              >
                {index + 1}
              </div>
              <span
                className={`text-xs mt-2 ${
                  index === currentStep ? 'text-primary font-semibold' : 'text-gray-500'
                }`}
              >
                {step.title}
              </span>
            </div>
          ))}
        </div>

        {/* Progress Bar */}
        <div className="w-full bg-gray-200 rounded-full h-2">
          <div
            className="bg-primary h-2 rounded-full transition-all duration-300"
            style={{
              width: `${((currentStep + 1) / steps.length) * 100}%`,
            }}
          />
        </div>
      </div>

      {/* Form Content */}
      <form onSubmit={handleSubmit(onSubmit)} className="bg-white rounded-lg shadow-md p-6">
        <CurrentStepComponent
          register={register}
          errors={errors}
          formData={formData}
          setValue={setValue}
        />

        {/* Navigation Buttons */}
        <div className="flex justify-between mt-8 pt-6 border-t border-gray-200">
          <button
            type="button"
            onClick={handlePrevious}
            disabled={currentStep === 0}
            className="px-6 py-2 bg-gray-500 text-white rounded-md disabled:bg-gray-300 disabled:cursor-not-allowed hover:bg-gray-600 transition-colors"
          >
            Previous
          </button>

          {currentStep < steps.length - 1 ? (
            <button
              type="button"
              onClick={handleNext}
              className="px-6 py-2 bg-primary text-white rounded-md hover:bg-primary-dark transition-colors"
            >
              Next
            </button>
          ) : (
            <button
              type="submit"
              disabled={!isValid || isSubmitting}
              className="px-6 py-2 bg-green-600 text-white rounded-md disabled:bg-green-300 disabled:cursor-not-allowed hover:bg-green-700 transition-colors"
            >
              {isSubmitting ? 'Submitting...' : 'Submit Application'}
            </button>
          )}
        </div>
      </form>

      {/* Save Status */}
      <div className="mt-4 text-sm text-gray-500 text-center">
        Your progress is automatically saved as you complete each section.
      </div>
    </div>
  );
};

export default FAFSAForm;