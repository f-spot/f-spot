//
// DbBoundType.cs
//
// Author:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Data.Sqlite.Tests
{
	enum IntEnum : int
	{
		Zero,
		One,
		Two,
		Three
	}

	enum LongEnum : long
	{
		Cero,
		Uno,
		Dos,
		Tres
	}

	class ModelProvider : SqliteModelProvider<DbBoundType>
	{
		public ModelProvider (HyenaSqliteConnection connection) : base (connection)
		{
			Init ();
		}

		public override string TableName {
			get { return "TestTable"; }
		}
		protected override int ModelVersion {
			get { return 1; }
		}
		protected override int DatabaseVersion {
			get { return 1; }
		}

		protected override void MigrateTable (int old_version)
		{
		}
		protected override void MigrateDatabase (int old_version)
		{
		}
		protected override DbBoundType MakeNewObject ()
		{
			return new DbBoundType ();
		}
	}

	class DbBoundType
	{
		[DatabaseColumn ("PrimaryKey", Constraints = DatabaseColumnConstraints.PrimaryKey)]
		public long PrimaryKey;

		[DatabaseColumn ("PublicIntField")]
		public int PublicIntField;
		[DatabaseColumn ("PublicLongField")]
		public long PublicLongField;
		[DatabaseColumn ("PublicStringField")]
		public string PublicStringField;
		[DatabaseColumn ("PublicDateTimeField")]
		public DateTime PublicDateTimeField;
		[DatabaseColumn ("PublicTimeSpanField")]
		public TimeSpan PublicTimeSpanField;
		[DatabaseColumn ("PublicIntEnumField")]
		public IntEnum PublicIntEnumField;
		[DatabaseColumn ("PublicLongEnumField")]
		public LongEnum PublicLongEnumField;

		int public_int_property_field;
		[DatabaseColumn ("PublicIntProperty")]
		public int PublicIntProperty {
			get { return public_int_property_field; }
			set { public_int_property_field = value; }
		}
		long public_long_property_field;
		[DatabaseColumn ("PublicLongProperty")]
		public long PublicLongProperty {
			get { return public_long_property_field; }
			set { public_long_property_field = value; }
		}
		string public_string_property_field;
		[DatabaseColumn ("PublicStringProperty")]
		public string PublicStringProperty {
			get { return public_string_property_field; }
			set { public_string_property_field = value; }
		}
		DateTime public_date_time_proprety_field;
		[DatabaseColumn ("PublicDateTimeProperty")]
		public DateTime PublicDateTimeProperty {
			get { return public_date_time_proprety_field; }
			set { public_date_time_proprety_field = value; }
		}
		TimeSpan public_time_span_property_field;
		[DatabaseColumn ("PublicTimeSpanProperty")]
		public TimeSpan PublicTimeSpanProperty {
			get { return public_time_span_property_field; }
			set { public_time_span_property_field = value; }
		}
		IntEnum public_int_enum_property_field;
		[DatabaseColumn ("PublicIntEnumProperty")]
		public IntEnum PublicIntEnumProperty {
			get { return public_int_enum_property_field; }
			set { public_int_enum_property_field = value; }
		}
		LongEnum public_long_enum_property_field;
		[DatabaseColumn ("PublicLongEnumProperty")]
		public LongEnum PublicLongEnumProperty {
			get { return public_long_enum_property_field; }
			set { public_long_enum_property_field = value; }
		}

		[DatabaseColumn ("PrivateIntField")]
		int private_int_field;
		[DatabaseColumn ("PrivateLongField")]
		long private_long_field;
		[DatabaseColumn ("PrivateStringField")]
		string private_string_field;
		[DatabaseColumn ("PrivateDateTimeField")]
		DateTime private_date_time_field;
		[DatabaseColumn ("PrivateTimeSpanField")]
		TimeSpan private_time_span_field;
		[DatabaseColumn ("PrivateIntEnumField")]
		IntEnum private_int_enum_field;
		[DatabaseColumn ("PrivateLongEnumField")]
		LongEnum private_long_enum_field;

		public int GetPrivateIntField ()
		{
			return private_int_field;
		}
		public void SetPrivateIntField (int value)
		{
			private_int_field = value;
		}
		public long GetPrivateLongField ()
		{
			return private_long_field;
		}
		public void SetPrivateLongField (long value)
		{
			private_long_field = value;
		}
		public string GetPrivateStringField ()
		{
			return private_string_field;
		}
		public void SetPrivateStringField (string value)
		{
			private_string_field = value;
		}
		public DateTime GetPrivateDateTimeField ()
		{
			return private_date_time_field;
		}
		public void SetPrivateDateTimeField (DateTime value)
		{
			private_date_time_field = value;
		}
		public TimeSpan GetPrivateTimeSpanField ()
		{
			return private_time_span_field;
		}
		public void SetPrivateTimeSpanField (TimeSpan value)
		{
			private_time_span_field = value;
		}
		public IntEnum GetPrivateIntEnumField ()
		{
			return private_int_enum_field;
		}
		public void SetPrivateIntEnumField (IntEnum value)
		{
			private_int_enum_field = value;
		}
		public LongEnum GetPrivateLongEnumField ()
		{
			return private_long_enum_field;
		}
		public void SetPrivateLongEnumField (LongEnum value)
		{
			private_long_enum_field = value;
		}

		int private_int_property_field;
		[DatabaseColumn ("PrivateIntProperty")]
		int private_int_property {
			get { return private_int_property_field; }
			set { private_int_property_field = value; }
		}
		long private_long_property_field;
		[DatabaseColumn ("PrivateLongProperty")]
		long private_long_property {
			get { return private_long_property_field; }
			set { private_long_property_field = value; }
		}
		string private_string_property_field;
		[DatabaseColumn ("PrivateStringProperty")]
		string private_string_property {
			get { return private_string_property_field; }
			set { private_string_property_field = value; }
		}
		DateTime private_date_time_property_field;
		[DatabaseColumn ("PrivateDateTimeProperty")]
		DateTime private_date_time_property {
			get { return private_date_time_property_field; }
			set { private_date_time_property_field = value; }
		}
		TimeSpan private_time_span_property_field;
		[DatabaseColumn ("PrivateTimeSpanProperty")]
		TimeSpan private_time_span_property {
			get { return private_time_span_property_field; }
			set { private_time_span_property_field = value; }
		}
		IntEnum private_int_enum_property_field;
		[DatabaseColumn ("PrivateIntEnumProperty")]
		IntEnum private_int_enum_property {
			get { return private_int_enum_property_field; }
			set { private_int_enum_property_field = value; }
		}
		LongEnum private_long_enum_property_field;
		[DatabaseColumn ("PrivateLongEnumProperty")]
		LongEnum private_long_enum_property {
			get { return private_long_enum_property_field; }
			set { private_long_enum_property_field = value; }
		}

		public int GetPrivateIntProperty ()
		{
			return private_int_property;
		}
		public void SetPrivateIntProperty (int value)
		{
			private_int_property = value;
		}
		public long GetPrivateLongProperty ()
		{
			return private_long_property;
		}
		public void SetPrivateLongProperty (long value)
		{
			private_long_property = value;
		}
		public string GetPrivateStringProperty ()
		{
			return private_string_property;
		}
		public void SetPrivateStringProperty (string value)
		{
			private_string_property = value;
		}
		public DateTime GetPrivateDateTimeProperty ()
		{
			return private_date_time_property;
		}
		public void SetPrivateDateTimeProperty (DateTime value)
		{
			private_date_time_property = value;
		}
		public TimeSpan GetPrivateTimeSpanProperty ()
		{
			return private_time_span_property;
		}
		public void SetPrivateTimeSpanProperty (TimeSpan value)
		{
			private_time_span_property = value;
		}
		public IntEnum GetPrivateIntEnumProperty ()
		{
			return private_int_enum_property;
		}
		public void SetPrivateIntEnumProperty (IntEnum value)
		{
			private_int_enum_property = value;
		}
		public LongEnum GetPrivateLongEnumProperty ()
		{
			return private_long_enum_property;
		}
		public void SetPrivateLongEnumProperty (LongEnum value)
		{
			private_long_enum_property = value;
		}
	}
}
