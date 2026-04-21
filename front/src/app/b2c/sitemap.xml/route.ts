import { NextResponse } from 'next/server';

const DOMAIN = 'https://kegry.rs';
const LOCALES = ['en', 'sr'];

export async function GET() {
  const staticPages = [
    '',
    '/privacy-policy',
    '/consent-to-communication',
    '/terms-of-service',
    '/community-guidelines',
  ];

  let xml = '<?xml version="1.0" encoding="UTF-8"?>\n';
  xml +=
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9" xmlns:xhtml="http://www.w3.org/1999/xhtml">\n';

  for (const page of staticPages) {
    const isHomePage = page === '';
    const currentLocales = isHomePage ? LOCALES : ['en'];

    for (const locale of currentLocales) {
      const isDefault = locale === 'en';
      const path = isDefault ? (page ? `${page}/` : '/') : `/${locale}${page}/`;
      const url = `${DOMAIN}${path}`;

      xml += '  <url>\n';
      xml += `    <loc>${url}</loc>\n`;
      xml +=
        '    <lastmod>' +
        new Date().toISOString().split('T')[0] +
        '</lastmod>\n';
      xml += '    <changefreq>monthly</changefreq>\n';
      xml += '    <priority>' + (isHomePage ? '1.0' : '0.8') + '</priority>\n';

      if (isHomePage) {
        // Alternates for SEO
        xml += `    <xhtml:link rel="alternate" hreflang="en" href="${DOMAIN}/" />\n`;
        xml += `    <xhtml:link rel="alternate" hreflang="sr" href="${DOMAIN}/sr/" />\n`;
        xml += `    <xhtml:link rel="alternate" hreflang="x-default" href="${DOMAIN}/" />\n`;
      }

      xml += '  </url>\n';
    }
  }

  xml += '</urlset>';

  return new NextResponse(xml, {
    headers: {
      'Content-Type': 'application/xml',
    },
  });
}
