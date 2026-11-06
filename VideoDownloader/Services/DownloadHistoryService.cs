using System.IO;
using System.Text.Json;
using VideoDownloader.Models;

namespace VideoDownloader.Services
{
    public class DownloadHistoryService
    {
        private readonly string _historyFilePath;

        public DownloadHistoryService()
        {
            string projectFolder = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

            string historyFolder = Path.Combine(projectFolder, "DHJSON");

            Directory.CreateDirectory(historyFolder);

            _historyFilePath = Path.Combine(historyFolder, "download-history.json");
        }

        public async Task<List<DownloadHistory>> GetHistoryAsync()
        {
            if (!File.Exists(_historyFilePath))
                return new List<DownloadHistory>();

            string json = await File.ReadAllTextAsync(_historyFilePath);

            return JsonSerializer.Deserialize<List<DownloadHistory>>(json)
                   ?? [];
        }

        public async Task AddEntryAsync(DownloadHistory entry)
        {
            List<DownloadHistory> history = await GetHistoryAsync();

            history.Insert(0, entry);

            string json = JsonSerializer.Serialize(
                history, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

            await File.WriteAllTextAsync(_historyFilePath, json);
        }


        public async Task<bool> RemoveEntryAsync(DownloadHistory entry)
        {
            var history = await GetHistoryAsync();

            int index = history.FindIndex(item =>
                item.Title == entry.Title &&
                item.Url == entry.Url &&
                item.Status == entry.Status &&
                item.SavePath == entry.SavePath &&
                item.DownloadDate == entry.DownloadDate &&
                item.Source == entry.Source);

            if (index == -1)
                return false;

            history.RemoveAt(index);

            string json = JsonSerializer.Serialize(
                history,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

            await File.WriteAllTextAsync(_historyFilePath, json);

            return true;
        }


    }
}
