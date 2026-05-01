#!/bin/bash

# Exit on any error + pipefail
set -euo pipefail

# Cleanup decrypted .env files on script termination
trap "./devops/clean-dotenv.sh" EXIT

# Change to project root relative to script location
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$DIR/.."

# echo "Verifying WolverineFX handlers are up-to-date..."
# dotnet run --project back/API.Web/API.Web.csproj -- codegen write
# git diff --exit-code || { echo "❌ Codegen is out of sync"; exit 1; }

# Decrypt secrets if they haven't been decrypted yet
if [ ! -f "devops/.env.deploy" ] && [ -f "devops/.env.deploy.sops" ]; then
    echo "🔒 Password required to decrypt SOPS secrets..."
    ./devops/secrets.sh decrypt
fi

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

SSH_PORT=${SSH_PORT:-22}

# Quick checkup ssh connection
echo "🔍 Checking SSH connection..."
ssh -p "$SSH_PORT" -o ConnectTimeout=5 -q "$TARGET_USER@$TARGET_HOST" exit || { 
    echo "❌ Error: Cannot connect to $TARGET_HOST via SSH on port $SSH_PORT."
    exit 1 
}

echo "🚀 Starting deployment to $TARGET_USER@$TARGET_HOST:$SSH_PORT..."
BUILD_TAG=$(date +%Y%m%d%H%M%S)

# 1. Build Backend
echo "📦 Building backend..."
$DOCKER_BIN build --platform linux/amd64 -t kegry-back:latest -f back/Dockerfile back/
$DOCKER_BIN tag kegry-back:latest kegry-back:$BUILD_TAG

# 2. Build Frontend
echo "📦 Building frontend..."
if [ ! -f "$FRONT_ENV_FILE" ]; then
    echo "❌ Error: $FRONT_ENV_FILE not found. Build aborted."
    exit 1
fi
$DOCKER_BIN build --platform linux/amd64 --secret id=next_env,src=$FRONT_ENV_FILE -t kegry-front:latest -f front/Dockerfile front/
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
ssh -t -p "$SSH_PORT" "$TARGET_USER@$TARGET_HOST" "sudo mkdir -p $TARGET_PATH && sudo chown -R $TARGET_USER:$TARGET_USER $TARGET_PATH"

# 5. Transfer configuration
echo "📫 Transferring docker-compose.yml and configs/..."
scp -P "$SSH_PORT" devops/docker-compose.yml "$TARGET_USER@$TARGET_HOST:$TARGET_PATH/"
rsync -av --progress -e "ssh -p $SSH_PORT" \
    devops/configs/ \
    "$TARGET_USER@$TARGET_HOST:$TARGET_PATH/configs/"

# 6. Transfer and Load Images
echo "📤 Transferring and loading images (this may take a few minutes)..."

# Create remote directory
ssh -p "$SSH_PORT" "$TARGET_USER@$TARGET_HOST" "mkdir -p $TARGET_PATH/tmp"

# Transfer via rsync with progress, compression and possibility to continue
echo "→ Transferring kegry-back.tar.gz..."
rsync -av --progress --partial -e "ssh -p $SSH_PORT" \
    devops/tmp/kegry-back.tar.gz \
    "$TARGET_USER@$TARGET_HOST:$TARGET_PATH/tmp/"

echo "→ Transferring kegry-front.tar.gz..."
rsync -av --progress --partial -e "ssh -p $SSH_PORT" \
    devops/tmp/kegry-front.tar.gz \
    "$TARGET_USER@$TARGET_HOST:$TARGET_PATH/tmp/"

# Load images on remote host
echo "📥 Loading images on remote host..."
ssh -p "$SSH_PORT" "$TARGET_USER@$TARGET_HOST" "
    cd $TARGET_PATH/tmp &&
    gunzip -c kegry-back.tar.gz | $DOCKER_BIN load &&
    gunzip -c kegry-front.tar.gz | $DOCKER_BIN load
"

# Verify images
echo "✅ Verifying images on remote host..."
ssh -p "$SSH_PORT" "$TARGET_USER@$TARGET_HOST" "
    $DOCKER_BIN image ls kegry-back:latest --format '{{.Repository}}:{{.Tag}}' | grep -q . || { echo '❌ kegry-back:latest not found'; exit 1; }
    $DOCKER_BIN image ls kegry-front:latest --format '{{.Repository}}:{{.Tag}}' | grep -q . || { echo '❌ kegry-front:latest not found'; exit 1; }
"

# 7. Start Services
echo "🔄 Starting services..."
ssh -p "$SSH_PORT" "$TARGET_USER@$TARGET_HOST" 'cd '"$TARGET_PATH"' && '"$DOCKER_COMPOSE_BIN"' up -d'

# 8. Cleanup Remote Images (keep only 3 latest for each)
echo "🧹 Cleaning up old images on remote (keeping last 3)..."
ssh -p "$SSH_PORT" "$TARGET_USER@$TARGET_HOST" '
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