import React from 'react';
import type { UseFormRegister, FieldErrors, UseFormSetValue } from 'react-hook-form';
import type { FAFSAData } from '../../../types';

interface ReviewSectionProps {
  register: UseFormRegister<FAFSAData>;
  errors: FieldErrors<FAFSAData>;
  formData: FAFSAData;
  setValue: UseFormSetValue<FAFSAData>;
}

const ReviewSection: React.FC<ReviewSectionProps> = ({
  register,
  errors,
  formData,
}) => {
  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    if (!dateString) return 'Not provided';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  const getMaritalStatusLabel = (status: string) => {
    const statusMap: Record<string, string> = {
      single: 'Single',
      married: 'Married',
      separated: 'Separated',
      divorced: 'Divorced',
      widowed: 'Widowed',
    };
    return statusMap[status] || status;
  };

  const getTaxStatusLabel = (status: string) => {
    const statusMap: Record<string, string> = {
      willFile: 'Will file a tax return',
      alreadyFiled: 'Already filed tax return',
      notRequired: 'Not required to file',
    };
    return statusMap[status] || status;
  };

  const getHousingLabel = (plan: string) => {
    const planMap: Record<string, string> = {
      onCampus: 'On Campus',
      offCampus: 'Off Campus',
      withParents: 'With Parents',
    };
    return planMap[plan] || plan;
  };

  const getEnrollmentLabel = (status: string) => {
    const statusMap: Record<string, string> = {
      fullTime: 'Full Time',
      partTime: 'Part Time',
      lessThanHalf: 'Less Than Half Time',
    };
    return statusMap[status] || status;
  };

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-semibold text-gray-800">Review & Submit</h2>
      
      <div className="bg-green-50 border border-green-200 rounded-lg p-6">
        <h3 className="text-lg font-medium text-green-800 mb-4">
          🎉 Ready to Submit!
        </h3>
        <p className="text-green-600">
          Please review all your information carefully before submitting. 
          Once submitted, you cannot make changes to this application.
        </p>
      </div>

      {/* Student Information Review */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-medium text-gray-700 mb-4">Student Information</h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <h4 className="font-semibold text-gray-600 mb-2">Personal Details</h4>
            <dl className="space-y-2">
              <div>
                <dt className="text-sm text-gray-500">Name</dt>
                <dd className="text-gray-800">
                  {formData.student?.firstName} {formData.student?.lastName}
                </dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">SSN</dt>
                <dd className="text-gray-800">{formData.student?.ssn || 'Not provided'}</dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Date of Birth</dt>
                <dd className="text-gray-800">{formatDate(formData.student?.dateOfBirth || '')}</dd>
              </div>
            </dl>
          </div>

          <div>
            <h4 className="font-semibold text-gray-600 mb-2">Contact Information</h4>
            <dl className="space-y-2">
              <div>
                <dt className="text-sm text-gray-500">Email</dt>
                <dd className="text-gray-800">{formData.student?.email || 'Not provided'}</dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Phone</dt>
                <dd className="text-gray-800">{formData.student?.phone || 'Not provided'}</dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Address</dt>
                <dd className="text-gray-800">
                  {formData.student?.address ? (
                    <>
                      {formData.student.address.street}<br />
                      {formData.student.address.city}, {formData.student.address.state} {formData.student.address.zipCode}
                    </>
                  ) : (
                    'Not provided'
                  )}
                </dd>
              </div>
            </dl>
          </div>
        </div>
      </div>

      {/* Family Information Review */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-medium text-gray-700 mb-4">Family Information</h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <h4 className="font-semibold text-gray-600 mb-2">Family Status</h4>
            <dl className="space-y-2">
              <div>
                <dt className="text-sm text-gray-500">Marital Status</dt>
                <dd className="text-gray-800">
                  {getMaritalStatusLabel(formData.family?.maritalStatus || '')}
                </dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Tax Return Status</dt>
                <dd className="text-gray-800">
                  {getTaxStatusLabel(formData.family?.taxReturnStatus || '')}
                </dd>
              </div>
            </dl>
          </div>

          <div>
            <h4 className="font-semibold text-gray-600 mb-2">Household Information</h4>
            <dl className="space-y-2">
              <div>
                <dt className="text-sm text-gray-500">Household Size</dt>
                <dd className="text-gray-800">{formData.family?.householdSize || 'Not provided'}</dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Number in College</dt>
                <dd className="text-gray-800">
                  {formData.family?.numberOfCollegeStudents || 'Not provided'}
                </dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Parents Information</dt>
                <dd className="text-gray-800">
                  {formData.family?.parents?.length || 0} parent(s) provided
                </dd>
              </div>
            </dl>
          </div>
        </div>
      </div>

      {/* Financial Information Review */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-medium text-gray-700 mb-4">Financial Information</h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <h4 className="font-semibold text-gray-600 mb-2">Income Information</h4>
            <dl className="space-y-2">
              <div>
                <dt className="text-sm text-gray-500">Adjusted Gross Income</dt>
                <dd className="text-gray-800">
                  {formatCurrency(formData.financial?.income?.adjustedGrossIncome || 0)}
                </dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Wages, Salaries, Tips</dt>
                <dd className="text-gray-800">
                  {formatCurrency(formData.financial?.income?.wages || 0)}
                </dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Taxable Interest</dt>
                <dd className="text-gray-800">
                  {formatCurrency(formData.financial?.income?.taxableInterest || 0)}
                </dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Tax-Exempt Interest</dt>
                <dd className="text-gray-800">
                  {formatCurrency(formData.financial?.income?.taxExemptInterest || 0)}
                </dd>
              </div>
            </dl>
          </div>

          <div>
            <h4 className="font-semibold text-gray-600 mb-2">Asset Information</h4>
            <dl className="space-y-2">
              <div>
                <dt className="text-sm text-gray-500">Cash & Savings</dt>
                <dd className="text-gray-800">
                  {formatCurrency(formData.financial?.assets?.cash || 0)}
                </dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Investments</dt>
                <dd className="text-gray-800">
                  {formatCurrency(formData.financial?.assets?.investments || 0)}
                </dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Real Estate Value</dt>
                <dd className="text-gray-800">
                  {formatCurrency(formData.financial?.assets?.realEstate || 0)}
                </dd>
              </div>
            </dl>
          </div>
        </div>

        <div className="mt-6">
          <h4 className="font-semibold text-gray-600 mb-2">Benefits Received</h4>
          <div className="flex space-x-4">
            {formData.financial?.benefits?.receivesSocialSecurity && (
              <span className="bg-blue-100 text-blue-800 px-3 py-1 rounded-full text-sm">
                Social Security
              </span>
            )}
            {formData.financial?.benefits?.receivesVeteransBenefits && (
              <span className="bg-green-100 text-green-800 px-3 py-1 rounded-full text-sm">
                Veterans Benefits
              </span>
            )}
            {formData.financial?.benefits?.receivesChildSupport && (
              <span className="bg-yellow-100 text-yellow-800 px-3 py-1 rounded-full text-sm">
                Child Support
              </span>
            )}
            {!formData.financial?.benefits?.receivesSocialSecurity &&
             !formData.financial?.benefits?.receivesVeteransBenefits &&
             !formData.financial?.benefits?.receivesChildSupport && (
              <span className="text-gray-500 text-sm">No benefits selected</span>
            )}
          </div>
        </div>
      </div>

      {/* School Information Review */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-medium text-gray-700 mb-4">School Selection</h3>
        
        {!formData.schools?.length ? (
          <p className="text-gray-500">No schools selected</p>
        ) : (
          <div className="space-y-4">
            {formData.schools.map((school: any, index: number) => (
              <div key={index} className="border border-gray-200 rounded-lg p-4">
                <h4 className="font-medium text-gray-800 mb-2">
                  {index + 1}. {school.schoolName}
                </h4>
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="text-gray-500">School Code:</span>{' '}
                    <span className="text-gray-800">{school.schoolId}</span>
                  </div>
                  <div>
                    <span className="text-gray-500">Housing:</span>{' '}
                    <span className="text-gray-800">{getHousingLabel(school.housingPlan)}</span>
                  </div>
                  <div>
                    <span className="text-gray-500">Enrollment:</span>{' '}
                    <span className="text-gray-800">{getEnrollmentLabel(school.enrollmentStatus)}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Final Confirmation */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
        <h3 className="text-lg font-medium text-blue-800 mb-4">Final Confirmation</h3>
        
        <div className="flex items-start space-x-3">
          <input
            id="confirmation"
            type="checkbox"
            {...register('confirmation' as any)} // Temporary any type for confirmation field
            className="h-5 w-5 text-primary focus:ring-primary border-gray-300 rounded mt-1"
          />
          <label htmlFor="confirmation" className="text-sm text-blue-700">
            I certify that all information I provided on this form is true and complete to the best of my knowledge. 
            I understand that if I purposely give false or misleading information, I may be fined, be sentenced to jail, 
            or both.
          </label>
        </div>
        
        {errors.confirmation && (
          <p className="text-sm text-red-600 mt-2">{errors.confirmation.message}</p>
        )}

        <div className="mt-4 p-4 bg-white rounded-lg border">
          <h4 className="font-semibold text-gray-700 mb-2">Before you submit:</h4>
          <ul className="text-sm text-gray-600 space-y-1">
            <li>• Review all information for accuracy</li>
            <li>• Ensure you've included all required schools</li>
            <li>• Verify financial information matches your tax records</li>
            <li>• Keep a copy of this application for your records</li>
          </ul>
        </div>
      </div>

      {/* Submission Notes */}
      <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
        <h4 className="font-semibold text-gray-700 mb-2">What happens next?</h4>
        <ul className="text-sm text-gray-600 space-y-1">
          <li>• You'll receive a confirmation email with your FAFSA Submission Summary</li>
          <li>• Schools will receive your information within 3-5 business days</li>
          <li>• Each school will contact you about their financial aid process</li>
          <li>• You can check your application status at fafsa.gov</li>
        </ul>
      </div>
    </div>
  );
};

export default ReviewSection;