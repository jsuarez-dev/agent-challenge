#!/bin/bash

# Load environment variables from .env file and set them as user secrets
if [ -f ".env" ]; then
  while IFS='=' read -r key value; do
    if [[ $key != \#* ]]; then
      key=$(echo $key | xargs)
      value=$(echo $value | xargs)
      dotnet user-secrets set "$key" "$value" --project WebConnection
    fi
  done < .env
else
  echo ".env file not found."
fi
