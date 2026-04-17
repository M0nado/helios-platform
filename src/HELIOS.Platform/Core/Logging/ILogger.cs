using System;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Logging
{
    /// <summary>
    /// Logging service for HELIOS Platform.
    /// </summary>
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception ex = null);
        void Critical(string message, Exception ex = null);
    }

    /// <summary>
    /// Default logger implementation using Console.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void Debug(string message) => Log(ConsoleColor.Gray, $"[DEBUG] {message}");
        public void Info(string message) => Log(ConsoleColor.Cyan, $"[INFO] {message}");
        public void Warning(string message) => Log(ConsoleColor.Yellow, $"[WARN] {message}");
        public void Error(string message, Exception ex = null) => Log(ConsoleColor.Red, $"[ERROR] {message}" + (ex != null ? $"\n{ex.Message}" : ""));
        public void Critical(string message, Exception ex = null) => Log(ConsoleColor.Magenta, $"[CRITICAL] {message}" + (ex != null ? $"\n{ex.Message}" : ""));

        private void Log(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
