using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Com.Lybecker.LuceneLibrary
{
    /// <summary>
    /// Group results based on groupFieldName and return the top scoring item within each group.
    /// </summary>
    public class GroupTopDocsCollector : Collector
    {
        private readonly int _maxHitCount;
        private int _docBase = 0;
        private Scorer _scorer;

        private readonly string _groupFieldName;
        readonly SortedDictionary<string, GroupMatch> _groups = new SortedDictionary<string, GroupMatch>();
        private string[] _values;

        public GroupTopDocsCollector(int maxHitCount, string groupFieldName)
        {
            _maxHitCount = maxHitCount;
            _groupFieldName = groupFieldName;
        }

        public override void Collect(int doc)
        {
            float score = _scorer.Score();

            var value = _values[doc];

            var absoluteDoc = _docBase + doc;

            if (_groups.ContainsKey(value))
            {
                var oldScore = _groups[value];

                if (oldScore.Score < score && absoluteDoc > oldScore.MaxScoreDoc)
                {
                    oldScore.Score = score;
                    oldScore.MaxScoreDoc = absoluteDoc;
                }

                oldScore.GroupCount += 1;
            }
            else
            {
                //TODO: remove the lowest item when more than maxHitCount is reached
                _groups.Add(value, new GroupMatch() { MaxScoreDoc = absoluteDoc, Score = score, GroupCount = 1 });
            }
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            _values = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetStrings(reader, _groupFieldName);
            _docBase = docBase;
        }

        public override void SetScorer(Scorer scorer)
        {
            _scorer = scorer;
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return true; }
        }

        public GroupTopDocs GroupTopDocs()
        {
            var totalHits = _groups.Count;

            if (_groups.Count == 0)
                return new GroupTopDocs(0, new GroupScoreDoc[0], Single.NaN);

            var results = _groups.OrderByDescending(pair => pair.Value.Score)
                .ThenBy(pair => pair.Value.MaxScoreDoc)
                .Take(_maxHitCount)
                .Select(pair => new GroupScoreDoc(pair.Value.MaxScoreDoc, pair.Value.Score, pair.Value.GroupCount, pair.Key))
                .ToArray();

            return new GroupTopDocs(totalHits, results, _groups.Max(x => x.Value.Score));
        }

        private class GroupMatch
        {
            public float Score { get; set; }
            public int MaxScoreDoc { get; set; }
            public int GroupCount { get; set; }

            public override string ToString()
            {
                return string.Format("MaxScoreDoc: {0} Score: {1} Group Count: {2}", MaxScoreDoc, Score, GroupCount);
            }
        }
    }

    public class GroupTopDocs
    {
        /// <summary>The total number of hits for the query.</summary>
        public int TotalHits { get; set; }

        /// <summary>The top hits for the query. </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public GroupScoreDoc[] GroupScoreDocs { get; set; }

        /// <summary>
        /// Gets or sets the maximum score value encountered, needed for normalizing.
        /// Note that in case scores are not tracked, this returns <see cref="float.NaN" />.
        /// </summary>
        public float MaxScore { get; set; }

        public GroupTopDocs(int totalHits, GroupScoreDoc[] groupScoreDocs, float maxScore)
        {
            TotalHits = totalHits;
            GroupScoreDocs = groupScoreDocs;
            MaxScore = maxScore;
        }

        public override string ToString()
        {
            return string.Format("TotalHits: {0}, GroupScoreDocs: {1}, MaxScore: {2}", MaxScore, GroupScoreDocs.Length, MaxScore);
        }
    }

    public class GroupScoreDoc : ScoreDoc
    {
        /// <summary>
        /// The number of matches in this group.
        /// </summary>
        public int GroupCount { get; set; }
        /// <summary>
        /// The value of the field grouped by
        /// </summary>
        public string GroupFieldValue { get; set; }

        public GroupScoreDoc(int doc, float score, int groupCount, string groupFieldValue)
            : base(doc, score)
        {
            GroupCount = groupCount;
            GroupFieldValue = groupFieldValue;
        }

        public override String ToString()
        {
            return string.Format("{0} group count {1} for '{2}'", base.ToString(), GroupCount, GroupFieldValue);
        }
    }
}