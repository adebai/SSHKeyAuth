using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SSHKeyAuth
{
    public static class ThemeManager
    {
        public static void ApplyTheme(bool isDarkMode)
        {
            var app = Application.Current;
            app.Resources["BackgroundColor"] = isDarkMode ? Brushes.Black : Brushes.White;
            app.Resources["ForegroundColor"] = isDarkMode ? Brushes.White : Brushes.Black;
        }
    }

}
