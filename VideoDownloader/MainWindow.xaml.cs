using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using VideoDownloader.Models;
using VideoDownloader.Services;

namespace VideoDownloader
{
    public partial class MainWindow : Window
    {
        private readonly YtDlpService _ytDlpService;
        private CancellationTokenSource? _cancellationTokenSource;
        private List<VideoFormat> _availableFormats = [];
        private readonly DownloadHistoryService _downloadHistoryService = new();

        public MainWindow()
        {
            InitializeComponent();

            _ytDlpService = new YtDlpService();
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                AnalyzeButton.IsEnabled = false;

                StatusTextBlock.Text = "Checking and downloading required tools (yt-dlp & ffmpeg)... Please wait.";

                await _ytDlpService.EnsureToolsExistAsync();

                StatusTextBlock.Text = "Status: Ready";
                AnalyzeButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Failed to load download tools.";
                MessageBox.Show($"Could not download required tools:\n" +
                    $"{ex.Message}\n\n" +
                    $"Please check your internet connection and restart the app.",
                    "Tools Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }


        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Please enter a video URL.");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                AnalyzeButton.IsEnabled = false;
                CancelButton.IsEnabled = true;

                StatusTextBlock.Text = "Analyzing URL...";

                VideoInfo videoInfo = await _ytDlpService.GetVideoInfoWithYoutubeDLSharpAsync(url, _cancellationTokenSource.Token);

                MessageBox.Show("Done at Analyzing.", "Analyzing", MessageBoxButton.OK, MessageBoxImage.Information);

                StatusTextBlock.Text = "Done at Analyzing.";

                _availableFormats = _ytDlpService.GetVideoFormats(videoInfo);

                TitleTextBlock.Text = videoInfo.Title ?? "Unknown title!";

                QualityComboBox.SelectedIndex = -1;

                QualityComboBox.Items.Clear();

                foreach (VideoFormat format in _availableFormats)
                {
                    QualityComboBox.Items.Add(format.DisplayName);
                }

                if (QualityComboBox.Items.Count > 0)
                {
                    QualityComboBox.SelectedIndex = 0;
                }

                DownloadButton.IsEnabled =
                    QualityComboBox.SelectedIndex >= 0
                    &&
                    !string.IsNullOrWhiteSpace(SavePathTextBox.Text);

            }
            catch (OperationCanceledException)
            {
                StatusTextBlock.Text = "The Analyze is canceld!";
            }
            catch (TimeoutException ex)
            {
                StatusTextBlock.Text = "Timeout Analyzed, Internet is bad!";

                MessageBox.Show(
                    ex.Message,
                    "Timeout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "That was proplem when Analyze!";

                MessageBox.Show(
                    $"An error occurred:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                AnalyzeButton.IsEnabled = true;
                CancelButton.IsEnabled = false;

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }


        private void ChooseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Choose download folder"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                SavePathTextBox.Text = dialog.FolderName;
                StatusTextBlock.Text = "Download folder selected.";

                DownloadButton.IsEnabled = QualityComboBox.SelectedIndex >= 0;
            }

        }


        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UrlTextBox.Text))
            {
                MessageBox.Show("Please enter a video URL.");
                return;
            }

