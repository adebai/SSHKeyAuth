using System;
using System.IO;

namespace SSHKeyAuth
{
    public static class Logger
    {
        private static readonly string LogFilePath = "log.txt";

        public static void Log(string message)
        {
            string logEntry = $"{DateTime.Now}: {message}";
            File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
        }
    }
}
