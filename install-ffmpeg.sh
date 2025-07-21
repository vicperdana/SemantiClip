#!/bin/bash

# Azure App Service post-deployment script to install FFmpeg
echo "Installing FFmpeg for Azure App Service..."

# Create a directory for FFmpeg binaries
mkdir -p /home/site/wwwroot/ffmpeg

# Download and extract FFmpeg static build for Linux
cd /tmp
wget -q https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz -O ffmpeg.tar.xz

if [ $? -eq 0 ]; then
    echo "FFmpeg downloaded successfully"
    tar -xf ffmpeg.tar.xz
    
    # Find the extracted directory
    FFMPEG_DIR=$(find . -name "ffmpeg-*-amd64-static" -type d | head -1)
    
    if [ -n "$FFMPEG_DIR" ]; then
        # Copy FFmpeg and FFprobe to our application directory
        cp "$FFMPEG_DIR/ffmpeg" /home/site/wwwroot/ffmpeg/
        cp "$FFMPEG_DIR/ffprobe" /home/site/wwwroot/ffmpeg/
        
        # Make them executable
        chmod +x /home/site/wwwroot/ffmpeg/ffmpeg
        chmod +x /home/site/wwwroot/ffmpeg/ffprobe
        
        echo "FFmpeg installed successfully to /home/site/wwwroot/ffmpeg/"
    else
        echo "Error: Could not find extracted FFmpeg directory"
    fi
else
    echo "Error: Failed to download FFmpeg"
fi

# Clean up
rm -rf /tmp/ffmpeg*

echo "FFmpeg installation script completed"
