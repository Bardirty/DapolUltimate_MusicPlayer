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
            var asyncEnum = _client.Search.GetVideosAsync(query).GetAsyncEnumerator();

            try {
                while (await asyncEnum.MoveNextAsync()) {
                    var searchResult = asyncEnum.Current;

                    // �������� ����������� ����� �� ID
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
                var streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                await _client.Videos.Streams.DownloadAsync(streamInfo, savePath);

                return savePath;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error downloading video: {ex.Message}");
                return null;
            }
        }
    }
}
