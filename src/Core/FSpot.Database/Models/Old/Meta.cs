
namespace FSpot.Models.Old
{
	public partial class Meta : IConvert
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public string Data { get; set; }

		public object Convert ()
		{
			return new Models.Meta {
				OldId = Id,
				Name = Name,
				Data = Data
			};
		}
	}
}
