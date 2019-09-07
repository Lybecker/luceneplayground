using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Com.Lybecker.LuceneLibrary
{
    /// <summary>
    /// Makes it possible to use multiple <see cref="Collector"/> with a single query.
    /// </summary>
    public class MultiCollector : Collector
    {
        public static Collector Wrap(IList<Collector> collectors)
        {
            if (collectors.Any(collector => collector == null))
                throw new ArgumentNullException("collectors", "One or more collectors are null.");

            if (collectors.Count() == 1)
                return collectors.First();

            return new MultiCollector(collectors);
        }

        private readonly IEnumerable<Collector> _collectors;

        private MultiCollector(IEnumerable<Collector> collectors)
        {
            _collectors = collectors;
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get
            {
                return _collectors.All(collector => collector.AcceptsDocsOutOfOrder);
            }
        }

        public override void Collect(int doc)
        {
            foreach (var collector in _collectors)
            {
                collector.Collect(doc);
            }
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            foreach (var collector in _collectors)
            {
                collector.SetNextReader(reader, docBase);
            }
        }

        public override void SetScorer(Scorer scorer)
        {
            foreach (var collector in _collectors)
            {
                collector.SetScorer(scorer);
            }
        }
    }
}