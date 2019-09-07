using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Com.Lybecker.LuceneLibrary
{
    /// <summary>
    /// Sorts the documents according to the sort order of the sortOrder parameter.
    /// </summary>
    public class OpressComparatorSource<T> : FieldComparatorSource where T : IComparable
    {
        private readonly IDictionary<T, int> _sortOrderDictionary;
        private readonly Func<IndexReader, string, T[]> _getFieldCache;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="sortOrder">Sorts the document in the same order as the identifyer in the sortOrder enumerable.</param>
        /// <param name="getFieldCache">Method to retrieve FieldCache entries. E.g. Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetInts</param>
        public OpressComparatorSource(IEnumerable<T> sortOrder, Func<IndexReader, string, T[]> getFieldCache)
        {
            if (sortOrder == null)
                throw new ArgumentNullException("sortOrder");

            sortOrder = sortOrder.Distinct();

            int positionCounter = 0;

            _sortOrderDictionary = sortOrder.ToDictionary(key => key, value => positionCounter++);
            _getFieldCache = getFieldCache;
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="sortOrder">Sorts the identifyer (value) in the order or the keys.</param>
        /// <param name="getFieldCache">Method to retrieve FieldCache entries. E.g. Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetInts</param>
        public OpressComparatorSource(IDictionary<T, int> sortOrder, Func<IndexReader, string, T[]> getFieldCache)
        {
            if (sortOrder == null)
                throw new ArgumentNullException("sortOrder");

            _sortOrderDictionary = sortOrder;
            _getFieldCache = getFieldCache;
        }

        public override FieldComparator NewComparator(string fieldName, int numHits, int sortPos, bool reversed)
        {
            return new OpressFieldComparator<T>(numHits, fieldName, _sortOrderDictionary, _getFieldCache);
        }
    }

    /// <see cref="Lucene.Net.Search.FieldComparator"/>
    internal class OpressFieldComparator<T> : FieldComparator where T : IComparable
    {
        private readonly string _fieldName;
        private readonly int[] _valuesPos;
        private readonly IDictionary<T, int> _sortOrder;
        private readonly Func<IndexReader, string, T[]> _getFieldCache;
        private T[] _currentReaderValues;
        private int _bottomPos;

        public OpressFieldComparator(int numHits, string fieldName, IDictionary<T, int> sortOrder, Func<IndexReader, string, T[]> getFieldCache)
        {
            _valuesPos = new int[numHits];
            _fieldName = fieldName;
            _sortOrder = sortOrder;
            _getFieldCache = getFieldCache;
        }

        public override int Compare(int slot1, int slot2)
        {
            var orderPos1 = _valuesPos[slot1];
            var orderPos2 = _valuesPos[slot2];

            if (orderPos1 > orderPos2)
                return 1;
            else if (orderPos1 < orderPos2)
                return -1;
            else
                return 0;
        }

        public override int CompareBottom(int doc)
        {
            var docPos = GetPosition(doc);

            if (_bottomPos > docPos)
                return 1;
            else if (_bottomPos < docPos)
                return -1;
            else
                return 0;
        }

        public override void SetBottom(int slot)
        {
            _bottomPos = _valuesPos[slot];
        }

        public override void Copy(int slot, int doc)
        {
            var docPos = GetPosition(doc);

            _valuesPos[slot] = docPos;
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            _currentReaderValues = _getFieldCache(reader, _fieldName);
        }

        /// <summary>
        /// Returns the logical position of each doc in search context.
        /// </summary>
        public override IComparable this[int slot] 
        {
            get {return _valuesPos[slot]; }
        }

        private int GetPosition(int doc)
        {
            var docValue = _currentReaderValues[doc];

            var docPos = _sortOrder.ContainsKey(docValue) ? _sortOrder[docValue] : int.MaxValue;

            return docPos;
        }
    }
}