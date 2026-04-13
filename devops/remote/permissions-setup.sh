#!/bin/bash

# IMPORTANT: This script is intended to be run MANUALLY on the TARGET SERVER.
# Copy it to the remote host first:
# scp devops/run-local/permissions-setup.sh root@target-host:/tmp/
# Then run it via SSH:
# ssh root@target-host "sudo bash /tmp/permissions-setup.sh"

echo "⚠️  WARNING: This script manages system-level users and permissions."
echo "⚠️  Ensure you are running this on the TARGET SERVER, not your local machine."
echo ""

read -p "Are you sure you want to proceed with the setup on this machine? (y/N): " confirm
if [[ ! "$confirm" =~ ^[Yy]$ ]]; then
    echo "❌ Execution aborted by user."
    exit 0
fi

echo "🚀 Proceeding with setup..."
echo ""

# Check if the script is run as root
if [ "$EUID" -ne 0 ]; then 
  echo "Please run as root (use sudo)"
  exit 1
fi

# 1. Create the 'deploy' group
groupadd deploy

# 2. Create the 'sshadmin' user with a home directory and bash shell
useradd -m -s /bin/bash sshadmin

# 3. Add 'sshadmin' and 'root' to the 'deploy' group
usermod -aG deploy sshadmin
usermod -aG deploy root

# 4. Set ownership for the /srv directory
# Root remains the owner, 'deploy' becomes the group owner
DEPLOY_DIR="/srv"
chown -R root:deploy "$DEPLOY_DIR"

# 5. Set permissions:
# 2775 for directories (2 is SGID bit for group inheritance, 775 for access)
# 664 for files (read/write for owner and group)
find "$DEPLOY_DIR" -type d -exec chmod 2775 {} +
find "$DEPLOY_DIR" -type f -exec chmod 664 {} +

echo "--------------------------------------------------"
echo "Setup completed successfully!"
echo "User 'sshadmin' created and added to 'deploy' group."
echo "Permissions for $DEPLOY_DIR updated with SGID bit."
echo "New files in $DEPLOY_DIR will now inherit the 'deploy' group."
echo "--------------------------------------------------"
