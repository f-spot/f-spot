using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FSpot.Models
{
	public partial class PhotoVersion : BaseModel
	{
		public Photo Photo { get; set; }

		public Guid PhotoId { get; set; }
		[NotMapped]
		public long OldPhotoId { get; set; }
		public long VersionId { get; set; }
		public string Name { get; set; }
		public string BaseUri { get; set; }
		public string Filename { get; set; }
		public string ImportMd5 { get; set; }
		public bool Protected { get; set; }
	}
}
