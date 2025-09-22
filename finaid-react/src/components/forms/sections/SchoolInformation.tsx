import React, { useState } from 'react';
import type { UseFormRegister, FieldErrors, UseFormSetValue } from 'react-hook-form';
import type { FAFSAData } from '../../../types';

interface SchoolInformationProps {
  register: UseFormRegister<FAFSAData>;
  errors: FieldErrors<FAFSAData>;
  formData: FAFSAData;
  setValue: UseFormSetValue<FAFSAData>;
}

// Mock school data - in real app, this would come from an API
const mockSchools = [
  { id: '1', name: 'Stanford University', state: 'CA', code: '001305' },
  { id: '2', name: 'Harvard University', state: 'MA', code: '002155' },
  { id: '3', name: 'MIT', state: 'MA', code: '002178' },
  { id: '4', name: 'UC Berkeley', state: 'CA', code: '001312' },
  { id: '5', name: 'Yale University', state: 'CT', code: '001426' },
  { id: '6', name: 'Columbia University', state: 'NY', code: '002707' },
  { id: '7', name: 'University of Michigan', state: 'MI', code: '002325' },
  { id: '8', name: 'UCLA', state: 'CA', code: '001315' },
  { id: '9', name: 'Princeton University', state: 'NJ', code: '002620' },
  { id: '10', name: 'University of Texas at Austin', state: 'TX', code: '003658' },
];

const housingOptions = [
  { value: 'onCampus', label: 'On Campus' },
  { value: 'offCampus', label: 'Off Campus' },
  { value: 'withParents', label: 'With Parents' },
];

const enrollmentOptions = [
  { value: 'fullTime', label: 'Full Time' },
  { value: 'partTime', label: 'Part Time' },
  { value: 'lessThanHalf', label: 'Less Than Half Time' },
];

