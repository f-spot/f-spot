using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSpot.Models
{
	public partial class PhotoTag : BaseModel
	{
		public Photo Photo { get; set; }
		public Tag Tag { get; set; }

		public Guid PhotoId { get; set; }
		public Guid TagId { get; set; }

		[NotMapped]
		public long OldPhotoId { get; set; }

		[NotMapped]
		public long OldTagId { get; set; }
	}
}
