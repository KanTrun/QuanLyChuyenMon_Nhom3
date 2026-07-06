#!/usr/bin/env bash
set -euo pipefail

# Usage: ./scripts/git-push.sh "commit message"
MSG=${1:-"Update: auto commit"}

echo "Staging changes..."
git add -A

if git diff --cached --quiet; then
  echo "No staged changes to commit."
else
  echo "Committing with message: $MSG"
  git commit -m "$MSG"
fi

echo "Pulling latest from remote (rebase)..."
git pull --rebase

echo "Pushing to remote..."
git push

echo "Done."
