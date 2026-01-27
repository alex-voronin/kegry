'use client';

import { useEffect, useCallback, useRef, useState } from 'react';

// Global declarations for Google Identity Services
declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string;
            callback: (response: { credential: string }) => void;
            auto_select?: boolean;
            cancel_on_tap_outside?: boolean;
          }) => void;
          renderButton: (
            parent: HTMLElement,
            options: {
              theme?: 'outline' | 'filled_blue' | 'filled_black';
              size?: 'large' | 'medium' | 'small';
              type?: 'standard' | 'icon';
              text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
              shape?: 'rectangular' | 'pill' | 'circle' | 'square';
              logo_alignment?: 'left' | 'center';
              width?: string;
              locale?: string;
            },
          ) => void;
          prompt: () => void;
        };
      };
    };
  }
}

const GOOGLE_CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID!;

export interface UseGoogleSignInOptions {
  onSuccess: (credential: string) => void;
  onError?: (error: Error) => void;
}

export function useGoogleSignIn({
  onSuccess,
  onError,
}: UseGoogleSignInOptions) {
  const [isGoogleLoaded, setIsGoogleLoaded] = useState(false);
  const isInitialized = useRef(false);

  const handleCredentialResponse = useCallback(
    (response: { credential: string }) => {
      if (response.credential) {
        onSuccess(response.credential);
      } else {
        onError?.(new Error('No credential received from Google'));
      }
    },
    [onSuccess, onError],
  );

  // Check if already loaded (e.g., from a previous mount)
  useEffect(() => {
    if (window.google && !isGoogleLoaded) {
      setIsGoogleLoaded(true);
    }
  }, [isGoogleLoaded]);

  const onScriptLoad = useCallback(() => {
    setIsGoogleLoaded(true);
  }, []);

  useEffect(() => {
    if (!isGoogleLoaded || isInitialized.current || !window.google) return;

    try {
      window.google.accounts.id.initialize({
        client_id: GOOGLE_CLIENT_ID,
        callback: handleCredentialResponse,
        auto_select: false,
        cancel_on_tap_outside: true,
      });
      isInitialized.current = true;
    } catch (error) {
      console.error('Failed to initialize Google Sign-In:', error);
      onError?.(error as Error);
    }
  }, [isGoogleLoaded, handleCredentialResponse, onError]);

  const renderedElementIds = useRef<Set<string>>(new Set());

  const renderButton = useCallback(
    (elementId: string) => {
      if (!isGoogleLoaded || !window.google) {
        return;
      }

      // Prevent re-rendering to the same element (Strict Mode protection)
      if (renderedElementIds.current.has(elementId)) {
        return;
      }

      const element = document.getElementById(elementId);
      if (!element) {
        console.warn('Google Sign-In element not found:', elementId);
        return;
      }

      // Clear any existing content to prevent stacking
      element.innerHTML = '';

      window.google.accounts.id.renderButton(element, {
        type: 'icon',
        shape: 'circle',
        theme: 'filled_blue',
        size: 'large',
      });

      renderedElementIds.current.add(elementId);
    },
    [isGoogleLoaded],
  );

  return { renderButton, isGoogleLoaded, onScriptLoad };
}
