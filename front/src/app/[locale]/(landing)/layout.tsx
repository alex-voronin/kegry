'use client';

import React from 'react';

export default function LandingLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  // Landing page uses full-width layout
  // This wrapper breaks out of the parent container's max-width constraint
  return (
    <div className="fixed inset-0 overflow-auto bg-[#333333]">{children}</div>
  );
}
