import React, { Suspense } from 'react';
import TrialPageContent from './TrialPageContent';

function LoadingFallback() {
  return (
    <div className="min-h-screen bg-gray-50 pt-24 flex items-center justify-center">
      <div className="text-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-4"></div>
        <p className="text-gray-600">Yukleniyor...</p>
      </div>
    </div>
  );
}

export default function StartTrialPage() {
  return (
    <Suspense fallback={<LoadingFallback />}>
      <TrialPageContent />
    </Suspense>
  );
}
