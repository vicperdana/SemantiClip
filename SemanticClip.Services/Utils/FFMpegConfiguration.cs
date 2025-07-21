using Xabe.FFmpeg;
using Microsoft.Extensions.Logging;

namespace SemanticClip.Services.Utils;

public static class FFMpegConfiguration
{
    private static bool _isConfigured = false;
    private static readonly object _lock = new object();

    public static void ConfigureFFMpeg(ILogger? logger = null)
    {
        if (_isConfigured)
            return;

        lock (_lock)
        {
            if (_isConfigured)
                return;

            try
            {
                logger?.LogInformation("Configuring Xabe.FFmpeg");
                
                // Try to find FFmpeg in common locations
                var currentDirectory = AppContext.BaseDirectory;
                
                // Check for common locations where FFmpeg binaries might be placed
                var possiblePaths = new[]
                {
                    Path.Combine(currentDirectory, "ffmpeg"),
                    Path.Combine(currentDirectory, "bin", "ffmpeg"),
                    "/usr/bin/ffmpeg", // Common on Linux
                    "/usr/local/bin/ffmpeg" // Alternative on Linux
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        var directory = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            FFmpeg.SetExecutablesPath(directory);
                            logger?.LogInformation("FFmpeg configured to use binaries at: {Path}", directory);
                            _isConfigured = true;
                            return;
                        }
                    }
                }
                
                // If no FFmpeg found, log warning but continue (system might have it in PATH)
                logger?.LogWarning("FFmpeg binaries not found in expected locations. Relying on system installation.");
                _isConfigured = true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to configure Xabe.FFmpeg. Video processing may not work.");
                throw new InvalidOperationException("Xabe.FFmpeg could not be configured.", ex);
            }
        }
    }

    public static Task EnsureFFMpegAsync(ILogger? logger = null)
    {
        ConfigureFFMpeg(logger);
        
        try
        {
            logger?.LogInformation("Xabe.FFmpeg configuration completed");
            // No need to download anything if already configured in Program.cs
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to ensure FFmpeg availability");
            throw;
        }
    }
}
