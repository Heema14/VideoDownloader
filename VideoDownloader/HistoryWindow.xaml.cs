using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VideoDownloader.Models;
using VideoDownloader.Services;

namespace VideoDownloader
{
    public partial class HistoryWindow : Window
    {
        private readonly DownloadHistoryService _historyService = new();
        private List<DownloadHistory> _allHistory = [];

        public HistoryWindow()
        {
            InitializeComponent();
        }


        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            await LoadHistoryAsync();
        }


        private async Task HistoryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadHistoryAsync();
        }


        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadHistoryAsync();
        }


        private async Task LoadHistoryAsync()
        {
            try
            {
                _allHistory = await _historyService.GetHistoryAsync();
                ApplyStatusFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load download history:\n{ex.Message}",
                    "History Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyStatusFilter();
        }


        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not DownloadHistory entry)
                return;

            string folderPath = entry.SavePath;

            if (string.IsNullOrWhiteSpace(folderPath) ||
                !System.IO.Directory.Exists(folderPath))
            {
                MessageBox.Show(
                    "The saved folder could not be found.",
                    "Folder Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not open the folder:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private async void DeleteRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not DownloadHistory entry)
                return;


            var result = MessageBox.Show(
                $"Are you sure to delete this row? \n\n{entry.Title}\n\n That not delete video.",
                "Confirm Delete!",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                bool deleted = await _historyService.RemoveEntryAsync(entry);
                if (!deleted)
                {
                    MessageBox.Show(
                        "Not found row!",
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await LoadHistoryAsync();
                    return;
                }

                _allHistory.Remove(entry);
                ApplyStatusFilter();

                MessageBox.Show(
                    "Done, deleted row.",
                    "Done",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"That problem when delete:\n{ex.Message}",
                    "Wrong!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void SearchTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyStatusFilter();
        }


        private void ApplyStatusFilter()
        {
            if (HistoryDataGrid == null || StatusFilterComboBox == null || SearchTxt == null)
                return;

            string selectedStatus =
                (StatusFilterComboBox.SelectedItem as ComboBoxItem)?
                .Content?.ToString()
                ?? "All";

            string Searchtxt = SearchTxt.Text.Trim();

            var filteredHistory = _allHistory.Where(entry =>
             (selectedStatus == "All" || entry.Status == selectedStatus) &&
             (
                 string.IsNullOrWhiteSpace(Searchtxt) ||
                 (entry.Title?.Contains(Searchtxt, StringComparison.OrdinalIgnoreCase) ?? false) ||
                 (entry.Url?.Contains(Searchtxt, StringComparison.OrdinalIgnoreCase) ?? false) ||
                 (entry.Source?.Contains(Searchtxt, StringComparison.OrdinalIgnoreCase) ?? false)
             ))
         .ToList();

            HistoryDataGrid.ItemsSource = filteredHistory;

            HCountTxt.Text = $"Showing: {filteredHistory.Count} / {_allHistory.Count}";
        }


    }
}
