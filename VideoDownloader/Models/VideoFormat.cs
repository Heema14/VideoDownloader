using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VideoDownloader.Models
{
    public class VideoFormat
    {
        [JsonPropertyName("format_id")]
        public string? FormatId { get; set; }

        [JsonPropertyName("format_note")]
        public string? FormatNote { get; set; }

        [JsonPropertyName("ext")]
        public string? Extension { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("fps")]
        public double? Fps { get; set; }

        [JsonPropertyName("vcodec")]
        public string? VideoCodec { get; set; }

        [JsonPropertyName("acodec")]
        public string? AudioCodec { get; set; }

        [JsonPropertyName("filesize")]
        public long? FileSize { get; set; }


        public string DisplayName => $"{Height}p - {Extension}";
    }
}
