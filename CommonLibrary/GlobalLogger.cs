using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public static class GlobalLogger
    {
        private static readonly string logFilePath = Directory.GetCurrentDirectory() + @"\\log.txt";
        private static readonly object lockObj = new();

        public static void Log(string message, bool writeToFile = false)
        {
            lock (lockObj) // Ensures thread safety
            {
                WriteToScreen(message);
                if (writeToFile)
                {
                    WriteToFile(message);
                }
            }
        }

        private static void WriteToScreen(string message)
        {
            Console.WriteLine(message);
        }

        private static void WriteToFile(string message)
        {
            try
            {
                File.AppendAllText(logFilePath, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to file: {ex.Message}");
            }
        }
    }

}
