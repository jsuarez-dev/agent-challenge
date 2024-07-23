#!/bin/bash

# Load environment variables from .env file and set them as user secrets
project=$1

if [ -z "$project" ]; then
  echo "you need to selecte a project target to add the secrets"
  exit 1
fi

if [ -f ".env" ]; then
  dotnet user-secrets init --project "$project"
  while IFS='=' read -r key value; do
    if [[ $key != \#* ]]; then
      key=$(echo $key | xargs)
      value=$(echo $value | xargs)
      dotnet user-secrets set "$key" "$value" --project "$project"
    fi
  done < .env
else
  echo ".env file not found."
fi
