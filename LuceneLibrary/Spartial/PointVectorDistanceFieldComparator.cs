using System;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Vector;
using Spatial4n.Core.Shapes;

namespace Com.Lybecker.LuceneLibrary.Spartial
{
    public class PointVectorDistanceFieldComparatorSource : FieldComparatorSource
    {
        private readonly Point _center;
        private readonly PointVectorStrategy _strategy;

        public PointVectorDistanceFieldComparatorSource(Point center, PointVectorStrategy strategy)
        {
            if (center == null) throw new ArgumentNullException("center");
            if (strategy == null) throw new ArgumentNullException("strategy");

            _center = center;
            _strategy = strategy;
        }

        public override FieldComparator NewComparator(string fieldname, int numHits, int sortPos, bool reversed)
        {
            return new PointVectorDistanceFieldComparator(_center, numHits, _strategy);
        }

        /// <summary>
        /// Sorting for <see cref="PointVectorStrategy"/> by distrance to the origin.
        /// </summary>
        public class PointVectorDistanceFieldComparator : FieldComparator
        {
            private readonly DistanceValue[] _values;
            private DistanceValue _bottom;
            private readonly Point _originPt;
            private readonly PointVectorStrategy _strategy;
            private double[] _currentReaderValuesX;
            private double[] _currentReaderValuesY;

            public PointVectorDistanceFieldComparator(Point origin, int numHits, PointVectorStrategy strategy)
            {
                _values = new DistanceValue[numHits];
                _originPt = origin;
                _strategy = strategy;
            }

            public override int Compare(int slot1, int slot2)
            {
                var a = _values[slot1];
                var b = _values[slot2];

                if (a.Value > b.Value)
                    return 1;
                if (a.Value < b.Value)
                    return -1;

                return 0;
            }

            public override void SetBottom(int slot)
            {
                _bottom = _values[slot];
            }

            public override int CompareBottom(int doc)
            {
                var distance = CalculateDistance(doc);
                if (_bottom.Value > distance)
                {
                    return 1;
                }

                if (_bottom.Value < distance)
                {
                    return -1;
                }

                return 0;
            }

            public override void Copy(int slot, int doc)
            {
                _values[slot] = new DistanceValue
                {
                    Value = CalculateDistance(doc)
                };
            }

            private double CalculateDistance(int doc)
            {
                var x = _currentReaderValuesX[doc];
                var y = _currentReaderValuesY[doc];

                var context = _strategy.GetSpatialContext();

                var pt = context.MakePoint(x, y);

                return context.GetDistCalc().Distance(pt, _originPt);
            }

            public override void SetNextReader(IndexReader reader, int docBase)
            {
                _currentReaderValuesX = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetDoubles(reader, _strategy.GetFieldNameX());
                _currentReaderValuesY = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetDoubles(reader, _strategy.GetFieldNameY());
            }

            public override IComparable this[int slot]
            {
                get { return _values[slot]; }
            }
        }
    }

    public struct DistanceValue : IComparable
    {
        public double Value;
        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;
            return Value.CompareTo(((DistanceValue)obj).Value);
        }
    }
}