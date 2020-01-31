using System;
using System.ComponentModel.DataAnnotations.Schema;

using FSpot.Database.Models;

namespace FSpot.Models
{
	public partial class Meta : BaseModel
	{
		[NotMapped]
		public long OldId { get; set; }
		public string Name { get; set; }
		public string Data { get; set; }
	}
}
