using System;
using System.Windows;

namespace SSHKeyAuth
{
    public partial class App : Application
    {
        private static TrayIcon trayInstance;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure only one MainWindow instance
            if (!e.Args.Contains("--auth"))
            {
                MainWindow mainWindow = new MainWindow();
                //mainWindow.Show();
            }


            // Initialize the system tray icon only once
            if (trayInstance == null)
            {
                trayInstance = new TrayIcon();
                //trayInstance.Show();
            }
            if (System.Diagnostics.Process.GetProcessesByName("SSHKeyAuth").Length > 1)
            {
                Shutdown(); // Exit if another instance is found
                return;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
