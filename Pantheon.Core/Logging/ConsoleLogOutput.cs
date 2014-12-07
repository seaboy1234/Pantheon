using System;

namespace Pantheon.Core.Logging
{
    public class ConsoleLogOutput : LogOutput
    {
        public ConsoleLogOutput(IEventLogger logger)
            : base(logger)
        {
        }

        protected override void OnLogged(object sender, Core.Event.EventLoggedEventArgs e)
        {
            switch (e.Level)
            {
                case LogLevel.Critical:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;

                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;

                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;

                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
            }
            Console.WriteLine(e.Message);
        }
    }
}