# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the solution file
COPY SemanticClip.sln ./

# Copy project files for each project
COPY SemanticClip.API/SemanticClip.API.csproj SemanticClip.API/
COPY SemanticClip.Core/SemanticClip.Core.csproj SemanticClip.Core/
COPY SemanticClip.Services/SemanticClip.Services.csproj SemanticClip.Services/
COPY SemanticClip.Client/SemanticClip.Client.csproj SemanticClip.Client/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build the API project
WORKDIR /src/SemanticClip.API
RUN dotnet build -c Release -o /app/build --no-restore

# Publish the API project
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the official .NET 9 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install FFmpeg (required by the application)
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Expose the port that the app runs on
EXPOSE 8080

# Set environment variables for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SemanticClip.API.dll"]