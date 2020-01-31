using System.ComponentModel.DataAnnotations.Schema;

namespace FSpot.Models
{
	public partial class Tag : BaseModel
	{
		[NotMapped]
		public long OldId { get; set; }
		public string Name { get; set; }
		public long CategoryId { get; set; }
		public bool IsCategory { get; set; }
		public long SortPriority { get; set; }
		public string Icon { get; set; }
	}
}
