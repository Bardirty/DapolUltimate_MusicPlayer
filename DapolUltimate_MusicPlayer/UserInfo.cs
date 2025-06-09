using System;

namespace DapolUltimate_MusicPlayer {
    public class UserInfo {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
