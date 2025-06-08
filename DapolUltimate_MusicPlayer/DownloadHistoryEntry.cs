using System;

namespace DapolUltimate_MusicPlayer {
    public class DownloadHistoryEntry {
        public int Id { get; set; }
        public int TrackId { get; set; }
        public string Source { get; set; }
        public DateTime DownloadedAt { get; set; }
    }
}
