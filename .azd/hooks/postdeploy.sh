#!/bin/bash

echo "Post-deployment hook: Deployment completed successfully!"

# Get the service endpoint from azd environment
service_api_uri=$(azd env get-value SERVICE_API_URI)

if [ -n "$service_api_uri" ]; then
    echo ""
    echo "🎉 SemantiClip has been deployed successfully!"
    echo ""
    echo "📡 API Endpoint: $service_api_uri"
    echo "📊 Monitor your app: https://portal.azure.com"
    echo ""
    echo "🔧 To update environment variables:"
    echo "   azd env set VARIABLE_NAME \"new-value\""
    echo "   azd deploy"
    echo ""
    echo "📖 For more information, visit: https://github.com/vicperdana/SemantiClip"
else
    echo "⚠️  Could not retrieve service URI. Check the Azure portal for your deployed resources."
fi