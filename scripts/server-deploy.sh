#!/bin/bash
set -e

APP_DIR="/opt/maemo-compliance/app"
ENV_FILE="/opt/maemo-compliance/.env"
COMPOSE="docker compose -f $APP_DIR/docker-compose.prod.yml --env-file $ENV_FILE"

echo "=== Maemo Compliance Deploy ==="
echo "Time: $(date)"

cd "$APP_DIR"
git pull origin main

echo "Pulling latest images..."
$COMPOSE pull

echo "Starting services..."
$COMPOSE up -d --remove-orphans

echo "Waiting for API health..."
sleep 15
curl -f http://localhost:8090/health/live \
  && echo "Health check passed" \
  || (echo "HEALTH CHECK FAILED" && $COMPOSE logs --tail 30 && exit 1)

echo "=== Deploy complete ==="
