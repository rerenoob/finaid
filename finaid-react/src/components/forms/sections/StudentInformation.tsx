import React from 'react';
import type { UseFormRegister, FieldErrors, UseFormSetValue } from 'react-hook-form';
import type { FAFSAData } from '../../../types';

interface StudentInformationProps {
  register: UseFormRegister<FAFSAData>;
  errors: FieldErrors<FAFSAData>;
  formData: FAFSAData;
  setValue: UseFormSetValue<FAFSAData>;
}

const StudentInformation: React.FC<StudentInformationProps> = ({
  register,
  errors,
  setValue,
}) => {
  const handleSSNChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value.replace(/\D/g, '');
    if (value.length <= 9) {
      let formattedValue = value;
      if (value.length > 3) {
        formattedValue = `${value.slice(0, 3)}-${value.slice(3)}`;
      }
      if (value.length > 5) {
        formattedValue = `${value.slice(0, 3)}-${value.slice(3, 5)}-${value.slice(5)}`;
      }
      setValue('student.ssn', formattedValue, { shouldValidate: true });
    }
  };

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-semibold text-gray-800">Student Information</h2>
      
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Personal Information */}
        <div className="space-y-4">
          <h3 className="text-lg font-medium text-gray-700">Personal Details</h3>
          
          <div>
            <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 mb-1">
              First Name *
            </label>
            <input
              id="firstName"
              {...register('student.firstName')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.student?.firstName ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="Enter your first name"
            />
            {errors.student?.firstName && (
              <p className="mt-1 text-sm text-red-600">{errors.student.firstName.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 mb-1">
              Last Name *
            </label>
            <input
              id="lastName"
              {...register('student.lastName')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.student?.lastName ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="Enter your last name"
            />
            {errors.student?.lastName && (
              <p className="mt-1 text-sm text-red-600">{errors.student.lastName.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="ssn" className="block text-sm font-medium text-gray-700 mb-1">
              Social Security Number *
            </label>
            <input
              id="ssn"
              {...register('student.ssn')}
              onChange={handleSSNChange}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.student?.ssn ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="XXX-XX-XXXX"
              maxLength={11}
            />
            {errors.student?.ssn && (
              <p className="mt-1 text-sm text-red-600">{errors.student.ssn.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="dateOfBirth" className="block text-sm font-medium text-gray-700 mb-1">
              Date of Birth *
            </label>
            <input
              id="dateOfBirth"
              type="date"
              {...register('student.dateOfBirth')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.student?.dateOfBirth ? 'border-red-500' : 'border-gray-300'
              }`}
            />
            {errors.student?.dateOfBirth && (
              <p className="mt-1 text-sm text-red-600">{errors.student.dateOfBirth.message}</p>
            )}
          </div>
        </div>

        {/* Contact Information */}
        <div className="space-y-4">
          <h3 className="text-lg font-medium text-gray-700">Contact Information</h3>
          
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
              Email Address *
            </label>
            <input
              id="email"
              type="email"
              {...register('student.email')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.student?.email ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="your.email@example.com"
            />
            {errors.student?.email && (
              <p className="mt-1 text-sm text-red-600">{errors.student.email.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-1">
              Phone Number *
            </label>
            <input
              id="phone"
              type="tel"
              {...register('student.phone')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.student?.phone ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="(555) 123-4567"
            />
            {errors.student?.phone && (
              <p className="mt-1 text-sm text-red-600">{errors.student.phone.message}</p>
            )}
          </div>

          {/* Address Fields */}
          <div className="space-y-2">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Address *
            </label>
            
            <input
              {...register('student.address.street')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.student?.address?.street ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="Street address"
            />
            {errors.student?.address?.street && (
              <p className="text-sm text-red-600">{errors.student.address.street.message}</p>
            )}

            <div className="grid grid-cols-2 gap-2">
              <div>
                <input
                  {...register('student.address.city')}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                    errors.student?.address?.city ? 'border-red-500' : 'border-gray-300'
                  }`}
                  placeholder="City"
                />
                {errors.student?.address?.city && (
                  <p className="text-sm text-red-600">{errors.student.address.city.message}</p>
                )}
              </div>
              
              <div>
                <input
                  {...register('student.address.state')}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                    errors.student?.address?.state ? 'border-red-500' : 'border-gray-300'
                  }`}
                  placeholder="State"
                  maxLength={2}
                />
                {errors.student?.address?.state && (
                  <p className="text-sm text-red-600">{errors.student.address.state.message}</p>
                )}
              </div>
            </div>

            <input
              {...register('student.address.zipCode')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.student?.address?.zipCode ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="ZIP Code"
            />
            {errors.student?.address?.zipCode && (
              <p className="text-sm text-red-600">{errors.student.address.zipCode.message}</p>
            )}
          </div>
        </div>
      </div>

      {/* Help Text */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h4 className="font-semibold text-blue-800 mb-2">Important Information</h4>
        <p className="text-blue-600 text-sm">
          Your Social Security Number is required for FAFSA processing. This information 
          is used to verify your identity and determine your eligibility for federal student aid.
        </p>
      </div>
    </div>
  );
};

export default StudentInformation;