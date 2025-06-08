using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace DapolUltimate_MusicPlayer {
    public class YouTubeService {
        private readonly YoutubeClient _client;

        public YouTubeService() {
            _client = new YoutubeClient();
        }

        public async Task<List<Video>> SearchVideosAsync(string query, int limit = 20) {
            try {
                return await _client.Search.GetVideosAsync(query).CollectAsync(limit);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error searching YouTube: {ex.Message}");
                return new List<Video>();
            }
        }

        public async Task<string> DownloadAudioAsync(string videoUrl, string savePath) {
            try {
                var manifest = await _client.Videos.Streams.GetManifestAsync(VideoId.Parse(videoUrl));
                var streamInfo = manifest.GetAudioOnlyStreams()
                                         .OrderByDescending(s => s.Bitrate)
                                         .FirstOrDefault();
                if (streamInfo == null)
                    return null;

                var extension = streamInfo.Container.Name;
                if (!savePath.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))
                    savePath = Path.ChangeExtension(savePath, extension);

                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                await _client.Videos.Streams.DownloadAsync(streamInfo, savePath);
                return savePath;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error downloading from YouTube: {ex.Message}");
                return null;
            }
        }
    }
}
