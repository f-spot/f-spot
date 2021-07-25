// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Serilog;

namespace FSpot
{
	public static class Logger
	{
		public static ILogger Log { get; private set; }

		public static void CreateLogger ()
		{
			Log = new LoggerConfiguration ()
							.MinimumLevel.Debug ()
							.WriteTo.Console ()
							.WriteTo.File ("logs/f-spot.txt", rollingInterval: RollingInterval.Day)
							.CreateLogger ();

			Log.Information ("Serilog initialized");
		}
	}
}
