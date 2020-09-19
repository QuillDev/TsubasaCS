using System;
using System.Threading.Tasks;
using Discord;

namespace Tsubasa
{
    internal static class Logger
    {
        private static ConsoleColor SeverityToConsoleColor(LogSeverity severity)
        {
            return severity switch
            {
                //Change the color of the console message based on the severity of the message
                LogSeverity.Critical => ConsoleColor.Red,
                LogSeverity.Warning => ConsoleColor.Yellow,
                LogSeverity.Debug => ConsoleColor.Blue,
                LogSeverity.Error => ConsoleColor.DarkRed,
                LogSeverity.Verbose => ConsoleColor.Green,
                LogSeverity.Info => ConsoleColor.Blue,
                _ => ConsoleColor.White
            };
        }

        internal static Task Log(LogMessage logMessage)
        {
            //Set the color and formatting of the message then print it
            Console.ForegroundColor = SeverityToConsoleColor(logMessage.Severity);
            var message =
                $"[{DateTime.Now.ToLongTimeString()} | Source: {logMessage.Source}] Message: {logMessage.Message}";
            Console.WriteLine(message);
            Console.ResetColor();

            return Task.CompletedTask;
        }
    }
}