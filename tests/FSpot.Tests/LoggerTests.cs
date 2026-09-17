using System;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Sentry;

using Serilog;
using Serilog.Events;

namespace FSpot.Tests
{
	[TestClass]
	public class LoggerTests
	{
		readonly TemporaryDirectories temporaryDirectories = new TemporaryDirectories ();

		[TestCleanup]
		public void RemoveTemporaryDirectories ()
		{
			temporaryDirectories.DeleteAll ();
		}

		// Tests only call BuildLogger, so this sink must not initialize the SDK
		// or CI would ship events to the project DSN.
		[TestMethod]
		public void BuildLogger_DoesNotEnableSentry ()
		{
			string directory = temporaryDirectories.Create ("log");
			var logger = Logger.BuildLogger (Path.Combine (directory, "f-spot.txt"));

			using (logger as IDisposable)
				Assert.IsFalse (SentrySdk.IsEnabled);
		}

		[TestMethod]
		public void MessagesAtOrAboveTheConfiguredLevelReachTheFileSink ()
		{
			string directory = temporaryDirectories.Create ("log");
			var logger = Logger.BuildLogger (Path.Combine (directory, "f-spot.txt"));

			try {
				using (logger as IDisposable) {
					Logger.ApplyDebugging (true);
					logger.Debug ("debug when debugging is on");

					Logger.ApplyDebugging (false);
					logger.Debug ("debug when debugging is off");
					logger.Information ("information when debugging is off");
				}

				string log = ReadTheLog (directory);
				Assert.Contains ("debug when debugging is on", log);
				Assert.DoesNotContain ("debug when debugging is off", log);
				Assert.Contains ("information when debugging is off", log);
			} finally {
				Logger.SetLevel (LogEventLevel.Information);
			}
		}

		static string ReadTheLog (string directory)
		{
			string[] files = Directory.GetFiles (directory);
			Assert.IsNotEmpty (files, "the file sink should have created a log file");

			return string.Join (Environment.NewLine, files.Select (File.ReadAllText));
		}
	}
}
