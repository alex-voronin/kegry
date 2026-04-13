#!/bin/bash

# Server Initialization Wrapper Script (Local)
# Sources remote config and executes setup via SSH

set -euo pipefail

# Change to project root relative to script location
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$DIR/.."

# 1. Load deployment config
if [ -f "devops/.env.deploy" ]; then
    source "devops/.env.deploy"
else
    echo "❌ Error: devops/.env.deploy not found."
    exit 1
fi

# 2. Check required variables
for var in TARGET_USER TARGET_HOST SSH_PORT; do
    if [ -z "${!var}" ]; then
        echo "❌ Error: Required variable $var is not set in devops/.env.deploy"
        exit 1
    fi
done

echo "🚀 Starting remote server initialization on $TARGET_HOST:$SSH_PORT..."

echo "📤 Transferring provisioning script to remote server..."

# Create a temporary script file locally
TMP_SCRIPT=$(mktemp)
cat > "$TMP_SCRIPT" <<'EOF'
#!/bin/bash
set -euo pipefail
set -x

echo "🔄 Updating apt repository..."
apt-get update -qq

echo "🧹 Removing old Docker versions if any..."
apt-get remove -y docker docker-engine docker.io containerd runc 2>/dev/null || true

echo "🐳 Installing prerequisites..."
apt-get install -y ca-certificates curl gnupg lsb-release htop rsync

echo "🐳 Setting up Docker official repository..."

# Create directory for Docker GPG key
install -m 0755 -d /etc/apt/keyrings

# Download and save Docker official GPG key
OS_ID=$(grep -E '^ID=' /etc/os-release | cut -d= -f2 | tr -d '"')
OS_CODENAME=$(lsb_release -cs)
curl -fsSL "https://download.docker.com/linux/$OS_ID/gpg" | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg

# Add Docker repository using modern .sources format (recommended for Ubuntu 22.04+)
cat > /etc/apt/sources.list.d/docker.sources <<INNEREOF
Types: deb
URIs: https://download.docker.com/linux/$OS_ID
Suites: $OS_CODENAME
Components: stable
Signed-By: /etc/apt/keyrings/docker.gpg
INNEREOF

apt-get update -qq

echo "🐳 Installing Docker Engine..."
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

echo "🐳 Enabling and starting Docker service..."
systemctl enable --now docker

echo "👤 Adding $TARGET_USER to docker group..."
usermod -aG docker "$TARGET_USER"

echo "🛡️ Installing and configuring Fail2Ban..."
apt-get install -y fail2ban

echo "⚙️ Configuring Fail2Ban for SSH on custom port..."
cat > /etc/fail2ban/jail.local <<INNEREOF
[DEFAULT]
bantime  = 1h
findtime = 10m
maxretry = 5
ignoreip = 127.0.0.1/8 ::1

[sshd]
enabled  = true
port     = $SSH_PORT
logpath  = %(sshd_log)s
backend  = systemd
INNEREOF

echo "🔄 Restarting Fail2Ban..."
systemctl restart fail2ban

echo "✅ Remote server initialization completed successfully!"
echo "   Docker version: $(docker --version)"
echo "   User added to docker group: $TARGET_USER"
EOF

# Copy the script to the remote server
scp -P "$SSH_PORT" -q "$TMP_SCRIPT" "$TARGET_USER@$TARGET_HOST:/tmp/setup_script.sh"

# Remove the local temporary file
rm "$TMP_SCRIPT"

echo "⚙️ Executing script on remote server (You may be prompted for your sudo password)..."

# Execute the script directly with sudo
# Pass TARGET_USER and SSH_PORT as environment variables so the script can use them
ssh -t -p "$SSH_PORT" "$TARGET_USER@$TARGET_HOST" "export TARGET_USER='$TARGET_USER' SSH_PORT='$SSH_PORT'; sudo -E bash /tmp/setup_script.sh && rm /tmp/setup_script.sh"

echo "🎉 Done! You may need to log out and log back in (or run 'newgrp docker') for the docker group changes to take full effect."