using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSpot.Models
{
	public partial class Roll : BaseModel
	{
		[NotMapped]
		public long OldId { get; set; }
		public DateTime UtcTime { get; set; }
	}
}
