using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Converter;

namespace DapolUltimate_MusicPlayer {
    public class YouTubeService {
        private readonly YoutubeClient _client;

        public YouTubeService() {
            _client = new YoutubeClient();
        }

        public async Task<List<Video>> SearchVideosAsync(string query, int limit = 20) {
            var results = new List<Video>();
            var asyncEnum = _client.Search.GetVideosAsync(query).GetAsyncEnumerator();

            try {
                while (await asyncEnum.MoveNextAsync()) {
                    var searchResult = asyncEnum.Current;
                    var fullVideo = await _client.Videos.GetAsync(searchResult.Id);
                    results.Add(fullVideo);
                    if (results.Count >= limit)
                        break;
                }
            }
            finally {
                await asyncEnum.DisposeAsync();
            }

            return results;
        }

        public async Task<string> DownloadAudioAsync(string videoUrl, string savePath) {
            try {
                var videoId = VideoId.Parse(videoUrl);
                var manifest = await _client.Videos.Streams.GetManifestAsync(videoId);

                var audioStreams = manifest.GetAudioOnlyStreams();
                var mp4Stream = audioStreams.Where(s => s.Container == Container.Mp4).GetWithHighestBitrate();
                if (mp4Stream != null) {
                    var extension = mp4Stream.Container.Name;
                    if (!savePath.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))
                        savePath = Path.ChangeExtension(savePath, extension);
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                    await _client.Videos.Streams.DownloadAsync(mp4Stream, savePath);
                    return savePath;
                }

                savePath = Path.ChangeExtension(savePath, "mp3");
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                await _client.Videos.DownloadAsync(videoId, savePath, builder => builder.SetContainer("mp3"));
                return savePath;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error downloading video: {ex.Message}");
                return null;
            }
        }
    }
}
