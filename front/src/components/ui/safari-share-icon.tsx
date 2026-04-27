import React from 'react';

export function SafariShareIcon({ className }: { className?: string }) {
  return (
    <svg
      viewBox="12 5 26 36"
      className={className}
      fill="currentColor"
      width="32"
      height="32"
      stroke="currentColor"
      strokeWidth="0.25"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M30.3 13.7L25 8.4l-5.3 5.3-1.4-1.4L25 5.6l6.7 6.7z" />
      <path d="M24 7h2v21h-2z" />
      <path d="M35 40H15c-1.7 0-3-1.3-3-3V19c0-1.7 1.3-3 3-3h7v2h-7c-.6 0-1 .4-1 1v18c0 .6.4 1 1 1h20c.6 0 1-.4 1-1V19c0-.6-.4-1-1-1h-7v-2h7c1.7 0 3 1.3 3 3v18c0 1.7-1.3 3-3 3z" />
    </svg>
  );
}
