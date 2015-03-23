using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace WART.AppCode
{
    class CountryHelper
    {
        private Dictionary<string, CountryEntry> phoneCodeToItem = new Dictionary<string, CountryEntry>();
        private Dictionary<string, CountryEntry> isoCodeToItem = new Dictionary<string, CountryEntry>();
        private Dictionary<uint, CountryEntry> mccToItem = new Dictionary<uint, CountryEntry>();
        private List<CountryEntry> items = new List<CountryEntry>();

        public CountryHelper()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WART.AppCode.countries.tsv"))
            {
                StreamReader streamReader = new StreamReader(stream, false);
                string text;
                while ((text = streamReader.ReadLine()) != null)
                {
                    if (text.Length != 0)
                    {
                        CountryEntry cii = new CountryEntry(text);
                        this.Add(cii);
                    }
                }
            }
        }

        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this.GetSortedCountryInfos();
        }

        public IEnumerable<CountryEntry> GetSortedCountryInfos()
        {
            return this.items;
        }

        private void Add(CountryEntry cii)
        {
            if (!this.phoneCodeToItem.ContainsKey(cii.CountryCode) && !string.IsNullOrEmpty(cii.FormatRegexes))
            {
                this.phoneCodeToItem[cii.CountryCode] = cii;
            }

            if (!this.isoCodeToItem.ContainsKey(cii.IsoCode))
            {
                this.isoCodeToItem[cii.IsoCode] = cii;
            }

            using (List<uint>.Enumerator enumerator = cii.MCCs.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    uint current = enumerator.Current;
                    if (!this.mccToItem.ContainsKey(current))
                    {
                        this.mccToItem[current] = cii;
                    }
                }
            }
            this.items.Add(cii);
        }

        public bool CheckFormat(string cc, string phone, out string country)
        {
            country = string.Empty;
            using (List<CountryEntry>.Enumerator enumerator = this.items.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TerritoryInfo ti = new TerritoryInfo(cc, cc);
                    CountryEntry current = enumerator.Current;
                    if (current.CountryCode == cc && !string.IsNullOrEmpty(current.FormatRegexes))
                    {
                        //boom!
                        country = current.CountryName;
                        char[] array = ";".ToCharArray();
                        string[] array2 = current.FormatRegexes.Split(array);
                        string[] array3 = current.FormatStrings.Split(array);
                        string[] array4 = current.PrefixRegexes.Split(array);
                        ti.AllowedLengths = current.AllowedLengths;
                        int num = array2.Length;
                        char[] array5 = "#".ToCharArray();
                        for (int i = 0; i < num; i++)
                        {
                            string[] leadingDigitsPatterns = null;
                            string matchPattern = array2[i];
                            string format = array3[i];
                            string text = array4[i].Trim();
                            if (text != "N/A")
                            {
                                leadingDigitsPatterns = text.Split(array5);
                            }
                            ti.addAvailableFormat(matchPattern, format, leadingDigitsPatterns);
                        }

                        using (List<FormatInfo>.Enumerator fenumerator = ti.AvailableFormats.GetEnumerator())
                        {
                            while (fenumerator.MoveNext())
                            {
                                FormatInfo fcur = fenumerator.Current;
                                Regex regex = Enumerable.LastOrDefault<Regex>(fcur.LeadingDigitsPatterns);
                                Match match;
                                if (regex != null)
                                {
                                    match = regex.Match(phone);
                                    if (!match.Success || match.Index != 0)
                                    {
                                        continue;
                                    }
                                }
                                match = fcur.NumberPattern.Match(phone);
                                if (match.Success && match.Length == phone.Length)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }

    public class TerritoryInfo
    {
        private string territoryCode_;
        private string regionCode_;
        private List<FormatInfo> availableFormats_;
        public string TerritoryCode
        {
            get
            {
                return this.territoryCode_;
            }
        }
        public string RegionCode
        {
            get
            {
                return this.regionCode_;
            }
        }
        public List<FormatInfo> AvailableFormats
        {
            get
            {
                return this.availableFormats_;
            }
        }
        public List<int> AllowedLengths
        {
            get;
            set;
        }
        public TerritoryInfo(string territoryCode, string regionCode)
        {
            this.territoryCode_ = territoryCode;
            this.regionCode_ = regionCode;
            this.availableFormats_ = new List<FormatInfo>();
        }
        public void addAvailableFormat(string matchPattern, string format, string[] leadingDigitsPatterns)
        {
            this.availableFormats_.Add(new FormatInfo(matchPattern, format, leadingDigitsPatterns));
        }
    }

    public class FormatInfo
    {
        private Regex numberPattern_;
        private Regex[] leadingDigitsPatterns_;
        private string numberFormat_;
        public Regex NumberPattern
        {
            get
            {
                return this.numberPattern_;
            }
        }
        public string NumberFormat
        {
            get
            {
                return this.numberFormat_;
            }
        }
        public Regex[] LeadingDigitsPatterns
        {
            get
            {
                return this.leadingDigitsPatterns_;
            }
        }
        public FormatInfo(string pattern, string format, string[] leadingDigitsPatterns = null)
        {
            this.numberPattern_ = new Regex(pattern);
            this.numberFormat_ = format;
            Regex[] arg_4D_1;
            if (leadingDigitsPatterns != null)
            {
                arg_4D_1 = Enumerable.ToArray<Regex>(Enumerable.Select<string, Regex>(leadingDigitsPatterns, (string p) => new Regex(p)));
            }
            else
            {
                arg_4D_1 = new Regex[0];
            }
            this.leadingDigitsPatterns_ = arg_4D_1;
        }
    }
}
