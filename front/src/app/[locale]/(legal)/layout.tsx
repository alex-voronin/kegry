import Link from 'next/link';
import { ArrowLeft } from 'lucide-react';
import { PAGE_ROUTES } from '@/lib/routes';

export default function LegalLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="bg-background text-foreground flex min-h-screen w-full flex-col">
      <main className="bg-form-element mx-auto w-full max-w-[800px] flex-1 rounded-2xl p-8">
        <header className="mb-8 flex w-full items-center">
          <Link
            href={PAGE_ROUTES.HOME}
            className="bg-surface/80 hover:bg-surface flex items-center gap-2 rounded-full px-4 py-2 text-sm font-medium"
          >
            <ArrowLeft size={16} />
            Back
          </Link>
        </header>
        {children}
      </main>
    </div>
  );
}
