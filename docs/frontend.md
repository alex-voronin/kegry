# Frontend

## Stack

- Next.js 16 with the App Router
- React 19, TypeScript
- Tailwind CSS v4, shadcn/ui
- Zustand for state
- next-intl for i18n
- Vitest for unit tests

## Structure and routing

- Entrypoint: `front/src/app/`.
- Two platform route trees: `front/src/app/b2c/[locale]` and `front/src/app/b2b/[locale]`.
- `front/src/proxy.ts` selects the platform from the `Host` header (`business.` prefix → B2B), handles `robots.txt` / `sitemap.xml`, and runs the `next-intl` middleware.
- The B2B portal is served on the `business.` subdomain; everything else is B2C.

## i18n

- `next-intl`, locales `['en', 'sr']`, default `sr`, `localePrefix: 'as-needed'`.
- Messages live in `front/messages/{b2c,b2b}/{locale}.json`.
- Platform-specific messages are selected from the `Host` header in `front/src/i18n/request.ts`.

## State

- Zustand stores use the `use-*-store.ts` naming pattern.
- Shared stores live in `front/src/store/`; page-specific stores are co-located with pages.

## API and auth

- `front/src/lib/api-client.ts` is the Axios client for regular API calls. It attaches the JWT access token, refreshes it on 401, and retries transient GET errors.
- `front/src/lib/auth-client.ts` is a separate Axios instance for auth endpoints (`/auth/*`) to avoid interceptor loops.
- Auth is API-based: JWT access/refresh tokens are stored in `front/src/lib/auth/use-auth-store.ts`.
- Google sign-in uses Google Identity Services (`front/src/lib/auth/use-google-sign-in.ts`).
- Firebase is used only for FCM push notifications (`front/src/lib/fcm/`), not for authentication.
- The frontend proxies `/api/*` to `NEXT_PUBLIC_API_BASE_URL` via `next.config.ts` rewrites.

## Configuration and build

- `npm run generate-firebase-config` and `npm run generate-pwa-assets` run automatically in `predev` / `prebuild`.
- `front/public/firebase-config.js` is generated and gitignored.
- Production uses a Next.js standalone build (`output: 'standalone'`).

## UI conventions

Content sections follow the desktop/mobile pattern. Layouts are often implemented in CSS modules with `@apply` (for example `front/src/app/b2c/[locale]/settings/settings.module.css`), but the same classes can be used directly.

- Outer wrapper: `sm:px-4` — no horizontal padding on mobile, items go edge-to-edge.
- Item cards: `bg-form-element px-4 py-3 sm:rounded-xl` — rounded corners only on desktop.
- Clickable items: add `hover:bg-hover active:bg-active`.
- Labels: `text-secondary text-sm`.
- Values: `text-primary`.
- Section titles: `text-secondary text-sm` with left margin.
- Grouped items: wrap in a `bg-form-element overflow-hidden sm:rounded-xl` container; child items are separated by `border-surface border-b` and use `last:border-0`.

## Formatting and tests

- `make format` runs `dotnet format` for the backend and ESLint `--fix` + Prettier for the frontend.
- Prettier config: single quotes, semicolons, `prettier-plugin-tailwindcss`.
- `npm test` runs Vitest. The current suite covers `front/src/lib/api-error.test.ts`.
- `npm run lint` runs ESLint.
