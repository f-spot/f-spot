// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;

namespace FSpot.Core
{
	public static class TagDelete
	{
		public static bool HasChildren (Tag tag)
			=> tag is Category { Children.Count: > 0 };

		public static string BlockedHeading
			=> Strings.TagIsNotEmpty;

		public static string BlockedMessage (Tag tag)
			=> string.Format (Strings.CanNotDeleteTagsThatHaveTagsWihinThem_PleaseDeleteTagUnderXFirst, tag?.Name);

		public static string ConfirmHeading (Tag tag)
			=> ConfirmHeading (1, tag);

		public static string ConfirmHeading (int tagCount, Tag tag)
			=> tagCount > 1
				? string.Format (Strings.DeleteTheXSelectedTagsQuestion, tagCount)
				: string.Format (Strings.DeleteTagXQuestion, tag?.Name);

		public static string ConfirmBody (int associatedPhotos)
			=> ConfirmBody (1, associatedPhotos);

		public static string ConfirmBody (int tagCount, int associatedPhotos)
			=> associatedPhotos > 0
				? string.Format (tagCount > 1
						? Strings.IfYouDeleteThisTagsTheAssociationWithXYWillBeLost
						: Strings.IfYouDeleteThisTagTheAssociationWithXYWillBeLost,
					associatedPhotos,
					associatedPhotos == 1 ? Strings.Photo : Strings.Photos)
				: Strings.ThisRemovesTheTagFromTheCatalogAndFromAnyPhotosThatUseIt;
	}
}
