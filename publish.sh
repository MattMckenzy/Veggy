#!/bin/bash

docker buildx build --build-arg CONFIG="Release" --platform=linux/amd64,linux/arm64 --push -t "mattmckenzy/veggy:latest" -f "Dockerfile" .