using Microsoft.Win32;
using System.Windows;
using VideoDownloader.Models;
using VideoDownloader.Services;

namespace VideoDownloader
{
    public partial class PlaylistWindow : Window
    {
        private readonly YtDlpService _ytDlpService;
        private readonly List<PlaylistVideo> _playlistVideos = [];
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly DownloadHistoryService _downloadHistory = new();

        public PlaylistWindow(YtDlpService ytDlpService)
        {
            InitializeComponent();

            _ytDlpService = ytDlpService;
        }


        private async void AnalyzePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PlaylistUrlTextBox.Text.Trim()))
            {
                MessageBox.Show("Please enter a playlist URL.");
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                AnalyzePlaylistButton.IsEnabled = false;
                CancelPlaylistButton.IsEnabled = true;

                PlaylistVideosListBox.Items.Clear();

                PlaylistStatusTextBlock.Text = "Analyzing playlist...";

                var playlistData = await _ytDlpService.AnalyzePlaylistAsync(
                    PlaylistUrlTextBox.Text.Trim(),
                    _cancellationTokenSource.Token);

                MessageBox.Show("Done the analyzing.");
                PlaylistStatusTextBlock.Text = "Done the analyzing.";

                if (playlistData.Entries == null || playlistData.Entries.Length == 0)
                {
                    MessageBox.Show("No videos found in playlist.", "Playlist");
                    return;
                }

                _playlistVideos.Clear();
                foreach (var entry in playlistData.Entries)
                {
                    _playlistVideos.Add(new PlaylistVideo
                    {
                        Title = entry.Title ?? "No Title Available.",
                        Url = entry.Url,
                        IsSelected = true
                    });
                }

                PlaylistVideosListBox.ItemsSource = _playlistVideos;

                UpdatePlaylistVideoCount();
                UpdateDownloadButtonState();

                PlaylistStatusTextBlock.Text = $"Found {playlistData.Entries.Length} videos.";
            }
            catch (OperationCanceledException)
            {
                PlaylistStatusTextBlock.Text = "Playlist analysis cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Playlist Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                PlaylistStatusTextBlock.Text = "Failed to analyze playlist.";
            }
            finally
            {
                AnalyzePlaylistButton.IsEnabled = true;
                CancelPlaylistButton.IsEnabled = false;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }


        private void ChoosePlaylistFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Choose download folder."
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                PlaylistSavePathTextBox.Text = dialog.FolderName;

                UpdateDownloadButtonState();
            }
        }


        private async void DownloadPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedVideos = _playlistVideos
                .Where(v => v.IsSelected && !string.IsNullOrWhiteSpace(v.Url))
                .ToList();

            var historyEntries = new List<DownloadHistory>();

            if (selectedVideos.Count == 0)
            {
                MessageBox.Show("Please select at least one video.", "Playlist");
                return;
            }

            if (string.IsNullOrWhiteSpace(PlaylistSavePathTextBox.Text.Trim()))
            {
                MessageBox.Show("Please choose a save folder.", "Playlist");
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                DownloadPlaylistButton.IsEnabled = false;
                AnalyzePlaylistButton.IsEnabled = false;
                CancelPlaylistButton.IsEnabled = true;

                PlaylistStatusTextBlock.Text = $"Starting download... 0 / {selectedVideos.Count}";

                string formatSelector = GetPlaylistFormatSelector();

                var videoUrls = selectedVideos
                    .Where(video => !string.IsNullOrWhiteSpace(video.Url!.Trim()))
                    .Select(video => video.Url!)
                    .ToList();

                int currentVideoIndex = 0;
                var progress = new Progress<YoutubeDLSharp.DownloadProgress>(p =>
                {
                    if (currentVideoIndex >= selectedVideos.Count)
                        return;

                    var currentVideo = selectedVideos[currentVideoIndex];

                    currentVideo.Status = PlaylistVideoStatus.Downloading;

                    PlaylistVideosListBox.Items.Refresh();

                    PlaylistProgressBar.Value = p.Progress * 100;

                    string speed = !string.IsNullOrWhiteSpace(p.DownloadSpeed)
                        ? p.DownloadSpeed
                        : "--";

                    string eta = !string.IsNullOrWhiteSpace(p.ETA)
                        ? p.ETA
                        : "--";

                    PlaylistStatusTextBlock.Text =
                        $"Video: {currentVideoIndex + 1} / {selectedVideos.Count}\n" +
                        $"Current Video: {currentVideo.Title}\n" +
                        $"Downloading... {(p.Progress * 100):0}%\n" +
                        $"Speed: {speed}\n" +
                        $"Remaining: {eta}";
                });

                await _ytDlpService.DownloadPlaylistAsync(
                    videoUrls,
                    formatSelector,
                    PlaylistSavePathTextBox.Text,
                    progress,
                    (int index, bool success) =>
                    {
                        var video = selectedVideos[index];
                        if (success)
                        {
                            video.Status = PlaylistVideoStatus.Completed;
                        }
                        else
                        {
                            video.Status = PlaylistVideoStatus.Failed;
                        }

                        historyEntries.Add(new DownloadHistory
                        {
                            Title = video.Title!,
                            Url = video.Url!,
                            Status = success ? "Completed" : "Failed",
                            SavePath = PlaylistSavePathTextBox.Text.Trim(),
                            DownloadDate = DateTime.Now,
                            Source = "Playlist"
                        });

                        PlaylistVideosListBox.Items.Refresh();
                        currentVideoIndex = index + 1;

                        UpdateDownloadButtonState();
                    },

                    _cancellationTokenSource.Token);

                PlaylistProgressBar.Value = 100;

                int completedCount = selectedVideos.Count(video =>
                    video.Status == PlaylistVideoStatus.Completed);

                int failedCount = selectedVideos.Count(video =>
                    video.Status == PlaylistVideoStatus.Failed);

                PlaylistStatusTextBlock.Text =
                    $"Completed: {completedCount} | Failed: {failedCount}";

                UpdateDownloadButtonState();

                if (failedCount > 0)
                {
                    MessageBox.Show(
                        $"Download finished.\n\nCompleted: {completedCount}\nFailed: {failedCount}\n\nYou can use 'Retry Failed' to try the failed videos again.",
                        "Download Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(
                        $"All videos downloaded successfully.\n\nCompleted: {completedCount}",
                        "Download Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (OperationCanceledException)
            {
                PlaylistStatusTextBlock.Text = "Playlist download cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Playlist Download Error", MessageBoxButton.OK, MessageBoxImage.Error);

                PlaylistStatusTextBlock.Text = "Playlist download failed.";
            }
            finally
            {
                try
                {
                    foreach (var entry in historyEntries)
                    {
                        await _downloadHistory.AddEntryAsync(entry);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Could not save download history:\n{ex.Message}",
                        "History Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                AnalyzePlaylistButton.IsEnabled = true;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                UpdateDownloadButtonState();
            }
        }


        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var video in _playlistVideos)
                video.IsSelected = true;

            PlaylistVideosListBox.Items.Refresh();

            UpdatePlaylistVideoCount();
            UpdateDownloadButtonState();
        }


        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var video in _playlistVideos)
                video.IsSelected = false;

            PlaylistVideosListBox.Items.Refresh();

            UpdatePlaylistVideoCount();
            UpdateDownloadButtonState();
        }


        private void CancelPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }


        private void PlaylistQualityComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateDownloadButtonState();
        }


        private async void RetryFailedButton_Click(object sender, RoutedEventArgs e)
        {
            var failedVideos = _playlistVideos
                .Where(video =>
                    video.Status == PlaylistVideoStatus.Failed)
                .ToList();

            var historyEntries = new List<DownloadHistory>();

            if (failedVideos.Count == 0)
            {
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                RetryFailedButton.IsEnabled = false;
                DownloadPlaylistButton.IsEnabled = false;
                AnalyzePlaylistButton.IsEnabled = false;
                CancelPlaylistButton.IsEnabled = true;

                int retryCurrentIndex = 0;

                PlaylistStatusTextBlock.Text =
                    $"Starting retry... 0 / {failedVideos.Count}";

                string formatSelector = GetPlaylistFormatSelector();

                var failedUrls = failedVideos
                    .Where(video =>
                        !string.IsNullOrWhiteSpace(video.Url))
                    .Select(video => video.Url!)
                    .ToList();

                var retryProgress = new Progress<YoutubeDLSharp.DownloadProgress>(
                    p =>
                    {
                        if (retryCurrentIndex >= failedVideos.Count)
                        {
                            return;
                        }

                        var currentVideo = failedVideos[retryCurrentIndex];

                        currentVideo.Status = PlaylistVideoStatus.Downloading;

                        PlaylistVideosListBox.Items.Refresh();

                        PlaylistProgressBar.Value = p.Progress * 100;

                        string speed =
                            !string.IsNullOrWhiteSpace(p.DownloadSpeed)
                                ? p.DownloadSpeed
                                : "--";

                        string eta =
                            !string.IsNullOrWhiteSpace(p.ETA)
                                ? p.ETA
                                : "--";

                        PlaylistStatusTextBlock.Text =
                            $"Retrying: {retryCurrentIndex + 1} / {failedVideos.Count}\n" +
                            $"Current Video: {currentVideo.Title}\n" +
                            $"Downloading... {(p.Progress * 100):0}%\n" +
                            $"Speed: {speed}\n" +
                            $"Remaining: {eta}";
                    });

                await _ytDlpService.DownloadPlaylistAsync(
                    failedUrls,
                    formatSelector,
                    PlaylistSavePathTextBox.Text,
                    retryProgress,
                    (int index, bool success) =>
                    {
                        var video = failedVideos[index];
                        if (success)
                        {
                            video.Status = PlaylistVideoStatus.Completed;
                        }
                        else
                        {
                            video.Status = PlaylistVideoStatus.Failed;
                        }

                        historyEntries.Add(new DownloadHistory
                        {
                            Title = video.Title!,
                            Url = video.Url!,
                            Status = success ? "Completed" : "Failed",
                            SavePath = PlaylistSavePathTextBox.Text.Trim(),
                            DownloadDate = DateTime.Now,
                            Source = "Playlist Retry"
                        });

                        PlaylistVideosListBox.Items.Refresh();

                        retryCurrentIndex = index + 1;
                    },
                    _cancellationTokenSource.Token);

                PlaylistProgressBar.Value = 100;

                int remainingFailedVideos = _playlistVideos.Count(
                    video =>
                        video.Status == PlaylistVideoStatus.Failed);

                if (remainingFailedVideos == 0)
                {
                    PlaylistStatusTextBlock.Text =
                        "All failed videos were downloaded successfully.";
                }
                else
                {
                    PlaylistStatusTextBlock.Text =
                        $"{remainingFailedVideos} video(s) still failed.";
                }
            }
            catch (OperationCanceledException)
            {
                PlaylistStatusTextBlock.Text = "Retry cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Retry Error", MessageBoxButton.OK, MessageBoxImage.Error);

                PlaylistStatusTextBlock.Text = "Retry failed.";
            }
            finally
            {
                try
                {
                    foreach (var entry in historyEntries)
                        await _downloadHistory.AddEntryAsync(entry);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Could not save download history:\n{ex.Message}",
                        "History Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                CancelPlaylistButton.IsEnabled = false;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                UpdateDownloadButtonState();
            }
        }


        private void PlaylistVideoCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            PlaylistVideosListBox.Items.Refresh();
            UpdatePlaylistVideoCount();
            UpdateDownloadButtonState();
        }


        private void UpdateDownloadButtonState()
        {
            if (PlaylistSavePathTextBox == null ||
                PlaylistQualityComboBox == null ||
                DownloadPlaylistButton == null)
            {
                return;
            }

            bool hasSelectedVideos = _playlistVideos.Any(video => video.IsSelected);

            bool hasQuality = PlaylistQualityComboBox.SelectedIndex >= 0;

            bool hasSavePath = !string.IsNullOrWhiteSpace(PlaylistSavePathTextBox.Text);

            bool hasFailedVideos = _playlistVideos.Any(video =>
            video.Status == PlaylistVideoStatus.Failed);

            DownloadPlaylistButton.IsEnabled = hasSelectedVideos && hasQuality && hasSavePath;
            RetryFailedButton.IsEnabled = hasFailedVideos;
        }


        private string GetPlaylistFormatSelector()
        {
            return PlaylistQualityComboBox.SelectedIndex switch
            {
                0 => "bv*+ba/b",
                1 => "bv*[height<=2160]+ba/b",
                2 => "bv*[height<=1440]+ba/b",
                3 => "bv*[height<=1080]+ba/b",
                4 => "bv*[height<=720]+ba/b",
                5 => "bv*[height<=480]+ba/b",
                6 => "bv*[height<=360]+ba/b",
                _ => "bv*+ba/b"
            };
        }


        private void UpdatePlaylistVideoCount()
        {
            int totalVideos = _playlistVideos.Count;
            int selectedVideos = _playlistVideos.Count(v => v.IsSelected);

            PlaylistVideoCountTextBlock.Text =
                $"Videos: {totalVideos} | Selected: {selectedVideos}";
        }


    }
}


//playList1 7 Videos
// https://youtube.com/playlist?list=PL2ZpoDfD4b3Q0V8HbJaT4aiaD6OtP-nHi&si=F6G_0z2gT_Ppf2js

// playlist2 flutter 100 Videos
// https://youtube.com/playlist?list=PLw6Y5u47CYq47oDw63bMqkq06fjuoK_GJ&si=8SR9ytoGPSspBEmd