using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSpot.Models
{
	public partial class Photo : BaseModel
	{
		[NotMapped]
		public long OldId { get; set; }
		public DateTime UtcTime { get; set; }
		public string BaseUri { get; set; }
		public string Filename { get; set; }
		public string Description { get; set; }
		public Guid RollId { get; set; }
		[NotMapped]
		public long OldRollId { get; set; }
		public long DefaultVersionId { get; set; }
		public long Rating { get; set; }
		public Roll Roll { get; set; }
		public List<Tag> Tag { get; set; }
	}
}
