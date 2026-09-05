// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;

namespace FSpot
{
	public static class Logger
	{
		static readonly LoggingLevelSwitch logLevel = new LoggingLevelSwitch ();
		static ILogger log;

		public static ILogger Log {
			get {
				if (log == null)
					CreateLogger ();
				return log;
			}
		}

		public static void SetLevel (LogEventLevel newLevel)
		{
			logLevel.MinimumLevel = newLevel;
			Level = newLevel;
		}

		public static LogEventLevel Level { get; private set; }

		public static void CreateLogger ()
		{
			if (log != null)
				return;

			log = BuildLogger (Path.Combine ("logs", "f-spot.txt"));
			if (!ReferenceEquals (log, Serilog.Core.Logger.None)) {
				SetLevel (LogEventLevel.Information);
				log.Information ("Serilog initialized");
			}
		}

		internal static ILogger BuildLogger (string filePath)
		{
			try {
				return new LoggerConfiguration ()
					.MinimumLevel.ControlledBy (logLevel)
					.Enrich.WithExceptionDetails ()
					.Enrich.FromLogContext ()
					.WriteTo.Console ()
					.WriteTo.File (filePath, rollingInterval: RollingInterval.Day)
					.CreateLogger ();
			} catch (Exception ex) {
				Console.Error.WriteLine (ex);
				try {
					return new LoggerConfiguration ()
						.MinimumLevel.ControlledBy (logLevel)
						.WriteTo.Console ()
						.CreateLogger ();
				} catch {
					return Serilog.Core.Logger.None;
				}
			}
		}
	}
}
