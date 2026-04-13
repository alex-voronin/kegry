#!/usr/bin/env bash
set -euo pipefail

command -v sops >/dev/null || { echo "❌ sops not installed"; exit 1; }

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$DIR"

if [ -f ".sops.yaml" ]; then
    echo "⚠️  .sops.yaml already exists. Refusing to overwrite."
    exit 1
fi

SSH_PUB_KEY_FILE="$HOME/.ssh/id_ed25519.pub"

if [ ! -f "$SSH_PUB_KEY_FILE" ]; then
    echo "❌ Could not find $SSH_PUB_KEY_FILE"
    exit 1
fi

# Take only type and key, without the comment
SSH_PUB_KEY=$(awk '{print $1 " " $2}' "$SSH_PUB_KEY_FILE")

echo "Creating .sops.yaml using your SSH key..."

cat > .sops.yaml <<EOF
creation_rules:
  - path_regex: \\.env\\..+
    age: >-
      $SSH_PUB_KEY
EOF

echo "✅ .sops.yaml created."
echo "👉 Run: ./secrets.sh encrypt"