#!/bin/bash
set -e

# Test script to validate azd deployment setup
echo "Testing Azure Developer CLI deployment setup..."

# Check if required files exist
echo "✓ Checking azure.yaml..."
if [ ! -f "azure.yaml" ]; then
  echo "❌ azure.yaml not found"
  exit 1
fi

echo "✓ Checking infra directory..."
if [ ! -d "infra" ]; then
  echo "❌ infra directory not found"
  exit 1
fi

echo "✓ Checking main Bicep template..."
if [ ! -f "infra/main.bicep" ]; then
  echo "❌ infra/main.bicep not found"
  exit 1
fi

echo "✓ Checking src directory..."
if [ ! -d "src" ]; then
  echo "❌ src directory not found"
  exit 1
fi

echo "✓ Checking Dockerfile..."
if [ ! -f "src/SemanticClip.API/Dockerfile" ]; then
  echo "❌ Dockerfile not found"
  exit 1
fi

echo "✓ Checking .NET solution..."
if [ ! -f "src/SemantiClip.sln" ]; then
  echo "❌ .NET solution not found"
  exit 1
fi

# Validate Bicep templates
echo "✓ Validating Bicep templates..."
cd infra
if ! az bicep build --file main.bicep > /dev/null 2>&1; then
  echo "❌ Bicep template validation failed"
  exit 1
fi
cd ..

# Test .NET build
echo "✓ Testing .NET build..."
cd src
if ! dotnet build --configuration Release --verbosity quiet > /dev/null 2>&1; then
  echo "❌ .NET build failed"
  exit 1
fi
cd ..

echo "✅ All tests passed! Ready for azd deployment."
echo ""
echo "To deploy to Azure:"
echo "1. azd auth login"
echo "2. azd init (if not already done)"
echo "3. azd up"