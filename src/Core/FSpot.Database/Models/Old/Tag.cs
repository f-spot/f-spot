
namespace FSpot.Models.Old
{
	public partial class Tag : IConvert
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public long CategoryId { get; set; }
		public bool IsCategory { get; set; }
		public long SortPriority { get; set; }
		public string Icon { get; set; }

		public object Convert ()
		{
			return new Models.Tag {
				OldId = Id,
				Name = Name,
				CategoryId = CategoryId,
				IsCategory = IsCategory,
				SortPriority = SortPriority,
				Icon = Icon
			};
		}
	}
}
