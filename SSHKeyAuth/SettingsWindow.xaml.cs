using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SSHKeyAuth
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load settings from persistent storage
            CurrentHotkeyLabel.Text = SettingsManager.GetHotkeyDisplay();
            AutoPasteCheckBox.IsChecked = SettingsManager.AutoPasteEnabled;
            TrayBehaviorComboBox.SelectedIndex = SettingsManager.TrayDoubleClick ? 1 : 0;
            ColorModeComboBox.SelectedIndex = SettingsManager.IsDarkMode ? 1 : 0;
        }

        private void SetHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            HotkeyManager.StartListeningForHotkey();
            MessageBox.Show("Press the first key (Ctrl/Alt/Shift), then the second key.", "Set Hotkey");
        }

        private void AutoPasteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SettingsManager.AutoPasteEnabled = AutoPasteCheckBox.IsChecked ?? false;
            SettingsManager.Save();
        }

        private void ColorModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SettingsManager.IsDarkMode = ColorModeComboBox.SelectedIndex == 1;
            ThemeManager.ApplyTheme(SettingsManager.IsDarkMode);
            SettingsManager.Save();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.ResetDefaults();
            LoadSettings();
            MessageBox.Show("Settings have been reset to default.", "Reset");
        }
    }

}
