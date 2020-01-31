using System;

namespace FSpot.Models.Old
{
	public class Roll : IConvert
	{
		public long Id { get; set; }
		public long Time { get; set; }

		public object Convert ()
		{
			return new Models.Roll {
				OldId = Id,
				UtcTime = DateTimeOffset.FromUnixTimeSeconds (Time).UtcDateTime
			};
		}
	}
}
