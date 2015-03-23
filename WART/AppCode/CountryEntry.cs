using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WART.AppCode
{
    class CountryEntry
    {
        public string CountryName;
		public string IsoCode
		{
			get;
			private set;
		}
		public string CountryCode
		{
			get;
			private set;
		}
		public List<uint> MCCs
		{
			get;
			private set;
		}
		public List<int> AllowedLengths
		{
			get;
			private set;
		}
		public List<char> StripLeadingDigits
		{
			get;
			private set;
		}
		public string MobileRegex
		{
			get;
			private set;
		}
		public string FormatRegexes
		{
			get;
			private set;
		}
		public string FormatStrings
		{
			get;
			private set;
		}
		public string PrefixRegexes
		{
			get;
			private set;
		}
		public CountryEntry(string line)
		{
			string[] array = line.Split(new char[]
			{
				'\t'
			});
			this.IsoCode = array[0];
			this.CountryName = array[1];
			this.CountryCode = array[2];
			this.MCCs = Enumerable.ToList<uint>(Enumerable.Select<string, uint>(array[3].Split(new char[]
			{
				','
			}), (string s) => uint.Parse(s)));
			string text = array[4];
			IEnumerable<int> arg_C6_0;
			if (text.Length != 0)
			{
				arg_C6_0 = Enumerable.Select<string, int>(text.Split(new char[]
				{
					','
				}), (string st) => int.Parse(st));
			}
			else
			{
				arg_C6_0 = Enumerable.Empty<int>();
			}
			this.AllowedLengths = Enumerable.ToList<int>(arg_C6_0);
			string text2 = array[5];
			IEnumerable<char> arg_11C_0;
			if (text2.Length != 0)
			{
				arg_11C_0 = Enumerable.Select<string, char>(text2.Split(new char[]
				{
					','
				}), (string st) => st.ToCharArray()[0]);
			}
			else
			{
				arg_11C_0 = Enumerable.Empty<char>();
			}
			this.StripLeadingDigits = Enumerable.ToList<char>(arg_11C_0);
			this.MobileRegex = array[6];
			this.FormatRegexes = array[7];
			this.FormatStrings = array[8];
			this.PrefixRegexes = array[9];
		}
    }
}
