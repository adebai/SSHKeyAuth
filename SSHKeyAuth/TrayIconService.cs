using System;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace SSHKeyAuth
{
    public class TrayIconService
    {
        private readonly NotifyIcon _trayIcon;
        private readonly MainWindow _mainWindow;

        public TrayIconService(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            _trayIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
            };

            _trayIcon.ContextMenuStrip.Items.Add("Open", null, OpenWindow);
            _trayIcon.ContextMenuStrip.Items.Add("Exit", null, ExitApplication);
        }

        private void OpenWindow(object sender, EventArgs e)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
        }

        private void OpenUI_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            });
        }



        private void ExitApplication(object sender, EventArgs e)
        {
            _trayIcon.Dispose();
            Application.Current.Shutdown();
        }
    }
}