const SchoolInformation: React.FC<SchoolInformationProps> = ({
  errors,
  formData,
  setValue,
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [showSchoolList, setShowSchoolList] = useState(false);

  const filteredSchools = mockSchools.filter((school: any) =>
    school.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    school.state.toLowerCase().includes(searchTerm.toLowerCase()) ||
    school.code.includes(searchTerm)
  );

  const addSchool = (school: any) => {
    const currentSchools = formData.schools || [];
    if (currentSchools.length >= 10) {
      alert('You can only select up to 10 schools.');
      return;
    }

    if (!currentSchools.some((s: any) => s.schoolId === school.id)) {
      const newSchool = {
        schoolId: school.id,
        schoolName: school.name,
        housingPlan: '',
        enrollmentStatus: '',
      };
      
      setValue('schools', [...currentSchools, newSchool], { shouldValidate: true });
    }
    
    setSearchTerm('');
    setShowSchoolList(false);
  };

  const removeSchool = (index: number) => {
    const currentSchools = formData.schools || [];
    const newSchools = currentSchools.filter((_, i) => i !== index);
    setValue('schools', newSchools, { shouldValidate: true });
  };

  const updateSchoolField = (index: number, field: string, value: string) => {
    const currentSchools = formData.schools || [];
    const updatedSchools = currentSchools.map((school, i) =>
      i === index ? { ...school, [field]: value } : school
    );
    setValue('schools', updatedSchools, { shouldValidate: true });
  };

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-semibold text-gray-800">School Selection</h2>
      
      {/* School Search and Selection */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-medium text-gray-700 mb-4">Add Schools</h3>
        
        <div className="relative">
          <label htmlFor="schoolSearch" className="block text-sm font-medium text-gray-700 mb-1">
            Search for Schools *
          </label>
          <input
            id="schoolSearch"
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            onFocus={() => setShowSchoolList(true)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-primary"
            placeholder="Search by school name, state, or code"
          />
          
          {showSchoolList && searchTerm && (
            <div className="absolute z-10 w-full bg-white border border-gray-300 rounded-md shadow-lg max-h-60 overflow-y-auto">
              {filteredSchools.length > 0 ? (
                filteredSchools.map((school: any) => (
                  <div
                    key={school.id}
                    className="px-4 py-2 hover:bg-gray-100 cursor-pointer"
                    onClick={() => addSchool(school)}
                  >
                    <div className="font-medium">{school.name}</div>
                    <div className="text-sm text-gray-600">
                      {school.state} • Code: {school.code}
                    </div>
                  </div>
                ))
              ) : (
                <div className="px-4 py-2 text-gray-500">
                  No schools found matching "{searchTerm}"
                </div>
              )}
            </div>
          )}
        </div>

        <p className="text-sm text-gray-500 mt-2">
          You can select up to 10 schools to receive your FAFSA information.
        </p>
      </div>

      {/* Selected Schools */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-medium text-gray-700 mb-4">
          Selected Schools ({formData.schools?.length || 0}/10)
        </h3>
        
        {!formData.schools?.length ? (
          <div className="text-center py-8 text-gray-500">
            <p>No schools selected yet.</p>
            <p className="text-sm">Use the search above to add schools.</p>
          </div>
        ) : (
          <div className="space-y-4">
            {formData.schools.map((school, index) => (
              <div key={index} className="border border-gray-200 rounded-lg p-4">
                <div className="flex justify-between items-start mb-3">
                  <div>
                    <h4 className="font-medium text-gray-800">{school.schoolName}</h4>
                    <p className="text-sm text-gray-600">School Code: {school.schoolId}</p>
                  </div>
                  <button
                    type="button"
                    onClick={() => removeSchool(index)}
                    className="text-red-600 hover:text-red-800 text-sm"
                  >
                    Remove
                  </button>
                </div>
                
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Housing Plan *
                    </label>
                    <select
                      value={school.housingPlan}
                      onChange={(e) => updateSchoolField(index, 'housingPlan', e.target.value)}
                      className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                        errors.schools?.[index]?.housingPlan ? 'border-red-500' : 'border-gray-300'
                      }`}
                    >
                      <option value="">Select housing plan</option>
                      {housingOptions.map(option => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                    {errors.schools?.[index]?.housingPlan && (
                      <p className="text-sm text-red-600">{errors.schools[index]?.housingPlan?.message}</p>
                    )}
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Enrollment Status *
                    </label>
                    <select
                      value={school.enrollmentStatus}
                      onChange={(e) => updateSchoolField(index, 'enrollmentStatus', e.target.value)}
                      className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                        errors.schools?.[index]?.enrollmentStatus ? 'border-red-500' : 'border-gray-300'
                      }`}
                    >
                      <option value="">Select enrollment status</option>
                      {enrollmentOptions.map(option => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                    {errors.schools?.[index]?.enrollmentStatus && (
                      <p className="text-sm text-red-600">{errors.schools[index]?.enrollmentStatus?.message}</p>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
        
        {errors.schools && typeof errors.schools.message === 'string' && (
          <p className="text-sm text-red-600 mt-2">{errors.schools.message}</p>
        )}
      </div>

      {/* School Information Tips */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h4 className="font-semibold text-blue-800 mb-2">School Selection Guidance</h4>
        
        <ul className="text-blue-600 text-sm space-y-1">
          <li>• <strong>Order matters:</strong> List schools in order of preference</li>
          <li>• <strong>Research schools:</strong> Add all schools you're considering</li>
          <li>• <strong>Housing plans:</strong> Indicate where you plan to live while attending</li>
          <li>• <strong>Enrollment status:</strong> Select your planned enrollment intensity</li>
          <li>• <strong>No limit to applications:</strong> You can apply to more than 10 schools, but FAFSA only sends to 10</li>
        </ul>
      </div>

      {/* Why This Matters */}
      <div className="bg-green-50 border border-green-200 rounded-lg p-4">
        <h4 className="font-semibold text-green-800 mb-2">Why School Selection Matters</h4>
        
        <p className="text-green-600 text-sm">
          The schools you list will receive your FAFSA information and use it to prepare 
          your financial aid package. Each school may offer different types and amounts 
          of aid based on their own policies and available funding. It's important to 
          include all schools you're seriously considering, even if you haven't been 
          accepted yet.
        </p>
      </div>

      {/* Next Steps */}
      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
        <h4 className="font-semibold text-yellow-800 mb-2">After Submitting</h4>
        
        <p className="text-yellow-600 text-sm">
          Once you submit your FAFSA, each school will receive your information and 
          begin preparing your financial aid offer. You may need to provide additional 
          documentation directly to the schools. Monitor your email and student portals 
          for communications from each institution.
        </p>
      </div>
    </div>
  );
};

export default SchoolInformation;