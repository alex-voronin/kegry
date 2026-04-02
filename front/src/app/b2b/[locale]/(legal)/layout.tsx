'use client';

import { ArrowLeft } from 'lucide-react';
import React from 'react';
import { useSafeBack } from '@/hooks/use-safe-back';
import { PAGE_ROUTES } from '@/lib/routes';

export default function LegalLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { safeBack } = useSafeBack();

  return (
    <div className="bg-background text-foreground flex min-h-screen w-full flex-col">
      <main className="bg-form-element mx-auto w-full max-w-[800px] flex-1 rounded-none p-8 sm:rounded-2xl">
        <header className="mb-8 flex w-full items-center">
          <button
            onClick={() => {
              if (window.history.length > 1) {
                safeBack(PAGE_ROUTES.B2B.HOME);
              } else {
                window.close();
              }
            }}
            className="bg-surface/80 hover:bg-surface flex cursor-pointer items-center gap-2 rounded-full px-4 py-2 text-sm font-medium transition-colors"
          >
            <ArrowLeft size={16} />
            Back
          </button>
        </header>
        {children}
      </main>
    </div>
  );
}
