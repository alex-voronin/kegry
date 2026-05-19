import fs from 'fs';
import path from 'path';
import { notFound } from 'next/navigation';

export default async function LegalPage({
  params,
}: {
  params: Promise<{ locale: string; slug: string }>;
}) {
  const { locale, slug } = await params;
  const allowedSlugs = [
    'terms-of-service',
    'privacy-policy',
    'consent-to-communication',
    'community-guidelines',
  ];

  if (!allowedSlugs.includes(slug)) {
    notFound();
  }

  // Try to find the file in the requested locale, fallback to 'en'
  const getFilePath = (lang: string) =>
    path.join(process.cwd(), 'public', 'legal', lang, `${slug}.txt`);

  let filePath = getFilePath(locale);

  if (!fs.existsSync(filePath)) {
    filePath = getFilePath('en');
  }

  try {
    const content = fs.readFileSync(filePath, 'utf-8');
    return <div className="whitespace-pre-wrap">{content}</div>;
  } catch (error) {
    console.error('Error reading legal file:', error);
    notFound();
  }
}
