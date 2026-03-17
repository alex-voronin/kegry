import { setRequestLocale } from 'next-intl/server';

interface B2BHomePageProps {
  params: Promise<{
    locale: string;
  }>;
}

export default async function B2BHomePage({ params }: B2BHomePageProps) {
  const { locale } = await params;
  setRequestLocale(locale);

  return (
    <main className="flex h-screen w-full flex-col items-center justify-center p-8 text-center">
      <h1 className="text-primary mb-4 text-4xl font-bold">
        Kegrycorn for Business
      </h1>
      <p className="text-muted-foreground mb-8 max-w-lg text-lg">
        Welcome to the Business Portal. Manage your venues, track visits, and
        create offers.
      </p>
    </main>
  );
}
