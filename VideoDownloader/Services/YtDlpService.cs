using System.IO;
using VideoDownloader.Models;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace VideoDownloader.Services;

public class YtDlpService
{
    private readonly string _ytDlpPath;

    public YtDlpService()
    {
        _ytDlpPath = Path.Combine(
            AppContext.BaseDirectory,
            "Tools",
            "yt-dlp.exe"
        );
    }


    public async Task EnsureToolsExistAsync()
    {
        string toolsFolder = Path.Combine(AppContext.BaseDirectory, "Tools");

        if (!Directory.Exists(toolsFolder))
        {
            Directory.CreateDirectory(toolsFolder);
        }

        if (!File.Exists(Path.Combine(toolsFolder, "yt-dlp.exe")))
        {
            await YoutubeDLSharp.Utils.DownloadYtDlp(toolsFolder);
        }

        if (!File.Exists(Path.Combine(toolsFolder, "ffmpeg.exe")))
        {
            await YoutubeDLSharp.Utils.DownloadFFmpeg(toolsFolder);
        }

        if (!File.Exists(Path.Combine(toolsFolder, "ffprobe.exe")))
        {
            await YoutubeDLSharp.Utils.DownloadFFprobe(toolsFolder);
        }
    }


    public List<VideoFormat> GetVideoFormats(VideoInfo videoInfo)
    {
        return videoInfo.Formats
            .Where(f =>
                f.Height.HasValue &&
                f.Height.Value > 0 &&
                f.VideoCodec != "none")
            .GroupBy(f => f.Height!.Value)
            .Select(g => g.First())
            .OrderByDescending(f => f.Height)
            .ToList();
    }


    public async Task<VideoInfo> GetVideoInfoWithYoutubeDLSharpAsync(string url, CancellationToken cancellationToken)
    {
        var ytdl = new YoutubeDL
        {
            YoutubeDLPath = Path.Combine(AppContext.BaseDirectory, "Tools", "yt-dlp.exe"),

            FFmpegPath = Path.Combine(AppContext.BaseDirectory, "Tools", "ffmpeg.exe")
        };

        var result = await ytdl.RunVideoDataFetch(url, ct: cancellationToken);

        if (!result.Success || result.Data == null)
        {
            string error = string.Join(Environment.NewLine, result.ErrorOutput);

            throw new Exception(
                string.IsNullOrWhiteSpace(error)
                    ? "Failed to analyze video."
                    : error);
        }

        var data = result.Data;

        var videoInfo = new VideoInfo
        {
            Title = data.Title,
            WebpageUrl = data.WebpageUrl,
            Duration = data.Duration ?? 0,
            Thumbnail = data.Thumbnail
        };

        foreach (var format in data.Formats)
        {
            videoInfo.Formats.Add(new VideoFormat
            {
                FormatId = format.FormatId,
                FormatNote = format.FormatNote,
                Extension = format.Extension,
                Height = format.Height,
                Width = format.Width,
                Fps = format.FrameRate,
                VideoCodec = format.VideoCodec,
                AudioCodec = format.AudioCodec,
                FileSize = format.FileSize
            });
        }

        return videoInfo;
    }


    public async Task<YoutubeDLSharp.Metadata.VideoData> AnalyzePlaylistAsync(
    string url,
    CancellationToken cancellationToken)
    {
        var ytdl = new YoutubeDL
        {
            YoutubeDLPath = Path.Combine(
                AppContext.BaseDirectory,
                "Tools",
                "yt-dlp.exe"),

            FFmpegPath = Path.Combine(
                AppContext.BaseDirectory,
                "Tools",
                "ffmpeg.exe")
        };

        var result = await ytdl.RunVideoDataFetch(url, ct: cancellationToken);

        if (!result.Success || result.Data == null)
        {
            string error = string.Join(Environment.NewLine, result.ErrorOutput);

            throw new Exception(
                string.IsNullOrWhiteSpace(error)
                    ? "Failed to analyze playlist."
                    : error);
        }

        return result.Data;
    }


    public async Task DownloadVideoAsync(string url,
      string formatId,
      string outputFolder,
      IProgress<YoutubeDLSharp.DownloadProgress>? progress,
      CancellationToken cancellationToken)
    {
        var ytdl = new YoutubeDL
        {
            YoutubeDLPath = Path.Combine(AppContext.BaseDirectory, "Tools", "yt-dlp.exe"),
            FFmpegPath = Path.Combine(AppContext.BaseDirectory, "Tools", "ffmpeg.exe"),
            OutputFolder = outputFolder
        };

        string formatSelector = $"{formatId}+bestaudio/best";

        var progressHandler = new Progress<YoutubeDLSharp.DownloadProgress>(p =>
        {
            progress?.Report(p);
        });

        var result = await ytdl.RunVideoDownload(
            url,
            format: formatSelector,
            progress: progressHandler,
            ct: cancellationToken
        );

        if (!result.Success)
        {
            string error = result.ErrorOutput != null && result.ErrorOutput.Length > 0
                ? string.Join(Environment.NewLine, result.ErrorOutput)
                : "Download failed.";

            throw new Exception(error);
        }
    }


    public async Task DownloadPlaylistAsync(
    IEnumerable<string> videoUrls,
    string formatSelector,
    string outputFolder,
    IProgress<YoutubeDLSharp.DownloadProgress>? progress,
    Action<int, bool>? videoCompleted,
    CancellationToken cancellationToken)
    {
        var ytdl = new YoutubeDL
        {
            YoutubeDLPath = Path.Combine(
                AppContext.BaseDirectory,
                "Tools",
                "yt-dlp.exe"),

            FFmpegPath = Path.Combine(
                AppContext.BaseDirectory,
                "Tools",
                "ffmpeg.exe"),

            OutputFolder = outputFolder,
            OutputFileTemplate = "%(title)s.%(ext)s"
        };

        int videoIndex = 0;
        foreach (var videoUrl in videoUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var progressHandler = new Progress<YoutubeDLSharp.DownloadProgress>(p =>
            {
                progress?.Report(p);
            });

            var options = new OptionSet
            {
                MergeOutputFormat = YoutubeDLSharp.Options.DownloadMergeFormat.Mp4,
                Retries = 20,
                FragmentRetries = 20,
                SocketTimeout = 30,
            };

            var result = await ytdl.RunVideoDownload(
                videoUrl,
                format: formatSelector,
                progress: progressHandler,
                ct: cancellationToken,
                overrideOptions: options
                );


            if (!result.Success)
            {
                videoCompleted?.Invoke(videoIndex, false);
                videoIndex++;
                continue;
            }


            videoCompleted?.Invoke(videoIndex, true);
            videoIndex++;
        }
    }


}
