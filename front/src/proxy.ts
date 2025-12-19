import createMiddleware from 'next-intl/middleware';
import { routing } from './i18n/routing';

export default createMiddleware(routing);

export const config = {
  // Match all pathnames except for the API routes, Next.js assets, and paths with dots
  matcher: ['/((?!api|_next|_vercel|assets|.*\\..*).*)'],
};
