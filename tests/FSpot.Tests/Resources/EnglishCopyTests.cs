// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Resources
{
	[TestClass]
	public class EnglishCopyTests
	{
		[TestCleanup]
		public void RestoreCulture ()
		{
			Strings.Culture = null;
		}

		[TestMethod]
		public void EnglishResources_DoNotShowSephiaOrTransparencyTokens ()
		{
			Strings.Culture = CultureInfo.InvariantCulture;

			Assert.AreEqual ("Sepia Tone", Strings.SepiaTone);
			Assert.DoesNotContain ("Sephia", Strings.SepiaTone);

			string transparency = Strings.YouCanChooseHowToDisplayTransparentPartsInImagesThisOptionHasNoEffectOnPhotosButSettingThisValueToCHECKPATTERNOrCUSTOMCOLORCouldBeUsefulWhenViewingIconsOrOtherArtworkWithTransparentParts;
			Assert.AreEqual ("You can choose how to display transparent parts in images: the background, a check pattern, or a custom color. This option has no effect on photos, but a check pattern or custom color can be useful when viewing icons or other artwork with transparent parts.", transparency);
			Assert.DoesNotContain ("CHECK_PATTERN", transparency);
			Assert.DoesNotContain ("CUSTOM_COLOR", transparency);
			Assert.DoesNotContain ("Sephia", transparency);
		}
	}
}
