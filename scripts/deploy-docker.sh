#!/usr/bin/env bash
set -euo pipefail

# Build and redeploy the 'web' service only
echo "Building web image and deploying with docker-compose..."

# Ensure docker-compose file exists
if [ ! -f docker-compose.yml ]; then
  echo "docker-compose.yml not found in repo root. Exiting." >&2
  exit 1
fi

docker-compose build --no-cache web
docker-compose up -d --no-deps --force-recreate web

echo "Web service rebuilt and restarted."
