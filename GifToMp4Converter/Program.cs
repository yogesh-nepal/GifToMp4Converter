using System.Globalization;
using Xabe.FFmpeg;

namespace GifToMp4Converter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Determine the folder where the FFmpeg executables are located.
                // AppContext.BaseDirectory returns the path where the compiled application runs.
                string baseDir = AppContext.BaseDirectory;

                // Navigate up the directory tree to find the project root.
                // We use Parent repeatedly to go up four levels.
                // Alternative: hard-code the full path or use a configuration setting.
                //   Advantage: simpler code. Disadvantage: less portable if folder moves.
                string projectRoot = Directory.GetParent(baseDir)!.Parent!.Parent!.Parent!.FullName;

                // Construct the path to the FFmpeg executables folder.
                // If you installed FFmpeg in a system PATH, you could skip this and
                // allow the library to find FFmpeg automatically.
                string ffmpegPath = Path.Combine(projectRoot, "ffmpeg-master-latest-win64-gpl-shared", "bin");
                FFmpeg.SetExecutablesPath(ffmpegPath);

                // Define where input GIFs are located and where output MP4 files will be saved.
                string inputDirectory = @"C:\....\GIFS";
                string outputDirectory = @"C:\....\GIF_TO_MP4";

                // Ensure the output directory exists. If it doesn't, create it.
                // Alternative: throw an error if the folder is missing.
                //   Advantage: forces correct setup. Disadvantage: less user-friendly.
                Directory.CreateDirectory(outputDirectory);

                // Get a list of all GIF files in the input folder.
                string[] gifFiles = Directory.GetFiles(inputDirectory, "*.gif");
                int i = 1; // Counter to name output files sequentially.

                // Process each GIF file one by one.
                foreach (var gif in gifFiles)
                {
                    try
                    {
                        // Extract the file name without its extension (e.g., "sample").
                        string fileName = Path.GetFileNameWithoutExtension(gif);

                        // Build the output path, naming files as 1.mp4, 2.mp4, etc.
                        string outputPath = Path.Combine(outputDirectory, i + ".mp4");

                        // Read information about the input media (e.g., duration, streams).
                        IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(gif);

                        // Select the first video stream and set its codec to H.264, a widely
                        // supported format for MP4 files.
                        var videoStream = mediaInfo.VideoStreams.First()
                            .SetCodec(VideoCodec.h264);

                        if (videoStream == null)
                        {
                            // If no video stream is found, skip this file.
                            Console.WriteLine($"No video stream found in {gif}. Skipping.");
                            continue;
                        }

                        // Set a target frame rate of 30 frames per second.
                        videoStream.SetFramerate(30);

                        // Get the total duration of the GIF in seconds.
                        double originalDuration = mediaInfo.Duration.TotalSeconds;

                        // Determine if we need to pad the video to at least 4 seconds.
                        bool needsPadding = originalDuration < 4;

                        // Work out the frame dimensions and ensure they're even, as
                        // required by many video encoders.
                        int width = videoStream.Width;
                        int height = videoStream.Height;
                        if (width % 2 != 0) width++;
                        if (height % 2 != 0) height++;

                        if (needsPadding)
                        {
                            // Calculate how much padding is needed in seconds to reach 4s.
                            double padDuration = 4 - originalDuration;

                            // Build a complex filter to:
                            // 1) Reset timestamps and scale the GIF to desired size.
                            // 2) Create a black video segment of padDuration length.
                            // 3) Concatenate the GIF segment and black padding.
                            string filter = $"[0:v] setpts=PTS-STARTPTS, scale={width}:{height} [gif]; " +
                                            $"color=c=black:s={width}x{height}:d={padDuration.ToString("0.000", CultureInfo.InvariantCulture)} [pad]; " +
                                            $"[gif][pad] concat=n=2:v=1 [outv]";

                            // Build and configure the conversion command.
                            var conversion = FFmpeg.Conversions.New()
                                .AddStream(videoStream)
                                // Apply our filter chain.
                                .AddParameter($"-filter_complex \"{filter}\"")
                                .AddParameter("-map [outv]")
                                .AddParameter("-pix_fmt yuv420p")     // Ensures broad compatibility.
                                .AddParameter("-movflags +faststart")// Allows streaming playback.

                                // Add descriptive metadata to the resulting MP4. These tags can be seen
                                // in media players but are optional. You could skip metadata entirely.
                                .AddParameter("-metadata title=\"Title\"")
                                .AddParameter("-metadata artist=\"@artistName\"")
                                .AddParameter("-metadata album=\"Hits\"")
                                .AddParameter("-metadata genre=\"Internet Culture\"")
                                .AddParameter("-metadata comment=\"Comment\"")
                                .AddParameter("-metadata date=\"2011-06-09\"")
                                .AddParameter("-metadata copyright=\"© 2025 artistName\"")
                                .AddParameter("-metadata description=\"Based on everything\"")
                                .AddParameter("-metadata encoder=\"Media Encoder\"")
                                .AddParameter("-metadata language=\"en\"")
                                .AddParameter("-metadata rating=\"5.0\"")

                                // Limit the total duration to exactly 4 seconds.
                                // Alternative: let the video keep its natural length.
                                //   Advantage: simpler. Disadvantage: files shorter than 4s remain short.
                                .AddParameter("-t 4")

                                // Force output resolution.
                                .AddParameter($"-s {width}x{height}")

                                // Specify where the output should be saved.
                                .SetOutput(outputPath);

                            // Start the padding conversion.
                            await conversion.Start();
                        }
                        else
                        {
                            // For GIFs already at least 4s long, use a simpler conversion.
                            var conversion = FFmpeg.Conversions.New()
                                .AddStream(videoStream)
                                .AddParameter("-pix_fmt yuv420p")
                                .AddParameter("-movflags +faststart")
                                .AddParameter("-metadata title=\"Title\"")
                                .AddParameter("-metadata artist=\"@artistName\"")
                                .AddParameter("-metadata album=\"Hits\"")
                                .AddParameter("-metadata genre=\"Internet Culture\"")
                                .AddParameter("-metadata comment=\"Comment\"")
                                .AddParameter("-metadata date=\"2011-06-09\"")
                                .AddParameter("-metadata copyright=\"© 2025 artistName\"")
                                .AddParameter("-metadata description=\"Based on everything\"")
                                .AddParameter("-metadata encoder=\"Media Encoder\"")
                                .AddParameter("-metadata language=\"en\"")
                                .AddParameter("-metadata rating=\"5.0\"")
                                .AddParameter($"-s {width}x{height}")
                                .SetOutput(outputPath);

                            // Start the standard conversion.
                            await conversion.Start();
                        }

                        // Inform the user about the successful conversion.
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($" Converted: {fileName}.gif -> {fileName}.mp4");
                        Console.ResetColor();
                        i++; // Increment counter for next file name.
                    }
                    catch (Exception ex)
                    {
                        // Handle errors for individual files without stopping the whole process.
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed: {Path.GetFileName(gif)}");
                        Console.WriteLine($"   Reason: {ex.Message}");
                        Console.ResetColor();
                        i++;
                    }
                }

                // Notify that all files have been processed.
                Console.WriteLine("\n All conversions completed.");
            }
            catch (Exception globalEx)
            {
                // Catch any setup or unexpected errors.
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("🔥 Fatal setup error:");
                Console.WriteLine(globalEx.ToString());
                Console.ResetColor();
            }
        }
    }
}
