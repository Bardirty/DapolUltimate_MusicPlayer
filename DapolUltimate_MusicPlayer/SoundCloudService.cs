using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DapolUltimate_MusicPlayer {
    public class SoundCloudTrack {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("user")]
        public SoundCloudUser User { get; set; }

        [JsonProperty("artwork_url")]
        public string ArtworkUrl { get; set; }

        [JsonProperty("stream_url")]
        public string StreamUrl { get; set; }

        [JsonProperty("downloadable")]
        public bool Downloadable { get; set; }

        [JsonProperty("duration")]
        public long Duration { get; set; }
    }

    public class SoundCloudUser {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }
    }

    public class SoundCloudService {
        private const string ClientId = "yNSW5UvBmb1A5j7qPUtIMuB9Itx3jsOC";
        private readonly HttpClient _httpClient;
        private string _accessToken;

        public SoundCloudService() {
            _httpClient = new HttpClient();
        }

        public async Task<bool> LoginAsync(string login, string password) {
            var authContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", login),
                new KeyValuePair<string, string>("password", password)
            });

            var response = await _httpClient.PostAsync(
                "https://api.soundcloud.com/oauth2/token",
                authContent);

            if (response.IsSuccessStatusCode) {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(json);
                _accessToken = result.access_token;
                return true;
            }
            return false;
        }

        public async Task<List<SoundCloudTrack>> GetFavoritesAsync() {
            var response = await _httpClient.GetAsync(
                $"https://api.soundcloud.com/me/favorites?oauth_token={_accessToken}");

            if (response.IsSuccessStatusCode) {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<SoundCloudTrack>>(json);
            }
            return new List<SoundCloudTrack>();
        }

        public async Task<string> GetStreamUrlAsync(SoundCloudTrack track) {
            var response = await _httpClient.GetAsync(
                $"{track.StreamUrl}?client_id={ClientId}&oauth_token={_accessToken}");

            if (response.IsSuccessStatusCode) {
                var json = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(json);
                return result.url;
            }
            return null;
        }

        public async Task<string> DownloadTrackAsync(SoundCloudTrack track, string savePath) {
            try {
                var streamUrl = await GetStreamUrlAsync(track);
                if (string.IsNullOrEmpty(streamUrl))
                    return null;

                var bytes = await _httpClient.GetByteArrayAsync(streamUrl);

                Directory.CreateDirectory(Path.GetDirectoryName(savePath));


                using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true)) {
                    await fileStream.WriteAsync(bytes, 0, bytes.Length);
                }

                return savePath;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error downloading track: {ex.Message}");
                return null;
            }
        }

        public async Task<List<SoundCloudTrack>> SearchTracksAsync(string query, int limit = 20) {
            try {
                var url = $"https://api-v2.soundcloud.com/search/tracks?q={Uri.EscapeDataString(query)}&client_id={ClientId}&limit={limit}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return new List<SoundCloudTrack>();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(json);

                return result.collection.ToObject<List<SoundCloudTrack>>();
            }
            catch {
                return new List<SoundCloudTrack>();
            }
        }
    }
}
