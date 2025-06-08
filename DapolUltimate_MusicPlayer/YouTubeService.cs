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
            var results = new List<Video>();
            try {
                await foreach (var result in _client.Search.GetVideosAsync(query)) {
                    var video = await _client.Videos.GetAsync(result.Id);
                    results.Add(video);

                    if (results.Count >= limit)
                        break;
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error searching YouTube: {ex.Message}");
            }
            return results;
        }



        public async Task<string> DownloadAudioAsync(string videoUrl, string savePath) {
            try {
                // Безопасно парсим VideoId из URL
                var videoId = VideoId.TryParse(videoUrl);
                if (videoId == null) {
                    Console.WriteLine("Invalid YouTube URL.");
                    return null;
                }

                var manifest = await _client.Videos.Streams.GetManifestAsync(videoId.Value);
                var streamInfo = manifest
                    .GetAudioOnlyStreams()
                    .Where(s => s.Container == Container.WebM || s.Container == Container.Mp4)
                    .OrderByDescending(s => s.Bitrate)
                    .FirstOrDefault();



                if (streamInfo == null) {
                    Console.WriteLine("No audio streams found.");
                    return null;
                }

                var extension = streamInfo.Container.Name;
                if (!savePath.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))
                    savePath = Path.ChangeExtension(savePath, extension);

                var dir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

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
