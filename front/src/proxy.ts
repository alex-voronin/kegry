import { NextRequest, NextResponse } from 'next/server';
import createMiddleware from 'next-intl/middleware';
import { routing } from './i18n/routing';
import { B2B_SUBDOMAIN } from './lib/constants';

const intlMiddleware = createMiddleware(routing);

function SetSrLocaleCookie(response: NextResponse) {
  response.cookies.set('NEXT_LOCALE', 'sr', {
    maxAge: 60 * 60 * 24 * 365, // 1 year
    path: '/',
    sameSite: 'lax',
  });
}

export default function middleware(req: NextRequest) {
  // 1. Detect referer-based locale preference (.rs -> sr)
  let forceSrLocale = false;
  const referer = req.headers.get('referer');
  if (referer && !req.cookies.has('NEXT_LOCALE')) {
    try {
      const refererUrl = new URL(referer);
      if (
        refererUrl.hostname === 'kegry.rs' ||
        refererUrl.hostname.endsWith('.kegry.rs')
      ) {
        forceSrLocale = true;
      }
    } catch (e) {
      // Ignore invalid referer URLs
    }
  }

  if (forceSrLocale) {
    req.cookies.set('NEXT_LOCALE', 'sr');
  }

  // 2. Run next-intl middleware to handle locales and redirects
  const response = intlMiddleware(req);

  // If next-intl issued a redirect (e.g., to add a default locale), return it immediately
  if (response.status >= 300 && response.status < 400) {
    if (forceSrLocale) {
      SetSrLocaleCookie(response);
    }
    return response;
  }

  // 3. Determine the platform based on the requested host
  const hostname = req.headers.get('host') || '';
  const isB2B = hostname.startsWith(B2B_SUBDOMAIN);
  const platform = isB2B ? 'b2b' : 'b2c';

  // Set pathname header for layout detection
  // We use the original nextUrl.pathname here, before any rewrites.
  const pathname = req.nextUrl.pathname;
  response.headers.set('x-pathname', pathname);

  // 4. Apply the platform routing via Server Rewrites
  const rewriteUrlStr = response.headers.get('x-middleware-rewrite');

  if (rewriteUrlStr) {
    // If next-intl already set a rewrite, modify it to include the platform directory
    const rewriteUrl = new URL(rewriteUrlStr);
    rewriteUrl.pathname = `/${platform}${rewriteUrl.pathname}`;
    response.headers.set('x-middleware-rewrite', rewriteUrl.toString());
    if (forceSrLocale) {
      SetSrLocaleCookie(response);
    }
    return response;
  }

  // Otherwise, create our own rewrite using the request's URL
  const clonedUrl = req.nextUrl.clone();
  clonedUrl.pathname = `/${platform}${clonedUrl.pathname}`;

  // Return a new rewrite, but preserve headers set by next-intl (like x-next-intl-locale)
  const platformResponse = NextResponse.rewrite(clonedUrl, {
    headers: response.headers,
  });

  if (forceSrLocale) {
    SetSrLocaleCookie(platformResponse);
  }
  return platformResponse;
}

export const config = {
  // Match all paths except api routes, _next, static files, etc.
  matcher: ['/((?!api|_next|_vercel|assets|.*\\..*).*)'],
};
