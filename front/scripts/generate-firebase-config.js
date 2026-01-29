// Script to generate firebase-config.js from environment variables
// Run during build: node scripts/generate-firebase-config.js

const fs = require('fs');
const path = require('path');

const config = {
  apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY || '',
  authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN || '',
  projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID || '',
  messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID || '',
  appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID || '',
};

const content = `// Auto-generated Firebase config for service worker
// Generated at: ${new Date().toISOString()}
self.firebaseConfig = ${JSON.stringify(config, null, 2)};
`;

const outputPath = path.join(__dirname, '..', 'public', 'firebase-config.js');

fs.writeFileSync(outputPath, content, 'utf8');

console.log('Generated firebase-config.js');
