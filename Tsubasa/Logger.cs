using System;
using System.Threading.Tasks;
using Discord;

namespace Tsubasa
{
    internal class Logger
    {
        public static ConsoleColor SeverityToConsoleColor(LogSeverity severity)
        {
            switch (severity)
            {
                //Change the color of the console message based on the severity of the message
                case LogSeverity.Critical:
                    return ConsoleColor.Red; 
                case LogSeverity.Warning:
                    return ConsoleColor.Yellow;
                case LogSeverity.Debug:
                    return ConsoleColor.Blue;
                case LogSeverity.Error:
                    return ConsoleColor.DarkRed;
                case LogSeverity.Verbose:
                    return ConsoleColor.Green;
                case LogSeverity.Info:
                    return ConsoleColor.Blue;
                default:
                    return ConsoleColor.White;
            }

        }

        internal static Task Log(LogMessage logMessage)
        {
            
            //Set the color and formatting of the message then print it
            Console.ForegroundColor = SeverityToConsoleColor(logMessage.Severity);
            string message = $"[{DateTime.Now.ToLongTimeString()} | Source: {logMessage.Source}] Message: {logMessage.Message}";
            Console.WriteLine(message);
            Console.ResetColor();
            
            return Task.CompletedTask;
        }
    }
}