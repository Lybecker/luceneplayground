using System;
using Lucene.Net.Index;
using Lucene.Net.Search;

// Thease are copies from Lucene.Net.dll
namespace Com.Lybecker.LuceneLibrary
{
    public class MyStringComparatorSource : FieldComparatorSource
    {
        public override FieldComparator NewComparator(string fieldName, int numHits, int sortPos, bool reversed)
        {
            return new MyStringComparatorLocale(numHits, fieldName, System.Threading.Thread.CurrentThread.CurrentCulture);
        }
    }

    /// <summary>Sorts by a field's value using the Collator for a
    /// given Locale.
    /// </summary>
    public sealed class MyStringComparatorLocale : FieldComparator
    {

        private System.String[] values;
        private System.String[] currentReaderValues;
        private System.String field;
        internal System.Globalization.CompareInfo collator;
        private System.String bottom;

        internal MyStringComparatorLocale(int numHits, System.String field, System.Globalization.CultureInfo locale)
        {
            values = new System.String[numHits];
            this.field = field;
            collator = locale.CompareInfo;
        }

        public override int Compare(int slot1, int slot2)
        {
            System.String val1 = values[slot1];
            System.String val2 = values[slot2];
            if (val1 == null)
            {
                if (val2 == null)
                {
                    return 0;
                }
                return -1;
            }
            else if (val2 == null)
            {
                return 1;
            }
            return collator.Compare(val1.ToString(), val2.ToString());
        }

        public override int CompareBottom(int doc)
        {
            System.String val2 = currentReaderValues[doc];
            if (bottom == null)
            {
                if (val2 == null)
                {
                    return 0;
                }
                return -1;
            }
            else if (val2 == null)
            {
                return 1;
            }
            return collator.Compare(bottom.ToString(), val2.ToString());
        }

        public override void Copy(int slot, int doc)
        {
            values[slot] = currentReaderValues[doc];
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            currentReaderValues = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetStrings(reader, field);
        }

        public override IComparable this[int slot]
        {
            get { return values[slot]; }
        }

        public override void SetBottom(int slot)
        {
            this.bottom = values[slot];
        }
    }
}