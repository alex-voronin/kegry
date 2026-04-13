#!/usr/bin/env bash
set -euo pipefail

command -v sops >/dev/null || { echo "❌ sops not installed"; exit 1; }

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$DIR"

COMMAND=${1:-}

if [[ "$COMMAND" != "encrypt" && "$COMMAND" != "decrypt" ]]; then
    echo "Usage: ./secrets.sh [encrypt|decrypt]"
    exit 1
fi

if [ "$COMMAND" = "encrypt" ]; then
    while IFS= read -r file; do
        target="${file}.sops"

        if [ -f "$target" ]; then
            echo "⚠️  Skipping $target (already exists)"
            continue
        fi

        echo "→ Encrypting $file"
        sops -e --input-type dotenv --output-type dotenv "$file" > "$target"
    done < <(find . -type f -name ".env.*" ! -name "*.example" ! -name "*.sops")

elif [ "$COMMAND" = "decrypt" ]; then
    while IFS= read -r file; do
        target="${file%.sops}"

        if [ -f "$target" ]; then
            echo "⚠️  Skipping $target (already exists)"
            continue
        fi

        echo "→ Decrypting $file"
        sops -d --input-type dotenv --output-type dotenv "$file" > "$target"
    done < <(find . -type f -name ".env.*.sops")
fi

echo "✅ Done."