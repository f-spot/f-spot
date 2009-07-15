using System;
using System.Collections;

using SemWeb;

namespace SemWeb.Query {
	public abstract class ValueFilterFactory {
		public abstract ValueFilter GetValueFilter(string predicate, Resource obj);
	}
	
	public abstract class ValueFilter {
		public static Entity qFilterStringContains = "http://purl.oclc.org/NET/rsquary/string-contains";
		
		public static Entity qFilterLT = "http://purl.oclc.org/NET/rsquary/lt";
		public static Entity qFilterLE = "http://purl.oclc.org/NET/rsquary/le";
		public static Entity qFilterNE = "http://purl.oclc.org/NET/rsquary/ne";
		public static Entity qFilterEQ = "http://purl.oclc.org/NET/rsquary/eq";
		public static Entity qFilterGT = "http://purl.oclc.org/NET/rsquary/gt";
		public static Entity qFilterGE = "http://purl.oclc.org/NET/rsquary/ge";
		
		public abstract bool Filter(Resource resource, Store targetModel);

		public static ValueFilter GetValueFilter(Entity predicate, Resource obj) {
			if (predicate == qFilterStringContains && obj is Literal)
				return new StringContainsFilter((Literal)obj);
			if (obj is Literal && (predicate == qFilterLT || predicate == qFilterLE || predicate == qFilterNE || predicate == qFilterEQ || predicate == qFilterGT || predicate == qFilterGE)) {
				Literal lit = (Literal)obj;
				int c = 0; bool e = false;
				if (predicate == qFilterLT || predicate == qFilterLE) c = -1;
				if (predicate == qFilterGT || predicate == qFilterGE) c = 1;
				if (predicate == qFilterLE || predicate == qFilterGE) e = true;
				if (predicate == qFilterEQ) e = true;
				
				if (lit.DataType == null || lit.DataType == "" || lit.DataType == "http://www.w3.org/2001/XMLSchema#string" || lit.DataType == "http://www.w3.org/2001/XMLSchema#normalizedString")
					return new StringCompareFilter(lit, c, e);
				
				if (lit.DataType == "http://www.w3.org/2001/XMLSchema#float" || lit.DataType == "http://www.w3.org/2001/XMLSchema#double" || lit.DataType == "http://www.w3.org/2001/XMLSchema#decimal" || lit.DataType == "http://www.w3.org/2001/XMLSchema#integer" || lit.DataType == "http://www.w3.org/2001/XMLSchema#nonPositiveInteger" || lit.DataType == "http://www.w3.org/2001/XMLSchema#negativeInteger" || lit.DataType == "http://www.w3.org/2001/XMLSchema#long" || lit.DataType == "http://www.w3.org/2001/XMLSchema#int" || lit.DataType == "http://www.w3.org/2001/XMLSchema#short" || lit.DataType == "http://www.w3.org/2001/XMLSchema#byte" || lit.DataType == "http://www.w3.org/2001/XMLSchema#nonNegativeInteger" || lit.DataType == "http://www.w3.org/2001/XMLSchema#unsignedLong" || lit.DataType == "http://www.w3.org/2001/XMLSchema#unsignedInt" || lit.DataType == "http://www.w3.org/2001/XMLSchema#unsignedShort" || lit.DataType == "http://www.w3.org/2001/XMLSchema#unsignedByte" || lit.DataType == "http://www.w3.org/2001/XMLSchema#positiveInteger")
					return new NumericCompareFilter(lit, c, e);
				
				if (lit.DataType == "http://www.w3.org/2001/XMLSchema#dateTime" || lit.DataType == "http://www.w3.org/2001/XMLSchema#date" || lit.DataType == "http://www.w3.org/2001/XMLSchema#time")
					return new DateTimeCompareFilter(lit, c, e);

				if (lit.DataType == "http://www.w3.org/2001/XMLSchema#duration")
					return new TimeSpanCompareFilter(lit, c, e);
			}
			return null;
		}
		
	}
	
	public abstract class LiteralValueFilter : ValueFilter {
	}

	internal abstract class StringFilter : LiteralValueFilter {
		protected readonly string pattern;
		public StringFilter(Literal res) : this(res.Value) {
		}
		public StringFilter(string pattern) {
			this.pattern = pattern;
		}
	}
	
	internal class StringCompareFilter : StringFilter {
		int compare;
		bool eq;
		
