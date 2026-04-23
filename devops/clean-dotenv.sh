#!/usr/bin/env bash
set -euo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$DIR/.."

echo "🧹 Removing decrypted .env files..."

# Find all .sops files and remove their decrypted counterparts
find devops -type f -name ".env.*.sops" | while read -r sops_file; do
    env_file="${sops_file%.sops}"
    if [ -f "$env_file" ]; then
        rm "$env_file"
    fi
done

# Clean up decrypted cloudflared credentials
creds_file="devops/configs/cloudflared/credentials.json"
if [ -f "$creds_file" ]; then
    rm "$creds_file"
fi
echo "✅ Cleanup complete."
