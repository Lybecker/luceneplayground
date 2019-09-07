using System;
using Lucene.Net.Index;
using Lucene.Net.Search;

// Thease are copies from Lucene.Net.dll
namespace Com.Lybecker.LuceneLibrary
{
    public class MyIntComparatorSource : FieldComparatorSource
    {
        public override FieldComparator NewComparator(string fieldName, int numHits, int sortPos, bool reversed)
        {
            return new MyIntComparator(numHits, fieldName);
        }
    }

    [Serializable]
    class MyAnonymousClassIntParser : IntParser
    {
        public virtual int ParseInt(System.String value_Renamed)
        {
            return System.Int32.Parse(value_Renamed);
        }
        protected internal virtual System.Object ReadResolve()
        {
            return Lucene.Net.Search.FieldCache_Fields.DEFAULT_INT_PARSER;
        }
        public override System.String ToString()
        {
            return typeof(FieldCache).FullName + ".DEFAULT_INT_PARSER";
        }
    }

    /// <summary>Parses field's values as int (using {@link
    /// FieldCache#getInts} and sorts by ascending value 
    /// </summary>
    public sealed class MyIntComparator : FieldComparator
    {
        private int[] values;
        private int[] currentReaderValues;
        private System.String field;
        private int bottom; // Value of bottom of queue

        internal MyIntComparator(int numHits, System.String field)
        {
            values = new int[numHits];
            this.field = field;
        }

        public override int Compare(int slot1, int slot2)
        {
            // TODO: there are sneaky non-branch ways to compute
            // -1/+1/0 sign
            // Cannot return values[slot1] - values[slot2] because that
            // may overflow
            int v1 = values[slot1];
            int v2 = values[slot2];
            if (v1 > v2)
            {
                return 1;
            }
            else if (v1 < v2)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        public override void SetBottom(int slot)
        {
            this.bottom = values[bottom];
        }

        public override int CompareBottom(int doc)
        {
            // TODO: there are sneaky non-branch ways to compute
            // -1/+1/0 sign
            // Cannot return bottom - values[slot2] because that
            // may overflow
            int v2 = currentReaderValues[doc];
            if (bottom > v2)
            {
                return 1;
            }
            else if (bottom < v2)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        public override void Copy(int slot, int doc)
        {
            values[slot] = currentReaderValues[doc];
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            currentReaderValues = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetInts(reader, field);
        }

        public override IComparable this[int slot]
        {
            get { return (System.Int32)values[slot]; }
        }
    }
}