            if (QualityComboBox.SelectedIndex < 0 ||
                QualityComboBox.SelectedIndex >= _availableFormats.Count)
            {
                MessageBox.Show("Please select a video quality.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SavePathTextBox.Text))
            {
                MessageBox.Show("Please choose a download folder.");
                return;
            }

            VideoFormat selectedFormat = _availableFormats[QualityComboBox.SelectedIndex];

            string videoUrl = UrlTextBox.Text.Trim();
            string savePath = SavePathTextBox.Text.Trim();
            string videoTitle = string.IsNullOrWhiteSpace(TitleTextBlock.Text)
                ? videoUrl
                : TitleTextBlock.Text.Trim();

            string downloadStatus = "Failed";

            _cancellationTokenSource = new CancellationTokenSource();

            var progress = new Progress<YoutubeDLSharp.DownloadProgress>(p =>
            {
                DownloadProgressBar.Value = p.Progress * 100;

                string speed = !string.IsNullOrWhiteSpace(p.DownloadSpeed)
                        ? p.DownloadSpeed
                        : "--";

                string eta = !string.IsNullOrWhiteSpace(p.ETA)
                        ? p.ETA
                        : "--";

                string sizeDisplay = "--";

                if (!string.IsNullOrWhiteSpace(p.Data))
                {
                    // Get defult size of video 
                    var sizeMatch = Regex.Match(
                        p.Data,
                        @"([0-9\.]+\s*[a-zA-Z]+)\s+of\s+([0-9\.]+\s*[a-zA-Z]+)");

                    if (sizeMatch.Success)
                    {
                        string currentDownloaded = sizeMatch.Groups[1].Value;
                        string totalSize = sizeMatch.Groups[2].Value;
                        sizeDisplay = $"{currentDownloaded} / {totalSize}";
                    }
                }

                if (sizeDisplay == "--" &&
                    !string.IsNullOrWhiteSpace(p.TotalDownloadSize))
                {
                    string totalSize = p.TotalDownloadSize;
                    var match = Regex.Match(totalSize, @"([0-9\.]+)");

                    if (match.Success &&
                        double.TryParse(match.Value, out double totalNum))
                    {
                        double currentNum = totalNum * p.Progress;
                        string unit = totalSize.Replace(match.Value, "").Trim();
                        sizeDisplay = $"{currentNum:0.0} {unit} / {totalSize}";
                    }
                    else
                    {
                        sizeDisplay = p.TotalDownloadSize;
                    }
                }

                StatusTextBlock.Text =
                    $"Downloading... {(p.Progress * 100):0}%\n" +
                    $"Speed: {speed}\n" +
                    $"Remaining: {eta}\n" +
                    $"Size: {sizeDisplay}";
            });

            try
            {
                DownloadButton.IsEnabled = false;
                AnalyzeButton.IsEnabled = false;
                CancelButton.IsEnabled = true;

                StatusTextBlock.Text = $"Downloading {selectedFormat.DisplayName}...";

                await _ytDlpService.DownloadVideoAsync(
                    videoUrl,
                    selectedFormat.FormatId!,
                    savePath,
                    progress,
                    _cancellationTokenSource.Token);

                DownloadProgressBar.Value = 100;
                StatusTextBlock.Text = "Download completed.";

                downloadStatus = "Completed";

                MessageBox.Show(
                    "Video downloaded successfully!",
                    "Download Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                downloadStatus = "Cancelled";
                StatusTextBlock.Text = "Download cancelled.";
            }
            catch (Exception ex)
            {
                downloadStatus = "Failed";
                StatusTextBlock.Text = "Download failed.";

                MessageBox.Show(
                    ex.Message,
                    "Download Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                try
                {
                    await _downloadHistoryService.AddEntryAsync(
                        new DownloadHistory
                        {
                            Title = videoTitle,
                            Url = videoUrl,
                            Status = downloadStatus,
                            SavePath = savePath,
                            DownloadDate = DateTime.Now,
                            Source = "SingleVideo"
                        });
                }
                catch (Exception historyException)
                {
                    MessageBox.Show(
                        $"The download result could not be saved to history.\n\n{historyException.Message}",
                        "History Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                DownloadButton.IsEnabled = true;
                AnalyzeButton.IsEnabled = true;
                CancelButton.IsEnabled = false;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }


        private void QualityComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int selectedIndex = QualityComboBox.SelectedIndex;

            if (selectedIndex < 0 || selectedIndex >= _availableFormats.Count)
            {
                return;
            }

            DownloadButton.IsEnabled = !string.IsNullOrWhiteSpace(SavePathTextBox.Text);


            //VideoFormat selectedFormat = _availableFormats[selectedIndex];
            //MessageBox.Show(
            //    $"Format ID: {selectedFormat.FormatId}\n" +
            //    $"Quality: {selectedFormat.Height}p\n" +
            //    $"Extension: {selectedFormat.Extension}",
            //    "Selected Format");

        }


        private void PlaylistDownloaderButton_Click(object sender, RoutedEventArgs e)
        {
            PlaylistWindow playlistWindow = new PlaylistWindow(_ytDlpService);
            playlistWindow.Owner = this;
            playlistWindow.Show();
        }


        private void DownloadHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow historyWindow = new()
            {
                Owner = this
            };
            historyWindow.ShowDialog();
        }

    }
}


// اصالة اغنية
//https://youtu.be/kvtmDvYvm-8?si=qd_y3WzVFZOySkdH
//حمزة نمرة
//https://youtu.be/jTp4XSNee7Q?si=iGE8xZ6D8v4FxrEk
