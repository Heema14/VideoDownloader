using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoDownloader.Models
{
    public class PlaylistVideo
    {
        public bool IsSelected { get; set; } = true;

        public string? Title { get; set; }

        public string? Url { get; set; }

        public PlaylistVideoStatus Status { get; set; } = PlaylistVideoStatus.Pending;

        public string StatusText
        {
            get
            {
                if (!IsSelected)
                {
                    return "--Not Selected";
                }

                return Status switch
                {
                    PlaylistVideoStatus.Pending => "--Pending",
                    PlaylistVideoStatus.Downloading => "--Downloading",
                    PlaylistVideoStatus.Completed => "--Completed",
                    PlaylistVideoStatus.Failed => "--Failed",
                    _ => "--Unknown"
                };
            }
        }
    }


    public enum PlaylistVideoStatus
    {
        Pending,
        Downloading,
        Completed,
        Failed
    }
}
