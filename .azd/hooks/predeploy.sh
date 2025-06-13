#!/bin/bash

echo "Pre-deployment hook: Validating configuration..."

# Check if required environment variables are set
required_vars=(
    "AZURE_OPENAI_ENDPOINT"
    "AZURE_OPENAI_API_KEY"
)

missing_vars=()

for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        missing_vars+=("$var")
    fi
done

if [ ${#missing_vars[@]} -ne 0 ]; then
    echo "❌ Missing required environment variables:"
    printf '   %s\n' "${missing_vars[@]}"
    echo ""
    echo "Please set these variables using:"
    echo "   azd env set VARIABLE_NAME \"value\""
    echo ""
    echo "For example:"
    echo "   azd env set AZURE_OPENAI_ENDPOINT \"https://your-service.openai.azure.com/\""
    echo "   azd env set AZURE_OPENAI_API_KEY \"your-api-key\""
    exit 1
fi

echo "✅ All required environment variables are set"