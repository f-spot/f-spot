// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using FSpot.Resources.Lang;

namespace FSpot.Avalonia.Dialogs;

public enum DateRangePreset
{
	Today,
	Yesterday,
	Last7Days,
	Last30Days,
	Last90Days,
	Last360Days,
	CurrentWeek,
	PreviousWeek,
	ThisMonth,
	PreviousMonth,
	ThisYear,
	PreviousYear,
	AllDates,
	CustomizedRange
}

/// <summary>
/// The periods offered by the Set Date Range dialog, ported from the GTK DateRangeDialog.
/// </summary>
public static class DateRangePresets
{
	public const DateRangePreset Default = DateRangePreset.Last7Days;

	public static IReadOnlyList<DateRangePreset> All { get; } = Enum.GetValues<DateRangePreset> ();

	public static string Label (DateRangePreset preset, DateTime today)
	{
		var culture = CultureInfo.CurrentCulture;
		bool sameYear = today.Year == today.AddMonths (-1).Year;
		string monthFormat = sameYear ? "MMMM" : "MMMM, yyyy";
		return preset switch {
			DateRangePreset.Today => Strings.Today,
			DateRangePreset.Yesterday => Strings.Yesterday,
			DateRangePreset.Last7Days => Strings.LastSevenDays,
			DateRangePreset.Last30Days => Strings.LastThirtyDays,
			DateRangePreset.Last90Days => Strings.LastNintyDays,
			DateRangePreset.Last360Days => Strings.LastThreeHundredSixtyDays,
			DateRangePreset.CurrentWeek => Strings.CurrentWeekMonSun,
			DateRangePreset.PreviousWeek => Strings.PreviousWeekMonSun,
			DateRangePreset.ThisMonth => today.ToString (monthFormat, culture),
			DateRangePreset.PreviousMonth => today.AddMonths (-1).ToString (monthFormat, culture),
			DateRangePreset.ThisYear => today.ToString ("yyyy", culture),
			DateRangePreset.PreviousYear => today.AddYears (-1).ToString ("yyyy", culture),
			DateRangePreset.AllDates => Strings.AllImages,
			_ => Strings.CustomizedRange
		};
	}

	/// <summary>
	/// The inclusive range for a preset. The end runs to 23:59:59 of its last day, and
	/// All dates yields null, which clears the filter.
	/// </summary>
	public static (DateTime Start, DateTime End)? Range (DateRangePreset preset, DateTime today, DateTime customStart, DateTime customEnd)
	{
		today = today.Date;
		DateTime start;
		DateTime end = today;
		switch (preset) {
		case DateRangePreset.Today:
			start = today;
			break;
		case DateRangePreset.Yesterday:
			start = end = today.AddDays (-1);
			break;
		case DateRangePreset.Last7Days:
			start = today.AddDays (-6);
			break;
		case DateRangePreset.Last30Days:
			start = today.AddDays (-29);
			break;
		case DateRangePreset.Last90Days:
			start = today.AddDays (-89);
			break;
		case DateRangePreset.Last360Days:
			start = today.AddDays (-359);
			break;
		case DateRangePreset.CurrentWeek:
			start = WeekStart (today);
			break;
		case DateRangePreset.PreviousWeek:
			start = WeekStart (today).AddDays (-7);
			end = start.AddDays (6);
			break;
		case DateRangePreset.ThisMonth:
			start = new DateTime (today.Year, today.Month, 1);
			break;
		case DateRangePreset.PreviousMonth: {
			var previous = today.AddMonths (-1);
			start = new DateTime (previous.Year, previous.Month, 1);
			end = new DateTime (previous.Year, previous.Month, DateTime.DaysInMonth (previous.Year, previous.Month));
			break;
		}
		case DateRangePreset.ThisYear:
			start = new DateTime (today.Year, 1, 1);
			break;
		case DateRangePreset.PreviousYear:
			start = new DateTime (today.Year - 1, 1, 1);
			end = new DateTime (today.Year - 1, 12, 31);
			break;
		case DateRangePreset.CustomizedRange:
			start = customStart.Date;
			end = customEnd.Date;
			if (end < start)
				(start, end) = (end, start);
			break;
		default:
			return null;
		}
		return (start, end.Add (new TimeSpan (23, 59, 59)));
	}

	/// <summary>
	/// The Monday that starts today's ISO 8601 week. GTK stepped back to Sunday and then
	/// forward a day, which put Sunday's week start on the following Monday, after today.
	/// </summary>
	public static DateTime WeekStart (DateTime today)
	{
		int sinceMonday = ((int)today.DayOfWeek + 6) % 7;
		return today.Date.AddDays (-sinceMonday);
	}
}
