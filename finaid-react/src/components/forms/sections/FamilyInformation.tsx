import React, { useState } from 'react';
import type { UseFormRegister, FieldErrors, UseFormSetValue } from 'react-hook-form';
import type { FAFSAData } from '../../../types';

interface FamilyInformationProps {
  register: UseFormRegister<FAFSAData>;
  errors: FieldErrors<FAFSAData>;
  formData: FAFSAData;
  setValue: UseFormSetValue<FAFSAData>;
}

const FamilyInformation: React.FC<FamilyInformationProps> = ({
  register,
  errors,
  formData,
  setValue,
}) => {
  const [numberOfParents, setNumberOfParents] = useState(
    formData.family?.parents?.length || 1
  );

  const maritalStatusOptions = [
    { value: 'single', label: 'Single' },
    { value: 'married', label: 'Married' },
    { value: 'separated', label: 'Separated' },
    { value: 'divorced', label: 'Divorced' },
    { value: 'widowed', label: 'Widowed' },
  ];

  const taxReturnStatusOptions = [
    { value: 'willFile', label: 'Will file a tax return' },
    { value: 'alreadyFiled', label: 'Already filed tax return' },
    { value: 'notRequired', label: 'Not required to file' },
  ];

  const employmentStatusOptions = [
    { value: 'employed', label: 'Employed' },
    { value: 'unemployed', label: 'Unemployed' },
    { value: 'retired', label: 'Retired' },
    { value: 'disabled', label: 'Disabled' },
    { value: 'homemaker', label: 'Homemaker' },
  ];

  const handleParentCountChange = (count: number) => {
    setNumberOfParents(count);
    if (count === 0) {
      setValue('family.parents', undefined, { shouldValidate: true });
    } else {
      const currentParents = formData.family?.parents || [];
      const newParents = Array(count).fill(null).map((_, index) => ({
        ...currentParents[index],
        firstName: currentParents[index]?.firstName || '',
        lastName: currentParents[index]?.lastName || '',
        ssn: currentParents[index]?.ssn || '',
        dateOfBirth: currentParents[index]?.dateOfBirth || '',
        employmentStatus: currentParents[index]?.employmentStatus || '',
      }));
      setValue('family.parents', newParents, { shouldValidate: true });
    }
  };

  const handleSSNChange = (index: number, e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value.replace(/\D/g, '');
    if (value.length <= 9) {
      let formattedValue = value;
      if (value.length > 3) {
        formattedValue = `${value.slice(0, 3)}-${value.slice(3)}`;
      }
      if (value.length > 5) {
        formattedValue = `${value.slice(0, 3)}-${value.slice(3, 5)}-${value.slice(5)}`;
      }
      setValue(`family.parents.${index}.ssn` as const, formattedValue, { shouldValidate: true });
    }
  };

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-semibold text-gray-800">Family Information</h2>
      
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Family Status */}
        <div className="space-y-4">
          <h3 className="text-lg font-medium text-gray-700">Family Status</h3>
          
          <div>
            <label htmlFor="maritalStatus" className="block text-sm font-medium text-gray-700 mb-1">
              Marital Status *
            </label>
            <select
              id="maritalStatus"
              {...register('family.maritalStatus')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.family?.maritalStatus ? 'border-red-500' : 'border-gray-300'
              }`}
            >
              <option value="">Select marital status</option>
              {maritalStatusOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            {errors.family?.maritalStatus && (
              <p className="mt-1 text-sm text-red-600">{errors.family.maritalStatus.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="taxReturnStatus" className="block text-sm font-medium text-gray-700 mb-1">
              Tax Return Status *
            </label>
            <select
              id="taxReturnStatus"
              {...register('family.taxReturnStatus')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.family?.taxReturnStatus ? 'border-red-500' : 'border-gray-300'
              }`}
            >
              <option value="">Select tax return status</option>
              {taxReturnStatusOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            {errors.family?.taxReturnStatus && (
              <p className="mt-1 text-sm text-red-600">{errors.family.taxReturnStatus.message}</p>
            )}
          </div>
        </div>

        {/* Household Information */}
        <div className="space-y-4">
          <h3 className="text-lg font-medium text-gray-700">Household Information</h3>
          
          <div>
            <label htmlFor="householdSize" className="block text-sm font-medium text-gray-700 mb-1">
              Household Size *
            </label>
            <input
              id="householdSize"
              type="number"
              min="1"
              {...register('family.householdSize', { valueAsNumber: true })}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.family?.householdSize ? 'border-red-500' : 'border-gray-300'
              }`}
            />
            {errors.family?.householdSize && (
              <p className="mt-1 text-sm text-red-600">{errors.family.householdSize.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="numberOfCollegeStudents" className="block text-sm font-medium text-gray-700 mb-1">
              Number in College *
            </label>
            <input
              id="numberOfCollegeStudents"
              type="number"
              min="0"
              {...register('family.numberOfCollegeStudents', { valueAsNumber: true })}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.family?.numberOfCollegeStudents ? 'border-red-500' : 'border-gray-300'
              }`}
            />
            {errors.family?.numberOfCollegeStudents && (
              <p className="mt-1 text-sm text-red-600">{errors.family.numberOfCollegeStudents.message}</p>
            )}
          </div>
        </div>
      </div>

      {/* Parent Information */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-medium text-gray-700">Parent Information</h3>
          <div className="flex space-x-2">
            <button
              type="button"
              onClick={() => handleParentCountChange(0)}
              className="px-3 py-1 text-sm bg-gray-500 text-white rounded hover:bg-gray-600"
            >
              No Parents
            </button>
            <button
              type="button"
              onClick={() => handleParentCountChange(1)}
              className="px-3 py-1 text-sm bg-blue-500 text-white rounded hover:bg-blue-600"
            >
              One Parent
            </button>
            <button
              type="button"
              onClick={() => handleParentCountChange(2)}
              className="px-3 py-1 text-sm bg-green-500 text-white rounded hover:bg-green-600"
            >
              Two Parents
            </button>
          </div>
        </div>

        {numberOfParents > 0 && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {Array.from({ length: numberOfParents }, (_, index) => (
              <div key={index} className="bg-gray-50 p-4 rounded-lg">
                <h4 className="font-medium text-gray-700 mb-3">
                  Parent {index + 1}
                </h4>
                
                <div className="space-y-3">
                  <div>
                    <input
                      {...register(`family.parents.${index}.firstName` as const)}
                      className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                        errors.family?.parents?.[index]?.firstName ? 'border-red-500' : 'border-gray-300'
                      }`}
                      placeholder="First Name"
                    />
                    {errors.family?.parents?.[index]?.firstName && (
                      <p className="text-sm text-red-600">{errors.family.parents[index]?.firstName?.message}</p>
                    )}
                  </div>

                  <div>
                    <input
                      {...register(`family.parents.${index}.lastName` as const)}
                      className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                        errors.family?.parents?.[index]?.lastName ? 'border-red-500' : 'border-gray-300'
                      }`}
                      placeholder="Last Name"
                    />
                    {errors.family?.parents?.[index]?.lastName && (
                      <p className="text-sm text-red-600">{errors.family.parents[index]?.lastName?.message}</p>
                    )}
                  </div>

                  <div>
                    <input
                      {...register(`family.parents.${index}.ssn` as const)}
                      onChange={(e) => handleSSNChange(index, e)}
                      className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                        errors.family?.parents?.[index]?.ssn ? 'border-red-500' : 'border-gray-300'
                      }`}
                      placeholder="SSN (XXX-XX-XXXX)"
                      maxLength={11}
                    />
                    {errors.family?.parents?.[index]?.ssn && (
                      <p className="text-sm text-red-600">{errors.family.parents[index]?.ssn?.message}</p>
                    )}
                  </div>

                  <div>
                    <input
                      type="date"
                      {...register(`family.parents.${index}.dateOfBirth` as const)}
                      className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                        errors.family?.parents?.[index]?.dateOfBirth ? 'border-red-500' : 'border-gray-300'
                      }`}
                    />
                    {errors.family?.parents?.[index]?.dateOfBirth && (
                      <p className="text-sm text-red-600">{errors.family.parents[index]?.dateOfBirth?.message}</p>
                    )}
                  </div>

                  <div>
                    <select
                      {...register(`family.parents.${index}.employmentStatus` as const)}
                      className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                        errors.family?.parents?.[index]?.employmentStatus ? 'border-red-500' : 'border-gray-300'
                      }`}
                    >
                      <option value="">Employment Status</option>
                      {employmentStatusOptions.map(option => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                    {errors.family?.parents?.[index]?.employmentStatus && (
                      <p className="text-sm text-red-600">{errors.family.parents[index]?.employmentStatus?.message}</p>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Help Text */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h4 className="font-semibold text-blue-800 mb-2">Family Information Guidance</h4>
        <p className="text-blue-600 text-sm">
          Provide accurate information about your family situation. This helps determine 
          your Expected Family Contribution (EFC) and eligibility for need-based aid.
          Include all family members who live with you and receive more than half their 
          support from you/your parents.
        </p>
      </div>
    </div>
  );
};

export default FamilyInformation;