# Repo convenience Makefile

.PHONY: git-push deploy-docker

git-push:
	./scripts/git-push.sh

deploy-docker:
	./scripts/deploy-docker.sh