		// Specify:
		//   compareResult  orEqual  Meaning
		//        -1         false   Less Than
		//        -1         true    Less Than Or Equal
		//         0         false   Not Equal
		//         0         true    Equal
		//         1         false   Greater Than
		//         1         true    Greater Than Or Equal
		
		public StringCompareFilter(Literal res, int compareResult, bool orEqual) : base(res) { compare = compareResult; eq = orEqual; }
		public StringCompareFilter(string pattern, int compareResult, bool orEqual) : base(pattern) { compare = compareResult; eq = orEqual; }
		
		public override bool Filter(Resource resource, Store targetModel) {
			string v = ((Literal)resource).Value;
			int c = v.CompareTo(pattern);
			if (compare == 0) return (c == 0) ^ !eq;
			return c == compare || (c == 0 && eq);
		}
	}	

	internal class StringContainsFilter : StringFilter {
		public StringContainsFilter(Literal res) : base(res) { }
		public StringContainsFilter(string pattern) : base(pattern) { }
		
		public override bool Filter(Resource resource, Store targetModel) {
			string v = ((Literal)resource).Value;
			return v.IndexOf(pattern) != -1;
		}
	}
	
	internal abstract class NumericFilter : LiteralValueFilter {
		protected readonly Decimal number;
		public NumericFilter(Literal res) : this(int.Parse(res.Value)) { }
		public NumericFilter(Decimal number) { this.number = number; }
		
	}

	internal class NumericCompareFilter : NumericFilter {
		int compare;
		bool eq;
		
		public NumericCompareFilter(Literal res, int compareResult, bool orEqual) : base(res) { compare = compareResult; eq = orEqual; }
		public NumericCompareFilter(Decimal number, int compareResult, bool orEqual) : base(number) { compare = compareResult; eq = orEqual; }
		
		public override bool Filter(Resource resource, Store targetModel) {
			string v = ((Literal)resource).Value;
			try {
				Decimal i = Decimal.Parse(v);
				int c = i.CompareTo(number);
				if (compare == 0) return (c == 0) ^ !eq;
				return c == compare || (c == 0 && eq);
			} catch (Exception e) {
				return false;
			}
		}
	}

	internal abstract class DateTimeFilter : LiteralValueFilter {
		protected readonly DateTime datetime;
		public DateTimeFilter(Literal res) : this(DateTime.Parse(res.Value)) { }
		public DateTimeFilter(DateTime datetime) { this.datetime = datetime; }
		
	}

	internal class DateTimeCompareFilter : DateTimeFilter {
		int compare;
		bool eq;
		
		public DateTimeCompareFilter(Literal res, int compareResult, bool orEqual) : base(res) { compare = compareResult; eq = orEqual; }
		public DateTimeCompareFilter(DateTime datetime, int compareResult, bool orEqual) : base(datetime) { compare = compareResult; eq = orEqual; }
		
		public override bool Filter(Resource resource, Store targetModel) {
			string v = ((Literal)resource).Value;
			try {
				DateTime i = DateTime.Parse(v);
				int c = i.CompareTo(datetime);
				if (compare == 0) return (c == 0) ^ !eq;
				return c == compare || (c == 0 && eq);
			} catch (Exception e) {
				return false;
			}
		}
	}
	
	internal abstract class TimeSpanFilter : LiteralValueFilter {
		protected readonly TimeSpan timespan;
		public TimeSpanFilter(Literal res) : this(TimeSpan.Parse(res.Value)) { }
		public TimeSpanFilter(TimeSpan timespan) { this.timespan = timespan; }
		
	}

	internal class TimeSpanCompareFilter : TimeSpanFilter {
		int compare;
		bool eq;
		
		public TimeSpanCompareFilter(Literal res, int compareResult, bool orEqual) : base(res) { compare = compareResult; eq = orEqual; }
		public TimeSpanCompareFilter(TimeSpan timespan, int compareResult, bool orEqual) : base(timespan) { compare = compareResult; eq = orEqual; }
		
		public override bool Filter(Resource resource, Store targetModel) {
			string v = ((Literal)resource).Value;
			try {
				TimeSpan i = TimeSpan.Parse(v);
				int c = i.CompareTo(timespan);
				if (compare == 0) return (c == 0) ^ !eq;
				return c == compare || (c == 0 && eq);
			} catch (Exception e) {
				return false;
			}
		}
	}	
}
