import React from 'react';
import type { UseFormRegister, FieldErrors, UseFormSetValue } from 'react-hook-form';
import type { FAFSAData } from '../../../types';

interface FinancialInformationProps {
  register: UseFormRegister<FAFSAData>;
  errors: FieldErrors<FAFSAData>;
  formData: FAFSAData;
  setValue: UseFormSetValue<FAFSAData>;
}

const FinancialInformation: React.FC<FinancialInformationProps> = ({
  register,
  errors,
  formData,
  setValue,
}) => {
  const formatCurrency = (value: string) => {
    // Remove non-numeric characters
    const numericValue = value.replace(/[^0-9.]/g, '');
    
    // Format as currency if it's a valid number
    if (numericValue && !isNaN(parseFloat(numericValue))) {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0,
      }).format(parseFloat(numericValue));
    }
    return value;
  };

  const handleCurrencyChange = (
    field: keyof FAFSAData['financial']['income'] | 
           keyof FAFSAData['financial']['assets'],
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const value = e.target.value;
    const numericValue = value.replace(/[^0-9.]/g, '');
    
    if (numericValue === '' || !isNaN(parseFloat(numericValue))) {
      const fieldPath = field in formData.financial.income 
        ? `financial.income.${String(field)}`
        : `financial.assets.${String(field)}`;
      
      setValue(fieldPath as any, numericValue === '' ? 0 : parseFloat(numericValue), {
        shouldValidate: true,
      });
    }
  };

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-semibold text-gray-800">Financial Information</h2>
      
      {/* Income Information */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-medium text-gray-700 mb-4">Income Information</h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label htmlFor="adjustedGrossIncome" className="block text-sm font-medium text-gray-700 mb-1">
              Adjusted Gross Income *
            </label>
            <input
              id="adjustedGrossIncome"
              type="text"
              inputMode="numeric"
              {...register('financial.income.adjustedGrossIncome', { valueAsNumber: true })}
              onChange={(e) => handleCurrencyChange('adjustedGrossIncome', e)}
              value={formatCurrency(formData.financial?.income?.adjustedGrossIncome?.toString() || '')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.financial?.income?.adjustedGrossIncome ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="$0"
            />
            {errors.financial?.income?.adjustedGrossIncome && (
              <p className="mt-1 text-sm text-red-600">{errors.financial.income.adjustedGrossIncome.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="wages" className="block text-sm font-medium text-gray-700 mb-1">
              Wages, Salaries, Tips *
            </label>
            <input
              id="wages"
              type="text"
              inputMode="numeric"
              {...register('financial.income.wages', { valueAsNumber: true })}
              onChange={(e) => handleCurrencyChange('wages', e)}
              value={formatCurrency(formData.financial?.income?.wages?.toString() || '')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.financial?.income?.wages ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="$0"
            />
            {errors.financial?.income?.wages && (
              <p className="mt-1 text-sm text-red-600">{errors.financial.income.wages.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="taxableInterest" className="block text-sm font-medium text-gray-700 mb-1">
              Taxable Interest *
            </label>
            <input
              id="taxableInterest"
              type="text"
              inputMode="numeric"
              {...register('financial.income.taxableInterest', { valueAsNumber: true })}
              onChange={(e) => handleCurrencyChange('taxableInterest', e)}
              value={formatCurrency(formData.financial?.income?.taxableInterest?.toString() || '')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.financial?.income?.taxableInterest ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="$0"
            />
            {errors.financial?.income?.taxableInterest && (
              <p className="mt-1 text-sm text-red-600">{errors.financial.income.taxableInterest.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="taxExemptInterest" className="block text-sm font-medium text-gray-700 mb-1">
              Tax-Exempt Interest *
            </label>
            <input
              id="taxExemptInterest"
              type="text"
              inputMode="numeric"
              {...register('financial.income.taxExemptInterest', { valueAsNumber: true })}
              onChange={(e) => handleCurrencyChange('taxExemptInterest', e)}
              value={formatCurrency(formData.financial?.income?.taxExemptInterest?.toString() || '')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.financial?.income?.taxExemptInterest ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="$0"
            />
            {errors.financial?.income?.taxExemptInterest && (
              <p className="mt-1 text-sm text-red-600">{errors.financial.income.taxExemptInterest.message}</p>
            )}
          </div>
        </div>
      </div>

      {/* Asset Information */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-medium text-gray-700 mb-4">Asset Information</h3>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div>
            <label htmlFor="cash" className="block text-sm font-medium text-gray-700 mb-1">
              Cash & Savings *
            </label>
            <input
              id="cash"
              type="text"
              inputMode="numeric"
              {...register('financial.assets.cash', { valueAsNumber: true })}
              onChange={(e) => handleCurrencyChange('cash', e)}
              value={formatCurrency(formData.financial?.assets?.cash?.toString() || '')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.financial?.assets?.cash ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="$0"
            />
            {errors.financial?.assets?.cash && (
              <p className="mt-1 text-sm text-red-600">{errors.financial.assets.cash.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="investments" className="block text-sm font-medium text-gray-700 mb-1">
              Investments *
            </label>
            <input
              id="investments"
              type="text"
              inputMode="numeric"
              {...register('financial.assets.investments', { valueAsNumber: true })}
              onChange={(e) => handleCurrencyChange('investments', e)}
              value={formatCurrency(formData.financial?.assets?.investments?.toString() || '')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.financial?.assets?.investments ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="$0"
            />
            {errors.financial?.assets?.investments && (
              <p className="mt-1 text-sm text-red-600">{errors.financial.assets.investments.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="realEstate" className="block text-sm font-medium text-gray-700 mb-1">
              Real Estate Value *
            </label>
            <input
              id="realEstate"
              type="text"
              inputMode="numeric"
              {...register('financial.assets.realEstate', { valueAsNumber: true })}
              onChange={(e) => handleCurrencyChange('realEstate', e)}
              value={formatCurrency(formData.financial?.assets?.realEstate?.toString() || '')}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-primary ${
                errors.financial?.assets?.realEstate ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="$0"
            />
            {errors.financial?.assets?.realEstate && (
              <p className="mt-1 text-sm text-red-600">{errors.financial.assets.realEstate.message}</p>
            )}
          </div>
        </div>
      </div>

      {/* Benefit Information */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-medium text-gray-700 mb-4">Benefits Received</h3>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="flex items-center">
            <input
              id="receivesSocialSecurity"
              type="checkbox"
              {...register('financial.benefits.receivesSocialSecurity')}
              className="h-4 w-4 text-primary focus:ring-primary border-gray-300 rounded"
            />
            <label htmlFor="receivesSocialSecurity" className="ml-2 block text-sm text-gray-700">
              Social Security Benefits
            </label>
          </div>

          <div className="flex items-center">
            <input
              id="receivesVeteransBenefits"
              type="checkbox"
              {...register('financial.benefits.receivesVeteransBenefits')}
              className="h-4 w-4 text-primary focus:ring-primary border-gray-300 rounded"
            />
            <label htmlFor="receivesVeteransBenefits" className="ml-2 block text-sm text-gray-700">
              Veterans Benefits
            </label>
          </div>

          <div className="flex items-center">
            <input
              id="receivesChildSupport"
              type="checkbox"
              {...register('financial.benefits.receivesChildSupport')}
              className="h-4 w-4 text-primary focus:ring-primary border-gray-300 rounded"
            />
            <label htmlFor="receivesChildSupport" className="ml-2 block text-sm text-gray-700">
              Child Support
            </label>
          </div>
        </div>
      </div>

      {/* Help Text */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h4 className="font-semibold text-blue-800 mb-2">Financial Information Guidance</h4>
        <p className="text-blue-600 text-sm">
          Provide accurate financial information from your most recent tax return. 
          This includes income from all sources, assets, and any government benefits received. 
          All amounts should be annual figures. If you haven't filed taxes yet, use your 
          best estimates based on your current financial situation.
        </p>
      </div>

      {/* Data Source Guidance */}
      <div className="bg-green-50 border border-green-200 rounded-lg p-4">
        <h4 className="font-semibold text-green-800 mb-2">Where to Find This Information</h4>
        <ul className="text-green-600 text-sm space-y-1">
          <li>• <strong>Adjusted Gross Income:</strong> Line 11 of IRS Form 1040</li>
          <li>• <strong>Wages, Salaries, Tips:</strong> Line 1 of IRS Form 1040</li>
          <li>• <strong>Taxable Interest:</strong> Line 2b of IRS Form 1040</li>
          <li>• <strong>Tax-Exempt Interest:</strong> Line 2a of IRS Form 1040</li>
          <li>• <strong>Assets:</strong> Current market values of cash, investments, and real estate</li>
        </ul>
      </div>
    </div>
  );
};

export default FinancialInformation;