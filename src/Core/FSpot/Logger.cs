// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;


using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;

namespace FSpot
{
	public static class Logger
	{
		static LoggingLevelSwitch logLevel;

		public static ILogger Log { get; private set; }

		public static void SetLevel (LogEventLevel newLevel)
		{
			logLevel.MinimumLevel = newLevel;
			Level = newLevel;
		}

		public static LogEventLevel Level { get; private set; }

		public static void CreateLogger ()
		{
			Log = new LoggerConfiguration ()
							.MinimumLevel.ControlledBy (logLevel)
							.Enrich.WithExceptionDetails()
							.Enrich.FromLogContext()
							.WriteTo.Console ()
							.WriteTo.File (Path.Combine ("logs", "f-spot.txt"), rollingInterval: RollingInterval.Day)
							.CreateLogger ();

			SetLevel (LogEventLevel.Information);

			Logger.Log.Information ("Serilog initialized");
		}
	}
}
