# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install FFmpeg which is required by the application
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    rm -rf /var/lib/apt/lists/*

# Use .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution file and project files
COPY ["SemantiClip.sln", "."]
COPY ["SemanticClip.API/SemanticClip.API.csproj", "SemanticClip.API/"]
COPY ["SemanticClip.Core/SemanticClip.Core.csproj", "SemanticClip.Core/"]
COPY ["SemanticClip.Services/SemanticClip.Services.csproj", "SemanticClip.Services/"]
COPY ["SemanticClip.Client/SemanticClip.Client.csproj", "SemanticClip.Client/"]

# Restore dependencies
RUN dotnet restore "SemanticClip.API/SemanticClip.API.csproj"

# Copy source code
COPY . .

# Build the API project
WORKDIR "/src/SemanticClip.API"
RUN dotnet build "SemanticClip.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SemanticClip.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage - runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "SemanticClip.API.dll"]