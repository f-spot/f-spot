using System;

using FSpot.Database;

namespace FSpot.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			foreach (var arg in args)
				System.Console.WriteLine($"Args: {arg}");

			if (args.Length != 1)
				System.Console.WriteLine("Old database not specified");

			var dbUpgrader = new DatabaseUpgrader (args[0]);
			dbUpgrader.Migrate ();
		}
	}
}
