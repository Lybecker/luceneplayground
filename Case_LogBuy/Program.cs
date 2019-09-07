using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Com.Lybecker.LuceneLibrary;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Spatial.Vector;
using Lucene.Net.Store;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace Case_LogBuy
{
    class Program
    {
        private const Lucene.Net.Util.Version Version = Lucene.Net.Util.Version.LUCENE_30;
        private const string SupplierFieldName = "supplier";
        private const string LocationNameFieldName = "locationName";
        private const string DiscountPctFieldName = "discountPct";
        private const string TitleFieldName = "title";
        private const string SpartialFieldName = "locationCoordinates";
        private const string SuggestionTextFieldName = "suggestionText";

        static void Main(string[] args)
        {
            //AutoCompleteSample();
            SearchSample();
        }

        public static void AutoCompleteSample()
        {
            Directory dir = new RAMDirectory();

            // Create n-edge grams for field suggestionText
            Analyzer analyzer = new EdgeNGramAnalyzer(Version);

            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            var docs = CreateAutoCompleteDocuments(GetDeals());

            foreach (var doc in docs)
            {
                indexWriter.AddDocument(doc);
            }

            indexWriter.Commit();
            indexWriter.Dispose();

            var searcherManager = new SearcherManager(dir);

            // Default sort by dicount pct desc.
            var sort = new Sort(new SortField(DiscountPctFieldName, SortField.INT, true));
            const int maxSuggestions = 5;

            for (;;)
            {
                Console.Write("Enter a text for auto completion and press enter: ");
                var input = Console.ReadLine();

                Query query = new TermQuery(new Term(SuggestionTextFieldName, input));

                var searcher = searcherManager.GetSearcher();
                TopDocs hits;
                try
                {
                    hits = searcher.Search(query, null, maxSuggestions, sort);
                }
                finally
                {
                    searcherManager.ReleaseSearcher(searcher);
                }

                foreach (ScoreDoc match in hits.ScoreDocs)
                {
                    Document doc = searcher.Doc(match.Doc);
                    Console.WriteLine("Matched: '{0}' in '{1}' by '{2}'", 
                        doc.Get(SuggestionTextFieldName), doc.Get(LocationNameFieldName), doc.Get(SupplierFieldName));
                }
            }
        }

        public static void SearchSample()
        {
            Directory dir = new RAMDirectory();

            Analyzer analyzer = new StandardAnalyzer(Version);

            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            SpatialContext ctx = SpatialContext.GEO;
            var strategy = new PointVectorStrategy(ctx, SpartialFieldName);
            //var precision = 8; // Precision 8 means down to 19 meter - higher precision consumes more memory
            //SpatialPrefixTree grid = new GeohashPrefixTree(ctx, precision);
            //var strategy = new RecursivePrefixTreeStrategy(grid, spartialFieldName);

            var docs = CreateSearchDocuments(GetDeals(), strategy);

            foreach (var doc in docs)
            {
                indexWriter.AddDocument(doc);
            }

            indexWriter.Commit();
            indexWriter.Dispose();

            // "Current" position
            Point littleMermaid = ctx.MakePoint(12.599239, 55.692848);

            //var parser = new QueryParser(Version, "title", analyzer);
            //Query q = parser.Parse("deal");
            Query q = new MatchAllDocsQuery(); // NOTE: MatchAllDocsQuery always returns score as 1.0

            // Add distance from current point to the scoring
            q = new DistanceCustomScoreQuery(q, strategy, littleMermaid);
            //q = new RecursivePrefixTreeStrategyDistanceCustomScoreQuery(q, strategy, littleMermaid, spartialFieldName);

            // Remove everything more than 2000 km away
            var filter = strategy.MakeFilter(new SpatialArgs(SpatialOperation.Intersects,
                                    ctx.MakeCircle(littleMermaid, DistanceUtils.Dist2Degrees(2000, DistanceUtils.EARTH_MEAN_RADIUS_KM))));

            // Ensures the most recent searcher is used without destroying the Lucene IndexReader cache (via NRT)
            var searcherManager = new SearcherManager(dir);

            var collector = new GroupTopDocsCollector(5, SupplierFieldName);

            var searcher = searcherManager.GetSearcher();
            try
            {
                searcher.Search(q, filter, collector);
            }
            finally
            {
                searcherManager.ReleaseSearcher(searcher);
            }

            var hits = collector.GroupTopDocs();

            Console.WriteLine("Found {0} document(s) that matched query '{1}':", hits.TotalHits, q);
            foreach (var match in hits.GroupScoreDocs)
            {
                Document doc = searcher.Doc(match.Doc);
                Console.WriteLine("Best match '{0}' in group '{1}' with count {2} (MaxDoc: Score {3} Location '{4}')",
                    doc.Get(TitleFieldName), match.GroupFieldValue, match.GroupCount, match.Score, doc.Get(LocationNameFieldName));
            }
        }

        public static IEnumerable<Document> CreateSearchDocuments(IList<Deal> deals, SpatialStrategy strategy)
        {
            var docs = new List<Document>(deals.Count());
            var ctx = strategy.GetSpatialContext();

            foreach (var deal in deals)
            {
                foreach (var location in deal.Locations)
                {
                    var doc = new Document();
                    doc.Add(new Field(TitleFieldName, deal.Title, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field(SupplierFieldName, deal.Supplier, Field.Store.YES, Field.Index.NOT_ANALYZED));
                    doc.Add(new Field(LocationNameFieldName, location.Name, Field.Store.YES, Field.Index.NOT_ANALYZED));

                    foreach (var f in strategy.CreateIndexableFields(ctx.MakePoint(location.Latitude, location.Longitude)))
                    {
                        doc.Add(f);
                    }

                    docs.Add(doc);
                }
            }

            return docs;
        }

        public static IEnumerable<Document> CreateAutoCompleteDocuments(IList<Deal> deals)
        {
            var docs = new List<Document>(deals.Count());

            foreach (var deal in deals)
            {
                var doc = new Document();
                doc.Add(new Field(SuggestionTextFieldName, deal.Title, Field.Store.YES, Field.Index.ANALYZED));
                foreach (var location in deal.Locations)
                {
                    doc.Add(new Field(LocationNameFieldName, location.Name, Field.Store.YES, Field.Index.NOT_ANALYZED));
                }
                doc.Add(new Field(SupplierFieldName, deal.Supplier, Field.Store.YES, Field.Index.NOT_ANALYZED));
                doc.Add(new Field(DiscountPctFieldName, deal.DiscountPct.ToString(CultureInfo.InvariantCulture), Field.Store.NO, Field.Index.NOT_ANALYZED));

                docs.Add(doc);
            }

            return docs;
        }

        public static IList<Deal> GetDeals()
        {
            return new List<Deal>
                       {
                           new Deal("Deal A", "Shell", 10, new List<Location>() 
                                                               {
                                                                   new Location("Hillerød", 12.301268, 55.929526),
                                                                   new Location("Høje Taastrup", 12.276069, 55.644218),
                                                               }),
                           new Deal("Deal C", "DSB", 20, new List<Location>()
                                                             {
                                                                 new Location("Københavns Hovedbane", 12.564513, 55.672786),
                                                                 new Location("Høje Taastrup", 12.276069, 55.644218),
                                                             }),
                           new Deal("Deal B", "Tuborg", 15, new List<Location>() { new Location("Hellerup", 12.574724, 55.727527)}),
                           new Deal("Deal D", "Microsoft", 25, new List<Location>()
                                                                   {
                                                                       new Location("Lyngby", 12.503276, 55.769723),
                                                                       new Location("Vedbæk", 12.561758, 55.857609),
                                                                   }),
                           new Deal("Deal E in Sydney", "Sydney Herald", 50, new List<Location>() { new Location("Sydney", 151.206955, -33.869629)}),
                       };
        }

        public static string Explain(IndexSearcher searcher, Query query, ScoreDoc match)
        {
            Explanation explanation = searcher.Explain(query, match.Doc);
            return explanation.ToString();
        }
    }

    /// <summary>
    /// Only single-valued fiels
    /// </summary>
    public class DistanceCustomScoreQuery : CustomScoreQuery
    {
        private readonly PointVectorStrategy _strategy;
        private readonly Point _origin;

        public DistanceCustomScoreQuery(Query subQuery, PointVectorStrategy strategy, Point origin)
            : base(subQuery)
        {
            _strategy = strategy;
            _origin = origin;
        }

        public override string Name()
        {
            return "DistanceCustomScoreQuery";
        }

        protected override CustomScoreProvider GetCustomScoreProvider(IndexReader reader)
        {
            return new DistranceCustomScoreProvider(reader, _strategy, _origin);
        }

        class DistranceCustomScoreProvider : CustomScoreProvider
        {
            private readonly PointVectorStrategy _strategy;
            private readonly Point _originPt;
            private readonly double[] _currentReaderValuesX;
            private readonly double[] _currentReaderValuesY;

            public DistranceCustomScoreProvider(IndexReader reader, PointVectorStrategy strategy, Point origin)
                : base(reader)
            {
                _strategy = strategy;
                _originPt = origin;
                _currentReaderValuesX = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetDoubles(reader, _strategy.GetFieldNameX());
                _currentReaderValuesY = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetDoubles(reader, _strategy.GetFieldNameY());
            }

            public override float CustomScore(int doc, float subQueryScore, float valSrcScore)
            {
                var dist = CalculateDistance(doc);

                var distanceInHundredsOfMeters = (float)DistanceUtils.Degrees2Dist(dist, DistanceUtils.EARTH_EQUATORIAL_RADIUS_KM) * 10;

                if (distanceInHundredsOfMeters < 1)
                    distanceInHundredsOfMeters = 1;

                // Perhaps another distance boost algorithm?
                return subQueryScore + 1 / distanceInHundredsOfMeters;
            }

            //public override Explanation CustomExplain(int doc, Explanation subQueryExpl, Explanation valSrcExpl)
            //{
            //    float valSrcScore = valSrcExpl == null ? 0 : valSrcExpl.Value;
            //    var exp = new Explanation(valSrcScore + subQueryExpl.Value, "custom score: sum of:");
            //    exp.AddDetail(subQueryExpl);
            //    if (valSrcExpl != null)
            //    {
            //        exp.AddDetail(valSrcExpl);
            //    }
            //    return exp;
            //}

            //public override Explanation CustomExplain(int doc, Explanation subQueryExpl, Explanation[] valSrcExpls)
            //{

            //    return base.CustomExplain(doc, subQueryExpl, valSrcExpls);
            //}

            private float CalculateDistance(int doc)
            {
                var x = _currentReaderValuesX[doc];
                var y = _currentReaderValuesY[doc];

                var context = _strategy.GetSpatialContext();

                var pt = context.MakePoint(x, y);

                return (float)context.GetDistCalc().Distance(pt, _originPt);
            }
        }
    }

    /// <summary>
    /// Works with multi-valued fields
    /// Consumes alot more memory!
    /// </summary>
    public class RecursivePrefixTreeStrategyDistanceCustomScoreQuery : CustomScoreQuery
    {
        private readonly RecursivePrefixTreeStrategy _strategy;
        private readonly Point _origin;
        private readonly PointPrefixTreeFieldCacheProvider _cacheProvider;

        public RecursivePrefixTreeStrategyDistanceCustomScoreQuery(Query subQuery, RecursivePrefixTreeStrategy strategy, Point origin, String shapeField)
            : base(subQuery)
        {
            _strategy = strategy;
            _origin = origin;
            _cacheProvider = new PointPrefixTreeFieldCacheProvider(_strategy.GetGrid(), shapeField, 255);
        }

        public override string Name()
        {
            return "RecursivePrefixTreeStrategyDistanceCustomScoreQuery";
        }

        protected override CustomScoreProvider GetCustomScoreProvider(IndexReader reader)
        {
            return new RecursivePrefixTreeStrategyDistranceCustomScoreProvider(reader, _strategy, _cacheProvider, _origin);
        }

        class RecursivePrefixTreeStrategyDistranceCustomScoreProvider : CustomScoreProvider
        {
            private readonly Point _originPt;
            private readonly DistanceCalculator _calculator;
            private readonly ShapeFieldCache<Point> _cache;
            private readonly float _nullValue;

            public RecursivePrefixTreeStrategyDistranceCustomScoreProvider(IndexReader reader, RecursivePrefixTreeStrategy strategy, PointPrefixTreeFieldCacheProvider cacheProvider, Point origin)
                : base(reader)
            {
                var ctx = strategy.GetSpatialContext();

                _originPt = origin;

                _calculator = ctx.GetDistCalc();
                _nullValue = (ctx.IsGeo() ? 180 : float.MaxValue);
                _cache = cacheProvider.GetCache(reader);
            }

            public override float CustomScore(int doc, float subQueryScore, float valSrcScore)
            {
                var dist = CalculateDistance(doc);

                var distanceInHundredsOfMeters = (float)DistanceUtils.Degrees2Dist(dist, DistanceUtils.EARTH_EQUATORIAL_RADIUS_KM) * 10;

                if (distanceInHundredsOfMeters < 1)
                    distanceInHundredsOfMeters = 1;

                Console.WriteLine("docid {0} - dist = {1}, subscore = {3},  score = {2}", doc, dist, subQueryScore + 1 / distanceInHundredsOfMeters, subQueryScore);

                // Perhaps another distance boost algorithm?
                return subQueryScore + 1 / distanceInHundredsOfMeters;
            }

            //public override Explanation CustomExplain(int doc, Explanation subQueryExpl, Explanation valSrcExpl)
            //{
            //    float valSrcScore = valSrcExpl == null ? 0 : valSrcExpl.Value;
            //    var exp = new Explanation(valSrcScore + subQueryExpl.Value, "custom score: sum of:");
            //    exp.AddDetail(subQueryExpl);
            //    if (valSrcExpl != null)
            //    {
            //        exp.AddDetail(valSrcExpl);
            //    }
            //    return exp;
            //}

            //public override Explanation CustomExplain(int doc, Explanation subQueryExpl, Explanation[] valSrcExpls)
            //{

            //    return base.CustomExplain(doc, subQueryExpl, valSrcExpls);
            //}

            private float CalculateDistance(int doc)
            {
                var points = _cache.GetShapes(doc);

                if (points != null)
                {

                    double v = _calculator.Distance(_originPt, points[0]);
                    for (int i = 1; i < points.Count; i++)
                    {
                        v = Math.Min(v, _calculator.Distance(_originPt, points[i]));
                    }
                    return (float)v;
                }

                return _nullValue;
            }
        }
    }

    public class Deal
    {
        public Deal(string title, string supplier, int discountPct, IList<Location> locations)
        {
            Title = title;
            Supplier = supplier;
            DiscountPct = discountPct;
            Locations = locations;
        }

        public string Title { get; set; }
        public string Supplier { get; set; }
        public int DiscountPct { get; set; }
        public IList<Location> Locations { get; set; }
    }

    public class Location
    {
        public Location(string city, double latitude, double longitude)
        {
            Name = city;
            Latitude = latitude;
            Longitude = longitude;
        }

        /// <summary>
        /// x-axis
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// y-axis
        /// </summary>
        public double Longitude { get; set; }

        public string Name { get; set; }
    }
}