import createMiddleware from 'next-intl/middleware';
import { routing } from './i18n/routing';
import { NextRequest } from 'next/server';

const intlMiddleware = createMiddleware(routing);

export default function middleware(request: NextRequest) {
  const response = intlMiddleware(request);

  // Set pathname header for layout detection
  const pathname = request.nextUrl.pathname;
  response.headers.set('x-pathname', pathname);

  return response;
}

export const config = {
  // Match all pathnames except for the API routes, Next.js assets, and paths with dots
  matcher: ['/((?!api|_next|_vercel|assets|.*\\..*).*)'],
};
