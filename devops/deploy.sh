#!/bin/bash

# Exit on any error + pipefail
set -euo pipefail

# Change to project root relative to script location
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$DIR/.."

# Load deployment config
if [ -f "devops/.env.deploy" ]; then
    source "devops/.env.deploy"
else
    echo "❌ Error: devops/.env.deploy not found."
    exit 1
fi

# Check env vars
for var in TARGET_USER TARGET_HOST TARGET_PATH DOCKER_BIN DOCKER_COMPOSE_BIN FRONT_ENV_FILE; do
    if [ -z "${!var}" ]; then
        echo "❌ Error: Required variable $var is not set in devops/.env.deploy"
        exit 1
    fi
done

# Quick checkup ssh connection
echo "🔍 Checking SSH connection..."
ssh -o ConnectTimeout=5 -q "$TARGET_USER@$TARGET_HOST" exit || { 
    echo "❌ Error: Cannot connect to $TARGET_HOST via SSH."
    exit 1 
}

echo "🚀 Starting deployment to $TARGET_USER@$TARGET_HOST..."
BUILD_TAG=$(date +%Y%m%d%H%M%S)

# 1. Build Backend
echo "📦 Building backend..."
$DOCKER_BIN build -t kegry-back:latest -f back/Dockerfile back/
$DOCKER_BIN tag kegry-back:latest kegry-back:$BUILD_TAG

# 2. Build Frontend
echo "📦 Building frontend..."
if [ ! -f "$FRONT_ENV_FILE" ]; then
    echo "❌ Error: $FRONT_ENV_FILE not found. Build aborted."
    exit 1
fi
$DOCKER_BIN build --secret id=next_env,src=$FRONT_ENV_FILE -t kegry-front:latest -f front/Dockerfile front/
$DOCKER_BIN tag kegry-front:latest kegry-front:$BUILD_TAG

# 3. Save and compress images
echo "💾 Saving and compressing images..."
mkdir -p devops/tmp

$DOCKER_BIN save \
    kegry-back:latest \
    kegry-back:$BUILD_TAG \
| gzip > devops/tmp/kegry-back.tar.gz

$DOCKER_BIN save \
    kegry-front:latest \
    kegry-front:$BUILD_TAG \
| gzip > devops/tmp/kegry-front.tar.gz

# 4. Prepare remote environment
echo "📁 Preparing remote path $TARGET_PATH..."
ssh "$TARGET_USER@$TARGET_HOST" "mkdir -p $TARGET_PATH"

# 5. Transfer configuration
echo "📫 Transferring docker-compose.yml and configs/..."
scp devops/docker-compose.yml "$TARGET_USER@$TARGET_HOST:$TARGET_PATH/"
rsync -av --progress \
    devops/configs/ \
    "$TARGET_USER@$TARGET_HOST:$TARGET_PATH/configs/"

# 6. Transfer and Load Images
echo "📤 Transferring and loading images (this may take a few minutes)..."

# Create remote directory
ssh "$TARGET_USER@$TARGET_HOST" "mkdir -p $TARGET_PATH/tmp"

# Transfer via rsync with progress, compression and possibility to continue
echo "→ Transferring kegry-back.tar.gz..."
rsync -av --progress --partial \
    devops/tmp/kegry-back.tar.gz \
    "$TARGET_USER@$TARGET_HOST:$TARGET_PATH/tmp/"

echo "→ Transferring kegry-front.tar.gz..."
rsync -av --progress --partial \
    devops/tmp/kegry-front.tar.gz \
    "$TARGET_USER@$TARGET_HOST:$TARGET_PATH/tmp/"

# Load images on remote host
echo "📥 Loading images on remote host..."
ssh "$TARGET_USER@$TARGET_HOST" "
    cd $TARGET_PATH/tmp &&
    gunzip -c kegry-back.tar.gz | $DOCKER_BIN load &&
    gunzip -c kegry-front.tar.gz | $DOCKER_BIN load
"

# Verify images
echo "✅ Verifying images on remote host..."
ssh "$TARGET_USER@$TARGET_HOST" "
    $DOCKER_BIN image ls kegry-back:latest --format '{{.Repository}}:{{.Tag}}' | grep -q . || { echo '❌ kegry-back:latest not found'; exit 1; }
    $DOCKER_BIN image ls kegry-front:latest --format '{{.Repository}}:{{.Tag}}' | grep -q . || { echo '❌ kegry-front:latest not found'; exit 1; }
"

# 7. Start Services
echo "🔄 Starting services..."
ssh "$TARGET_USER@$TARGET_HOST" "cd $TARGET_PATH && $DOCKER_COMPOSE_BIN up -d"

# 8. Cleanup Remote Images (keep only 3 latest for each)
echo "🧹 Cleaning up old images on remote (keeping last 3)..."
ssh "$TARGET_USER@$TARGET_HOST" '
    echo "🧹 Removing old containers..."
    docker container prune -f

    echo "🧹 Cleaning images..."

    for repo in kegry-back kegry-front; do
        echo "→ $repo"

        # Get all tags except latest, newest first
        mapfile -t old_tags < <(
            docker images "$repo" \
                --format "{{.Tag}}" \
            | grep -v "^latest$" \
            | sort -r \
            | tail -n +4
        )

        # Delete all except 3 latest versioned tags
        for tag in "${old_tags[@]}"; do
            echo "Removing $repo:$tag"
            docker rmi -f "$repo:$tag" || true
        done
    done

    echo "🧹 Removing dangling images..."
    docker image prune -f
'

# 9. Cleanup Local Temporary Files
echo "🧹 Cleaning up local archives..."
rm -rf devops/tmp

echo "✅ Deployment complete!"