import React from 'react';

interface FormSectionTitleProps {
  children: React.ReactNode;
  className?: string; // Allow optional className override/extension
  required?: boolean;
}

export function FormSectionTitle({
  children,
  className = '',
  required = false,
}: FormSectionTitleProps) {
  return (
    <h3 className={`pl-2 text-sm text-white/50 sm:pl-0 ${className}`}>
      {children}
      {required && <span className="text-primary ml-0.5 text-base">*</span>}
    </h3>
  );
}
