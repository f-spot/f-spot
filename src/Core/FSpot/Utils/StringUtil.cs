
using System.Globalization;
using System.Text;

namespace FSpot.Utils
{
	public static class StringsUtils
	{
		public static string Simplify (string input)
		{
			string normalizedString = input.Normalize (NormalizationForm.FormD);

			var stringBuilder = new StringBuilder ();

			foreach (char c in normalizedString) {
				UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory (c);

				if (unicodeCategory != UnicodeCategory.NonSpacingMark) {
					stringBuilder.Append (c);
				}
			}

			return stringBuilder.ToString ().Normalize (NormalizationForm.FormC);
		}
	}
}
