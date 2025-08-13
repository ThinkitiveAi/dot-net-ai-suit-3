#!/bin/bash
echo "Starting Healthcare Portal API with network binding..."
echo "The application will be accessible at:"
echo "- Local: http://localhost:57157/swagger"
echo "- Network: http://192.168.0.37:57157/swagger"
echo ""
echo "Press Ctrl+C to stop the application"
echo ""

export ASPNETCORE_URLS="http://0.0.0.0:57157"
export ASPNETCORE_ENVIRONMENT="Development"

dotnet run
