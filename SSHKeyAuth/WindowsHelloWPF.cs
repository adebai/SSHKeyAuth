using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Windows.Security.Credentials.UI;

namespace SSHKeyAuth
{
    public partial class QMainWindow : Window
    {
        public ObservableCollection<string> Passphrases { get; set; } = new();
        public string SelectedPassphrase { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadPassphrases();
        }

        private void LoadPassphrases()
        {
            if (File.Exists("passphrases.txt"))
            {
                var lines = File.ReadAllLines("passphrases.txt");
                foreach (var line in lines)
                {
                    Passphrases.Add(line);
                }
            }
        }

        private async void Authenticate_Click(object sender, RoutedEventArgs e)
        {
            if (await AuthenticateUser())
            {
                MessageBox.Show($"Authenticated! Active passphrase: {SelectedPassphrase}");
            }
            else
            {
                MessageBox.Show("Authentication failed.");
            }
        }

        private async Task<bool> AuthenticateUser()
        {
            try
            {
                var result = await UserConsentVerifier.RequestVerificationAsync("Authenticate to access passphrase");
                return result == UserConsentVerificationResult.Verified;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Windows Hello unavailable: {ex.Message}");
                return false;
            }
        }

        private void AddPassphrase_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Enter new passphrase:", "Add Passphrase", "");
            if (!string.IsNullOrWhiteSpace(input) && !Passphrases.Contains(input))
            {
                Passphrases.Add(input);
                File.AppendAllLines("passphrases.txt", new[] { input });
            }
        }
    }
}
