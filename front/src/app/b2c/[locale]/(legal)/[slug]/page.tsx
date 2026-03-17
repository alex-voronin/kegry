import fs from 'fs';
import path from 'path';
import { notFound } from 'next/navigation';

export default async function LegalPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const allowedSlugs = [
    'terms-of-service',
    'privacy-policy',
    'consent-to-communication',
    'community-guidelines',
  ];

  if (!allowedSlugs.includes(slug)) {
    notFound();
  }

  const filePath = path.join(process.cwd(), 'public', 'legal', `${slug}.txt`);

  try {
    const content = fs.readFileSync(filePath, 'utf-8');
    return <div className="whitespace-pre-wrap">{content}</div>;
  } catch (error) {
    console.error('Error reading legal file:', error);
    notFound();
  }
}
