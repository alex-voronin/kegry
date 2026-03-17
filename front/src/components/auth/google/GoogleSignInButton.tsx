'use client';

import { useEffect } from 'react';
import Script from 'next/script';
import { useGoogleSignIn } from '@/lib/auth/use-google-sign-in';

export interface GoogleSignInButtonProps {
  onSuccess: (credential: string) => void;
  onError?: (error: Error) => void;
  containerId?: string;
}

export function GoogleSignInButton({
  onSuccess,
  onError,
  containerId = 'google-signin-button',
}: GoogleSignInButtonProps) {
  const { renderButton, isGoogleLoaded, onScriptLoad } = useGoogleSignIn({
    onSuccess,
    onError,
  });

  useEffect(() => {
    if (isGoogleLoaded) {
      renderButton(containerId);
    }
  }, [isGoogleLoaded, renderButton, containerId]);

  return (
    <>
      <Script
        src="https://accounts.google.com/gsi/client"
        strategy="afterInteractive"
        onLoad={onScriptLoad}
      />
      <div id={containerId} />
    </>
  );
}


