using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace DapolUltimate_MusicPlayer {
    public partial class LoginWindow : Window {
        private readonly OracleDbService dbService = new OracleDbService();
        public int? UserId { get; private set; }

        public LoginWindow() {
            InitializeComponent();

            var theme = Properties.Settings.Default.SelectedTheme;
            if (string.IsNullOrEmpty(theme)) theme = "Aero";
            ThemeHelper.ApplyTheme(Resources.MergedDictionaries, theme);
        }

        private void Login_Click(object sender, RoutedEventArgs e) {
            var user = dbService.GetUserByUsername(UsernameBox.Text.Trim());
            var hash = ComputeHash(PasswordBox.Password);
            if (user != null && user.PasswordHash == hash) {
                UserId = user.Id;
                DialogResult = true;
            } else {
                MessageBox.Show("Invalid credentials");
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrWhiteSpace(UsernameBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password)) {
                MessageBox.Show("Enter username and password");
                return;
            }
            if (UsernameBox.Text.Trim().Length < 3) {
                MessageBox.Show("Username too short");
                return;
            }
            if (PasswordBox.Password.Length < 6) {
                MessageBox.Show("Password too short");
                return;
            }
            if (dbService.GetUserByUsername(UsernameBox.Text.Trim()) != null) {
                MessageBox.Show("Username already exists");
                return;
            }
            var hash = ComputeHash(PasswordBox.Password);
            try {
                UserId = dbService.RegisterUser(UsernameBox.Text.Trim(), hash);
                DialogResult = true;
            } catch (Exception ex) {
                MessageBox.Show($"Registration error: {ex.Message}");
            }
        }

        private string ComputeHash(string s) {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
