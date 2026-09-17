// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Settings;

using Sentry.Serilog;

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

		// Next to the catalog and settings, not the process cwd: `./bin/f-spot` and a
		// desktop launcher otherwise scatter rolling files in different directories.
		internal static string LogFilePath =>
			Path.Combine (FSpotConfiguration.BaseDirectory, "logs", "f-spot.txt");

		// --debug, --debug-sql and BANSHEE_DEBUG all share FSpotConfiguration.Debugging.
		public static void ApplyDebugging (bool debugging)
		{
			SetLevel (debugging ? LogEventLevel.Debug : LogEventLevel.Information);
		}

		public static void CreateLogger ()
		{
			if (log != null)
				return;

			log = BuildLogger (LogFilePath);
			if (!ReferenceEquals (log, Serilog.Core.Logger.None)) {
				ApplyDebugging (FSpotConfiguration.Debugging);
				log.Information ("Serilog initialized at {Level}, file {File}", Level, LogFilePath);
			}
		}

		internal static ILogger BuildLogger (string filePath)
		{
			try {
				var config = new LoggerConfiguration ()
					.MinimumLevel.ControlledBy (logLevel)
					.Enrich.FromLogContext ()
					.WriteTo.Console ()
					.WriteTo.File (filePath, rollingInterval: RollingInterval.Day)
					.WriteTo.Sentry (ConfigureSentrySink);

				// Isolated so a TypeLoadException from Serilog.Exceptions on Mono
				// cannot take the file sink down with it.
				try {
					config.Enrich.WithExceptionDetails ();
				} catch (Exception ex) {
					Console.Error.WriteLine (ex);
				}

				return config.CreateLogger ();
			} catch (Exception ex) {
				Console.Error.WriteLine (ex);
				try {
					return new LoggerConfiguration ()
						.MinimumLevel.ControlledBy (logLevel)
						.WriteTo.Console ()
						.WriteTo.Sentry (ConfigureSentrySink)
						.CreateLogger ();
				} catch {
					return Serilog.Core.Logger.None;
				}
			}
		}

		// GTK Main and the Avalonia Program call SentrySdk.Init. Tests only call
		// BuildLogger, so this sink must not initialize the SDK or CI would ship
		// events to the project DSN.
		static void ConfigureSentrySink (SentrySerilogOptions options)
		{
			options.InitializeSdk = false;
			options.MinimumEventLevel = LogEventLevel.Error;
			options.MinimumBreadcrumbLevel = LogEventLevel.Information;
		}
	}
}
