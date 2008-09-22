//
// Mono.Facebook.User.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//	George Talusan (george@convolve.ca)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//

// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Xml.Serialization;

namespace Mono.Facebook
{
	public class Affiliation
	{
		[XmlElement ("nid")]
		public long NId;

		[XmlElement ("name")]
		public string Name;

		[XmlElement ("type")]
		public string Type;

		[XmlElement ("status")]
		public string Status;

		[XmlElement ("year")]
		public string Year;
	}

	public class Affiliations
	{
		[XmlElement ("affiliation")]
		public Affiliation[] affiliations_array;

		[XmlIgnore ()]
		public Affiliation[] AffiliationCollection
		{
			get { return affiliations_array ?? new Affiliation[0]; }
		}
	}

	public class Concentrations
	{
		[XmlElement ("concentration")]
		public string[] concentration_array;

		[XmlIgnore ()]
		public string[] ConcentrationCollection
		{
			get { return concentration_array ?? new string[0]; }
		}
	}

	public class EducationHistory
	{
		[XmlElement ("education_info")]
		public EducationInfo[] educations_array;

		[XmlIgnore ()]
		public EducationInfo[] EducationInfo
		{
			get { return educations_array ?? new EducationInfo[0]; }
		}
	}

	public class EducationInfo
	{
		[XmlElement ("name")]
		public string Name;

		[XmlElement ("year")]
		public int Year;

		[XmlElement ("concentrations")]
		public Concentrations concentrations;

		[XmlIgnore ()]
		public string[] Concentrations
		{
			get { return concentrations.ConcentrationCollection; }
		}
	}

	public class HighSchoolInfo
	{
		[XmlElement ("hs1_info")]
		public string HighSchoolOneName;

		[XmlElement ("hs2_info")]
		public string HighSchoolTwoName;

		[XmlElement ("grad_year")]
		public int GraduationYear;

		[XmlElement ("hs1_id")]
		public int HighSchoolOneId;

		[XmlElement ("hs2_id")]
		public int HighSchoolTwoId;
	}

	public class MeetingFor
	{
		[XmlElement ("seeking")]
		public string[] seeking;

		[XmlIgnore ()]
		public string[] Seeking
		{
			get { return seeking ?? new string[0]; }
		}
	}

	public class MeetingSex
	{
		[XmlElement ("sex")]
		public string[] sex;

		[XmlIgnore ()]
		public string[] Sex
		{
			get { return sex ?? new string[0]; }
		}
	}

	public class Status
	{
		[XmlElement ("message")]
		public string Message;

		[XmlElement ("time")]
		public long Time;
	}

	public class WorkHistory
	{
		[XmlElement ("work_info")]
		public WorkInfo[] workinfo_array;

		[XmlIgnore ()]
		public WorkInfo[] WorkInfo
		{
			get { return workinfo_array ?? new WorkInfo[0]; }
		}
	}

	public class WorkInfo
	{
		[XmlElement ("location")]
		public Location Location;

		[XmlElement ("company_name")]
		public string CompanyName;

		[XmlElement ("position")]
		public string Position;

		[XmlElement ("description")]
		public string Description;

		[XmlElement ("start_date")]
		public string StartDate;

		[XmlElement ("end_date")]
		public string EndDate;
	}

	public class User : Friend
	{
		public static readonly string[] FIELDS = { "about_me", "activities", "affiliations", "birthday", "books",
			"current_location", "education_history", "first_name", "hometown_location", "interests", "last_name",
			"movies", "music", "name", "notes_count", "pic", "pic_big", "pic_small", "political", "profile_update_time",
			"quotes", "relationship_status", "religion", "sex", "significant_other_id",
			"status", "timezone", "tv", "uid", "wall_count" };

		[XmlElement ("about_me")]
		public string AboutMe;

		[XmlElement ("activities")]
		public string Activities;

		[XmlElement ("affiliations")]
		public Affiliations affiliations;

		[XmlIgnore ()]
		public Affiliation[] Affiliations
		{
			get
			{
				if (affiliations == null)
				{
					return new Affiliation[0];
				}
				else
				{
					return affiliations.AffiliationCollection ?? new Affiliation[0];
				}
			}
		}

		[XmlElement ("birthday")]
		public string Birthday;

		[XmlElement ("books")]
		public string Books;

		[XmlElement ("current_location")]
		public Location CurrentLocation;

		[XmlElement ("education_history")]
		public EducationHistory EducationHistory;

		[XmlElement ("first_name")]
		public string FirstName;

		[XmlElement ("hometown_location")]
		public Location HomeTownLocation;

		[XmlElement ("hs_info")]
		public HighSchoolInfo HighSchoolInfo;

		[XmlElement ("interests")]
		public string Interests;

		[XmlElement ("is_app_user")]
		public string is_app_user;

		public bool IsAppUser {
			get {
				return Util.GetBoolFromString(is_app_user);
			}
		}

		[XmlElement ("last_name")]
		public string LastName;

		[XmlElement ("meeting_for")]
		public MeetingFor MeetingFor;

		[XmlElement ("meeting_sex")]
		public MeetingSex MeetingSex;

		[XmlElement ("movies")]
		public string Movies;

		[XmlElement ("music")]
		public string Music;

		[XmlElement ("name")]
		public string Name;

		[XmlElement("notes_count")]
		public string notes_count;

		[XmlIgnore()]
		public int NotesCount {
			get {
				return Util.GetIntFromString(notes_count);
			}
		}

		[XmlElement ("pic")]
		public string Pic;

		[XmlIgnore ()]
		public Uri PicUri
		{
			get { return new Uri (Pic); }
		}

		[XmlElement ("pic_big")]
		public string PicBig;

		[XmlIgnore ()]
		public Uri PicBigUri
		{
			get { return new Uri (PicBig); }
		}

		[XmlElement ("pic_small")]
		public string PicSmall;

		[XmlIgnore ()]
		public Uri PicSmallUri
		{
			get { return new Uri (PicSmall); }
		}

		[XmlElement ("political")]
		public string Political;

		[XmlElement ("profile_update_time")]
		public long ProfileUpdateTime;

		[XmlElement ("quotes")]
		public string Quotes;

		[XmlElement ("relationship_status")]
		public string RelationshipStatus;

		[XmlElement ("religion")]
		public string Religion;

		[XmlElement ("sex")]
		public string Sex;

		[XmlElement ("significant_other_id")]
		public string significant_other_id;

		[XmlIgnore ()]
		public long SignificantOtherId
		{
			get {
				return Util.GetIntFromString(significant_other_id);
			}
		}

		[XmlElement ("status")]
		public Status Status;

		[XmlElement ("timezone")]
		public string timezone;

		public int TimeZone {
			get {
				return Util.GetIntFromString(timezone);
			}
		}

		[XmlElement ("tv")]
		public string Tv;

		[XmlElement ("wall_count")]
		public string wall_count;

		[XmlIgnore()]
		public int WallCount
		{
			get {
				return Util.GetIntFromString(wall_count);
			}
		}

		[XmlElement ("work_history")]
		public WorkHistory WorkHistory;
	}
}
