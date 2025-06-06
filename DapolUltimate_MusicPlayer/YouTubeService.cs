using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace DapolUltimate_MusicPlayer {
    public class YouTubeVideo {
        public string Id { get; set; }
        public string Title { get; set; }
    }

    public class YouTubeService {
        private readonly YoutubeClient _client = new YoutubeClient();

        public async Task<List<YouTubeVideo>> SearchVideosAsync(string query, int maxResults = 20) {
            var videos = await _client.Search.GetVideosAsync(query).CollectAsync(maxResults);
            return videos.Select(v => new YouTubeVideo { Id = v.Id.Value, Title = v.Title }).ToList();
        }

        public async Task<string> DownloadAudioAsync(string videoId, string savePath) {
            var manifest = await _client.Videos.Streams.GetManifestAsync(videoId);
            var streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            await _client.Videos.Streams.DownloadAsync(streamInfo, savePath);
            return savePath;
        }
    }
}
