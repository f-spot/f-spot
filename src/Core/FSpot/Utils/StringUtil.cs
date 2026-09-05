
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

				if (unicodeCategory == UnicodeCategory.NonSpacingMark)
					continue;

				stringBuilder.Append (FoldStrokeLetter (c));
			}

			return stringBuilder.ToString ().Normalize (NormalizationForm.FormC);
		}

		// FormD does not decompose stroked letters (Ø, Ł, Đ, …).
		static char FoldStrokeLetter (char c)
		{
			switch (c) {
			case 'Ø':
			case 'ᴓ':
				return 'O';
			case 'ø':
				return 'o';
			case 'Ł':
				return 'L';
			case 'ł':
				return 'l';
			case 'Đ':
			case 'Ð':
				return 'D';
			case 'đ':
			case 'ð':
				return 'd';
			default:
				return c;
			}
		}
	}
}
