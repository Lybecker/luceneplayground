using System;
using System.Collections.Generic;
using Com.Lybecker.LuceneLibrary.Spartial;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.BBox;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Spatial.Vector;
using Lucene.Net.Store;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using Version = Lucene.Net.Util.Version;

namespace Spartial
{
    class Program
    {
        static void Main(string[] args)
        {
            const Version version = Lucene.Net.Util.Version.LUCENE_30;

            Directory dir = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(version);

            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            SpatialContext ctx = SpatialContext.GEO;

            // BBoxStrategy
            //var strategy = new BBoxStrategy(ctx, "spartial"); // Can only index and search by retangle

            // TermQueryPrefixTreeStrategy
            //SpatialPrefixTree grid = new GeohashPrefixTree(ctx, 8);
            //var strategy = new TermQueryPrefixTreeStrategy(grid, "spartial"); // Only supports SpatialOperation.Intersects

            // RecursivePrefixTreeStrategy
            SpatialPrefixTree grid = new GeohashPrefixTree(ctx, 8);
            var strategy = new RecursivePrefixTreeStrategy(grid, "spartial");

            

            // PointVectorStrategy
            //var strategy = new PointVectorStrategy(ctx, "pointvector");

            foreach (var doc in CreateDocuments(strategy))
            {
                indexWriter.AddDocument(doc);
            }
            indexWriter.Commit();
            indexWriter.Dispose();


            var searcher = new IndexSearcher(dir, true);

            Point littleMermaid = ctx.MakePoint(12.599239, 55.692848);



            // PointVectorStrategy
            //TopDocs hits = DistanceQueryAndSort_PointVectorStrategy(searcher, strategy, littleMermaid);
            //TopDocs hits = DistanceFilter_PointVectorStrategy(searcher, strategy, littleMermaid);
            //TopDocs hits = DistranceScore_PointVectorStrategy(searcher, strategy, littleMermaid);

            // TermQueryPrefixTreeStrategy
            //TopDocs hits = DistanceFilter_TermQueryPrefixTreeStrategy(searcher, strategy, littleMermaid);

            // RecursivePrefixTreeStrategy
            TopDocs hits = DistanceFilter_RecursivePrefixTreeStrategy(searcher, strategy, littleMermaid);

            Console.WriteLine("Found {0} document(s) that matched query:", hits.TotalHits);
            foreach (ScoreDoc match in hits.ScoreDocs)
            {
                Document doc = searcher.Doc(match.Doc);
                Console.WriteLine("Matched {0} (score: {1})", doc.Get("id"), match.Score);
            }
            searcher.Dispose();
        }

        public static TopDocs DistanceFilter_RecursivePrefixTreeStrategy(Searcher searcher, RecursivePrefixTreeStrategy strategy, Point myLocation)
        {
            var ctx = strategy.GetSpatialContext();

            var q = new MatchAllDocsQuery();
            var filter = strategy.MakeFilter(new SpatialArgs(SpatialOperation.Intersects,
                                                ctx.MakeCircle(myLocation,
                                                               DistanceUtils.Dist2Degrees(2000,
                                                                                          DistanceUtils
                                                                                              .EARTH_MEAN_RADIUS_KM))));

            // Reverse sorting...
            var sortField = new SortField(null, SortField.SCORE, true);
            
            TopDocs hits = searcher.Search(q, filter, 100, new Sort(sortField));

            return hits;
        }

        public static TopDocs DistanceFilter_TermQueryPrefixTreeStrategy(Searcher searcher, TermQueryPrefixTreeStrategy strategy, Point myLocation)
        {
            var ctx = strategy.GetSpatialContext();

            var q = new MatchAllDocsQuery();
            var filter = strategy.MakeFilter(new SpatialArgs(SpatialOperation.Intersects,
                                                ctx.MakeCircle(myLocation,
                                                               DistanceUtils.Dist2Degrees(2000,
                                                                                          DistanceUtils
                                                                                              .EARTH_MEAN_RADIUS_KM))));
            TopDocs hits = searcher.Search(q, filter, 100);

            return hits;
        }

        public static TopDocs DistranceScore_PointVectorStrategy(Searcher searcher, PointVectorStrategy strategy,
                                                                 Point myLocation)
        {
            var vs = strategy.MakeDistanceValueSource(myLocation);

            var q = new FunctionQuery(vs);  // Returns a score for each document based on a ValueSource

            TopDocs hits = searcher.Search(q, null, 100);

            return hits;
        }

        public static TopDocs DistanceFilter_PointVectorStrategy(Searcher searcher, PointVectorStrategy strategy, Point myLocation)
        {
            var ctx = strategy.GetSpatialContext();

            var q = new MatchAllDocsQuery();
            var filter = strategy.MakeFilter(new SpatialArgs(SpatialOperation.IsWithin,
                                                ctx.MakeCircle(myLocation,
                                                               DistanceUtils.Dist2Degrees(2000,
                                                                                          DistanceUtils
                                                                                              .EARTH_MEAN_RADIUS_KM))));
            TopDocs hits = searcher.Search(q, filter, 100);

            return hits;
        }

        public static TopDocs DistanceQueryAndSort_PointVectorStrategy(Searcher searcher, PointVectorStrategy strategy, Point myLocation)
        {
            var ctx = strategy.GetSpatialContext();

            var q = strategy.MakeQuery(new SpatialArgs(
                SpatialOperation.IsWithin,
                ctx.MakeCircle(myLocation, DistanceUtils.Dist2Degrees(20000, DistanceUtils.EARTH_MEAN_RADIUS_KM))));

            TopDocs hits = searcher.Search(q, null, 100, new Sort(new SortField("pointvector", new PointVectorDistanceFieldComparatorSource(myLocation, strategy))));

            return hits;
        }

        public static IList<Document> CreateDocuments(SpatialStrategy strategy)
        {
            var docs = new List<Document>();
            var ctx = strategy.GetSpatialContext();

            docs.Add(CreateDocument("London", ctx.MakePoint(0.11, 51.5), strategy));
            docs.Add(CreateDocument("New York", ctx.MakePoint(-74.005970, 40.714270), strategy));
            docs.Add(CreateDocument("Copenhagen", ctx.MakePoint(12.567600, 55.675678), strategy));
            docs.Add(CreateDocument("Sydney", ctx.MakePoint(151.206955, -33.869629), strategy));
            docs.Add(CreateDocument("Paris", ctx.MakePoint(2.341200, 48.856930), strategy));
            docs.Add(CreateDocument("Berlin", ctx.MakePoint(13.376980, 52.516071), strategy));

            return docs;
        }

        public static Document CreateDocument(string id, Shape shape, SpatialStrategy strategy)
        {
            var document = new Document();

            document.Add(new Field("id", id, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));

            foreach (var f in strategy.CreateIndexableFields(shape))
            {
                document.Add(f);
            }

            //document.Add(new Field(strategy.GetFieldName(), strategy.GetSpatialContext().ToString(shape), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));

            return document;
        }
    }
}