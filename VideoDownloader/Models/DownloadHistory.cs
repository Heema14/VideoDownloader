using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoDownloader.Models
{
    public class DownloadHistory
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SavePath { get; set; } = string.Empty;
        public DateTime DownloadDate { get; set; } = DateTime.Now;
        public string Source { get; set; } = string.Empty;
    }
}
