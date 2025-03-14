using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using Windows.Security.Credentials.UI;
using System.Security.Cryptography;
using System.Text;
using NHotkey;
using NHotkey.Wpf;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Windows.Controls.Primitives;
using static System.Windows.Forms.DataFormats;

namespace SSHKeyAuth
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> Passphrases { get; set; } = new();
        //private string encryptionKey = EncryptionHelper.GetOrCreateEncryptionKey(); // TODO: Replace with a securely stored key
        private string currentHotkey = "Ctrl+0";
        private string selectedPassphraseFile = "selected_passphrase.txt";
        private IntPtr previousWindowHandle;
        private NotifyIcon notifyIcon = new NotifyIcon();
        private static string selectedPhrase;
        private bool isSettingHotkey = false;
        private Key firstKey;
        private Key secondKey;
        private DispatcherTimer hintResetTimer;


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadPassphrases();
            LoadSelectedPassphrase();
            LoadAutoPasteConfig();
            RegisterHotkey();
            TrackActiveWindow();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private const int SW_RESTORE = 9;

        // Delegate for window enumeration
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private void RegisterHotkey()
        {
            try
            {
                // Register the global hotkey (Default: Ctrl + 0)
                HotkeyManager.Current.AddOrReplace("AuthHotkey", Key.D0, ModifierKeys.Control, OnHotkeyPressed);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error registering hotkey: {ex.Message}");
            }
        }

        private static IntPtr FindWindowsHelloWindow()
        {
            IntPtr helloWindow = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                StringBuilder windowText = new StringBuilder(256);
                GetWindowText(hWnd, windowText, windowText.Capacity);

                string title = windowText.ToString();

                // Windows Hello sometimes has "Windows Security" or "Sign-in Options"
                if (title.Contains("Windows Security") || title.Contains("Sign-in Options"))
                {
                    helloWindow = hWnd;
                    return false; // Stop enumeration when found
                }

                return true; // Continue enumeration
            }, IntPtr.Zero);

            return helloWindow;
        }


        private void OnHotkeyPressed(object sender, HotkeyEventArgs e)
        {
            e.Handled = true; // Prevents hotkey from triggering multiple times
            Authenticate();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();


        private const int SW_MINIMIZE = 6;

        private static IntPtr lastActiveWindow = IntPtr.Zero;


        
        private static string GetWindowTitle(IntPtr hWnd)
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            if (GetWindowText(hWnd, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            return "Unknown Window";
        }
        private void TrackActiveWindow()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    IntPtr currentWindow = GetForegroundWindow();
                    if (currentWindow != IntPtr.Zero && currentWindow != lastActiveWindow)
                    {
                        lastActiveWindow = currentWindow;
                    }
                    Thread.Sleep(500); // Check every 500ms
                }
            });
        }


        public async void Authenticate()
        {
            IntPtr previousWindow = previousWindowHandle = lastActiveWindow; // Store the active window before authentication
            MainWindow handle = bringUp();
            object clipboardData;
            string dataType;
            Dictionary<string, object>

                        // Backup clipboard 
                        lBackup = new Dictionary<string, object>();
            System.Windows.IDataObject lDataObject = System.Windows.Clipboard.GetDataObject();
            String[] lFormats = lDataObject.GetFormats(false);
            try
            {
                foreach (var lFormat in lFormats)
                {
                    lBackup.Add(lFormat, lDataObject.GetData(lFormat, false));
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, display a message, etc.)
                Console.WriteLine("An error occurred: " + ex.Message);
            }


            string previousWindowName = GetWindowTitle(previousWindow);

            if (await AuthenticateUser())
            {
                handle.Close();
                string passphrase = DecryptPassphrase(GetActivePassphrase());

                // 
                if (AutoPasteCheckBox.IsChecked == true)
                {
                    System.Windows.Clipboard.SetText(passphrase);
                    await Task.Delay(500); // Small delay to allow the window to gain focus
                    RestorePreviousWindow(); // Ensure the right window is active
                    // Simulate Ctrl+V to paste
                    SendKeys.SendWait("^v");
                    lDataObject = new System.Windows.DataObject();
                    try
                    {
                        foreach (var lFormat in lFormats)
                        {
                            lDataObject.SetData(lFormat, lBackup[lFormat]);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception (e.g., log it, display a message, etc.)
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                    //This might be unnecessary
                    System.Windows.Clipboard.SetDataObject(lDataObject);

                    NotifyUser("Authentication Successful", $"Pasted {selectedPhrase} passphrase into \"{previousWindowName}\"", false);
                }
                else
                {
                    System.Windows.Clipboard.SetText(passphrase);
                    await Task.Delay(500); // Small delay to allow the window to gain focus
                    //RestorePreviousWindow(); // Ensure the right window is active
                    //System.Windows.Clipboard.Clear();
                    NotifyUser("Authentication Successful", "Passphrase copied to clipboard!", false);
                }
            }
            else
            {
                handle.Close();
                RestoreActiveWindow(); // Restore after authentication
                NotifyUser("Authentication Failed", "Windows Hello verification failed.", true);
            }
        }



        private static void MinimizeActiveWindow()
        {
            if (lastActiveWindow != IntPtr.Zero)
            {
                ShowWindow(lastActiveWindow, SW_MINIMIZE); // Minimize the active window
            }
        }

        private static void RestoreActiveWindow()
        {
            if (lastActiveWindow != IntPtr.Zero)
            {
                ShowWindow(lastActiveWindow, SW_RESTORE);  // Restore the active window
                SetForegroundWindow(lastActiveWindow);    // Bring it back to focus
            }
        }

        private MainWindow bringUp()
        {
            // Check if MainWindow is already open
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window is MainWindow mainWindow)
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal; // Restore if minimized
                    mainWindow.Activate(); // Bring the window to the foreground
                    mainWindow.Topmost = true; // Ensure it's on top
                    mainWindow.Topmost = false; // Reset Topmost so it doesn't stay always on top
                    mainWindow.Focus(); // Set focus to the window
                    //Thread.Sleep(1000); // Optional delay
                    return mainWindow;
                }
            }

            // If not open, create and show a new MainWindow
            MainWindow newWindow = new MainWindow();
            newWindow.Show();
            newWindow.WindowState = WindowState.Normal;
            newWindow.Activate(); // Bring the new window to the foreground
            newWindow.Topmost = true; // Ensure it's on top
            newWindow.Topmost = false; // Reset Topmost so it doesn't stay always on top
            newWindow.Focus(); // Set focus to the window
            //Thread.Sleep(1000); // Optional delay
            return newWindow;
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
                Logger.Log($"Authentication error: {ex.Message}");
                NotifyUser("Error", $"Windows Hello unavailable: {ex.Message}", true);
                return false;
            }
        }

        private void SetHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (isSettingHotkey) return; // Prevent multiple activations

            isSettingHotkey = true;
            firstKey = Key.None;
            secondKey = Key.None;
            HotkeyHintLabel.Text = "Press the first key (Ctrl, Alt, or Shift).";

            // Listen for keyboard input
            this.KeyDown += Hotkey_KeyDown;
        }

        private void Hotkey_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            HotkeyHintLabel.Text = "Press the first key (Ctrl, Alt, or Shift).";
            if (!isSettingHotkey) return;

            if (firstKey == Key.None)
            {
                // First key must be Ctrl, Alt, or Shift
                if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                    e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                    e.Key == Key.LeftShift || e.Key == Key.RightShift)
                {
                    firstKey = e.Key;
                    HotkeyHintLabel.Text = "Now press the second key (letters, numbers, or symbols).";
                }
                else
                {
                    HotkeyHintLabel.Text = "Invalid key. First key must be Ctrl, Alt, or Shift.";
                    return; // Stop here, do not proceed
                }
            }
            else if (secondKey == Key.None)
            {
                // Second key must NOT be a modifier key
                if (!IsModifierKey(e.Key))
                {
                    secondKey = e.Key;
                    UpdateHotkey(); // Save the new hotkey
                    HotkeyHintLabel.Text = "Hotkey changed!";
                    Task.Delay(5000).ContinueWith(_ => Dispatcher.Invoke(() =>
                        HotkeyHintLabel.Text = "Press `Set Hotkey` to set a new hotkey (one of the 2 keys has to be Ctrl/Alt/Shift)."));
                }
                else
                {
                    HotkeyHintLabel.Text = "Invalid key. Second key must be a letter, number, or symbol.";
                    return; // Stop here, do not proceed
                }
            }
        }

        // Helper method to check if the key is a modifier (Ctrl, Alt, Shift, Windows key)
        private bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LWin || key == Key.RWin; // Prevents Windows key from being set
        }


        private void UpdateHotkey()
        {
            string newHotkey = $"{firstKey}+{secondKey}";
            CurrentHotkeyLabel.Text = $"Current Hotkey: {newHotkey}";
            HotkeyHintLabel.Text = $"Hotkey changed to: {newHotkey}";

            isSettingHotkey = false;
            this.KeyDown -= Hotkey_KeyDown;

            // Reset hint after 5 seconds
            if (hintResetTimer == null)
            {
                hintResetTimer = new DispatcherTimer();
                hintResetTimer.Interval = TimeSpan.FromSeconds(5);
                hintResetTimer.Tick += (s, e) => { HotkeyHintLabel.Text = "Press `Set Hotkey` to set a new hotkey (one of the 2 keys has to be Ctrl/Alt/Shift)."; };
            }

            hintResetTimer.Start();
        }

        

        private void CaptureHotkey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ModifierKeys modifiers = Keyboard.Modifiers;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            string hotkey = $"{(modifiers.HasFlag(ModifierKeys.Control) ? "Ctrl+" : "")}" +
                            $"{(modifiers.HasFlag(ModifierKeys.Alt) ? "Alt+" : "")}" +
                            $"{(modifiers.HasFlag(ModifierKeys.Shift) ? "Shift+" : "")}" +
                            $"{key}";

            currentHotkey = hotkey;
            CurrentHotkeyLabel.Text = hotkey;

            // Unregister the old hotkey
            HotkeyManager.Current.Remove("AuthHotkey");

            // Register the new hotkey
            HotkeyManager.Current.AddOrReplace("AuthHotkey", key, modifiers, OnHotkeyPressed);

            PreviewKeyDown -= CaptureHotkey; // Stop capturing after setting hotkey
        }

        private void iNotifyUser(string title, string message, bool highPriority)
        {
            notifyIcon.Visible = true;
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = message + "!";
            notifyIcon.BalloonTipIcon = highPriority ? ToolTipIcon.Error : ToolTipIcon.Info;

            notifyIcon.ShowBalloonTip(3000);

            // Dispose notify icon after notification
            notifyIcon.BalloonTipClosed += (sender, e) => notifyIcon.Dispose();
        }

        private void NotifyUser(string title, string message, bool isTrue)
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show(); // Sends the notification to the Windows Notification Center
            iNotifyUser(title, message, isTrue);
        }

        private void LoadPassphrases()
        {
            if (File.Exists("passphrases.csv"))
            {
                var lines = File.ReadAllLines("passphrases.csv");
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        Passphrases.Add(parts[0]); // Display only the name in the UI
                    }
                }
            }
        }

        private void LoadSelectedPassphrase()
        {
            if (File.Exists(selectedPassphraseFile))
            {
                string selected = File.ReadAllText(selectedPassphraseFile);
                if (Passphrases.Contains(selected))
                {
                    PassphraseList.SelectedItem = selected;
                    selectedPhrase = selected;
                    SelectedPassphraseLabel.Content = selected;
                }
                else
                {
                    SelectedPassphraseLabel.Content = "No passphrase selected. Please click a passphrase.";
                }
            }
        }

        private void PassphraseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PassphraseList.SelectedItem is string selected)
            {
                File.WriteAllText(selectedPassphraseFile, selected);
                SelectedPassphraseLabel.Content = selected;
                selectedPhrase = selected;

            }
        }

        private string GetActivePassphrase()
        {
            LoadSelectedPassphrase();
            if (PassphraseList.SelectedItem is string selectedPassphrase)
            {
                try
                {
                    var lines = File.ReadAllLines("passphrases.csv");
                    foreach (var line in lines)
                    {
                        //Logger.Log($"Reading line: {line}");
                    }

                    string encryptedPassphrase = lines
                        .Where(line => line.StartsWith(selectedPassphrase + ","))
                        .Select(line => line.Split(',')[1])
                        .FirstOrDefault();

                    if (string.IsNullOrEmpty(encryptedPassphrase))
                    {
                        NotifyUser("Error", $"No matching passphrase found for: {selectedPassphrase}", true);
                        Logger.Log($"No matching passphrase found for: {selectedPassphrase}");
                    }
                    return encryptedPassphrase;
                }
                catch (Exception ex)
                {
                    NotifyUser("Error", $"No matching passphrase found for: {selectedPassphrase}", true);
                    Logger.Log($"Error reading passphrase: {ex.Message}");
                    return string.Empty;
                }
            }
            NotifyUser("Error", "No selected passphrase found.", true);
            return string.Empty;
        }


        private void RestorePreviousWindow()
        {
            if (previousWindowHandle != IntPtr.Zero)
            {
                //ShowWindow(previousWindowHandle, SW_RESTORE); // Restore if minimized
                SetForegroundWindow(previousWindowHandle);    // Bring to front
            }
        }

        private void AddPassphrase_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Enter new passphrase:", "Add Passphrase", "");
            if (!string.IsNullOrWhiteSpace(input))
            {
                string name = Microsoft.VisualBasic.Interaction.InputBox("Enter a name for this passphrase:", "Passphrase Name", "");
                if (!string.IsNullOrWhiteSpace(name) && !Passphrases.Contains(name))
                {
                    string encrypted = EncryptPassphrase(input);
                    File.AppendAllLines("passphrases.csv", new[] { $"{name},{encrypted}" });
                    Passphrases.Add(name);
                }
            }
        }

        private void RemovePassphrase_Click(object sender, RoutedEventArgs e)
        {
            if (PassphraseList.SelectedItem is string selectedPassphrase)
            {
                Passphrases.Remove(selectedPassphrase);
                var lines = File.ReadAllLines("passphrases.csv").Where(l => !l.StartsWith(selectedPassphrase + ",")).ToList();
                File.WriteAllLines("passphrases.csv", lines);
            }
        }

        private string EncryptPassphrase(string passphrase)
        {
            byte[] key = EncryptionHelper.GetOrCreateEncryptionKey().Chunk(32).First();
            Console.WriteLine($"Key Size: {key.Length} bytes");
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = new byte[16]; // Zero IV for simplicity (Consider using a random IV for better security)

            using var encryptor = aes.CreateEncryptor();
            byte[] encrypted = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(passphrase), 0, passphrase.Length);
            return Convert.ToBase64String(encrypted);
        }

        private string DecryptPassphrase(string encrypted)
        {
            try
            {
                //Logger.Log($"Decrypting: {encrypted}");
                byte[] key = EncryptionHelper.GetOrCreateEncryptionKey().Chunk(32).First();
                using Aes aes = Aes.Create();
                aes.Key = key;
                aes.IV = new byte[16]; // Zero IV must match encryption

                using var decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(Convert.FromBase64String(encrypted), 0, Convert.FromBase64String(encrypted).Length);

                string passphrase = Encoding.UTF8.GetString(decrypted);
                //Logger.Log($"Decrypted passphrase: {passphrase}");
                return passphrase;
            }
            catch (Exception ex)
            {
                Logger.Log($"Decryption Failed: {ex.Message}");
                NotifyUser("Failed To Decrypt", "If you think this is a bug, please report this bug.", true);
                return "Decryption failed";
            }
        }


        private void AutoPasteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            File.WriteAllText("auto_paste_config.txt", "true");
        }

        private void AutoPasteCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            File.WriteAllText("auto_paste_config.txt", "false");
        }

        private void LoadAutoPasteConfig()
        {
            if (File.Exists("auto_paste_config.txt"))
            {
                string value = File.ReadAllText("auto_paste_config.txt");
                AutoPasteCheckBox.IsChecked = value.Trim().ToLower() == "true";
            }
        }

    }
}
