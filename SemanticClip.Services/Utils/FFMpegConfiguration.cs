using FFMpegCore;
using FFMpegCore.Helpers;
using Microsoft.Extensions.Logging;

namespace SemanticClip.Services.Utils;

public static class FFMpegConfiguration
{
    private static bool _isConfigured = false;
    private static readonly object _lock = new object();

    public static void ConfigureFFMpeg(ILogger? logger = null)
    {
        lock (_lock)
        {
            if (_isConfigured)
                return;

            try
            {
                // Try to use embedded binaries first
                // FFMpegCore will automatically try to find FFmpeg in:
                // 1. Environment PATH
                // 2. Current directory
                // 3. Common installation directories
                
                logger?.LogInformation("Configuring FFMpegCore");
                
                // Set a reasonable timeout for operations
                GlobalFFOptions.Configure(new FFOptions
                {
                    BinaryFolder = "", // Let FFMpegCore auto-discover
                    TemporaryFilesFolder = Path.GetTempPath(),
                    WorkingDirectory = Path.GetTempPath()
                });

                // Verify FFmpeg is available
                var version = FFMpeg.GetVersion();
                logger?.LogInformation("FFmpeg configured successfully. Version: {Version}", version);
                
                _isConfigured = true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to configure FFmpeg. Video processing may not work.");
                // Don't throw here - let the individual operations handle the failure
            }
        }
    }
}
