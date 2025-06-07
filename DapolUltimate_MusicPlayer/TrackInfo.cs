using System;

namespace DapolUltimate_MusicPlayer {
    public class TrackInfo {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public bool IsYouTube { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
