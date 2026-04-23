#!/usr/bin/env bash
set -euo pipefail

command -v sops >/dev/null || { echo "❌ sops not installed"; exit 1; }

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$DIR"

COMMAND=${1:-}
FILE=${2:-}
CLOUDFLARED_CREDS_PATH="configs/cloudflared/credentials.json"

if [[ "$COMMAND" != "encrypt" && "$COMMAND" != "decrypt" ]]; then
    echo "Usage: ./secrets.sh [encrypt|decrypt] [optional_file]"
    exit 1
fi

process_file() {
    local cmd=$1
    local file=$2
    local target
    
    local type="dotenv"
    if [[ "$file" == *.json* ]]; then
        type="json"
    fi

    if [[ "$cmd" == "encrypt" ]]; then
        target="${file}.sops"
        if [ -f "$target" ]; then
            echo "⚠️  Skipping $target (already exists)"
            return
        fi
        echo "→ Encrypting $file"
        sops -e --input-type "$type" --output-type "$type" "$file" > "$target"
    else
        target="${file%.sops}"
        if [ -f "$target" ]; then
            echo "⚠️  Skipping $target (already exists)"
            return
        fi
        echo "→ Decrypting $file"
        sops -d --input-type "$type" --output-type "$type" "$file" > "$target"
    fi
}

if [ -n "$FILE" ]; then
    process_file "$COMMAND" "$FILE"
elif [ "$COMMAND" = "encrypt" ]; then
    while IFS= read -r file; do
        process_file "$COMMAND" "$file"
    done < <(find . -type f -name ".env.*" ! -name "*.example" ! -name "*.sops")
    
    if [ -f "$CLOUDFLARED_CREDS_PATH" ]; then
        process_file "encrypt" "$CLOUDFLARED_CREDS_PATH"
    fi
elif [ "$COMMAND" = "decrypt" ]; then
    while IFS= read -r file; do
        process_file "$COMMAND" "$file"
    done < <(find . -type f -name ".env.*.sops")
    
    if [ -f "${CLOUDFLARED_CREDS_PATH}.sops" ]; then
        process_file "decrypt" "${CLOUDFLARED_CREDS_PATH}.sops"
    fi
fi

echo "✅ Done."