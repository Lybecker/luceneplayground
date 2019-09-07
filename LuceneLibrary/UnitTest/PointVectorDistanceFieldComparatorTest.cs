using System.Collections.Generic;
using System.Linq;
using Com.Lybecker.LuceneLibrary.Spartial;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Vector;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace Com.Lybecker.LuceneLibrary.UnitTest
{
    [TestFixture]
    public class PointVectorDistanceFieldComparatorTest
    {
        private const Version Version = Lucene.Net.Util.Version.LUCENE_30;
        private Directory _directory;
        private const string SpartialFieldName = "spartial";
        private const string IdFieldName = "id";
        private SpatialContext _ctx = SpatialContext.GEO;
        private PointVectorStrategy _strategy;
        private List<City> _cities;

        [SetUp]
        public void SetUp()
        {
            _cities = new List<City>()
                {
                    new City("London", 0.11, 51.5),
                    new City("Copenhagen", 12.567600, 55.675678),
                    new City("New York",-74.005970, 40.714270),
                    new City("Sydney", 151.206955, -33.869629),
                    new City("Paris", 2.341200, 48.856930),
                    new City("Berlin", 13.376980, 52.516071)
                };

            _directory = new RAMDirectory();
        }

        private void IndexDocuments(IEnumerable<City> cities)
        {
            Analyzer analyzer = new StandardAnalyzer(Version);

            var indexWriter = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            _ctx = SpatialContext.GEO;
            _strategy = new PointVectorStrategy(_ctx, SpartialFieldName);

            foreach (var doc in CreateDocuments(cities, _strategy))
            {
                indexWriter.AddDocument(doc);
            }
            indexWriter.Commit();
            indexWriter.Dispose();
        }

        [Test]
        public void SortAsc()
        {
            IndexDocuments(_cities);

            var reverse = false;
            var expected = new List<string>()
                               {
                                   "Copenhagen",
                                   "Berlin",
                                   "London",
                                   "Paris",
                                   "New York",
                                   "Sydney"
                               };

            var result = Search(reverse);

            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void SortDec()
        {
            IndexDocuments(_cities);

            var reverse = true;
            var expected = new List<string>()
                               {
                                   "Sydney",
                                   "New York",
                                   "Paris",
                                   "London",
                                   "Berlin",
                                   "Copenhagen"
                              
                               };

            var result = Search(reverse);

            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void SortDec_With_Multiple_Index_Segments()
        {
            IndexDocuments(_cities.Take(2));
            IndexDocuments(_cities.Skip(2));

            var reverse = true;
            var expected = new List<string>()
                               {
                                   "Sydney",
                                   "New York",
                                   "Paris",
                                   "London",
                                   "Berlin",
                                   "Copenhagen"
                              
                               };

            var result = Search(reverse);

            CollectionAssert.AreEqual(expected, result);
        }

        private IEnumerable<string> Search(bool reverse)
        {
            var searcher = new IndexSearcher(_directory, true);

            Point littleMermaid = _ctx.MakePoint(12.599239, 55.692848);

            var sort = new Sort(new SortField(SpartialFieldName, new PointVectorDistanceFieldComparatorSource(littleMermaid, _strategy), reverse));

            var q = _strategy.MakeQuery(new SpatialArgs(SpatialOperation.IsWithin, _ctx.MakeCircle(littleMermaid.GetX(), littleMermaid.GetY(), DistanceUtils.Dist2Degrees(20000, DistanceUtils.EARTH_MEAN_RADIUS_KM))));

            TopDocs hits = searcher.Search(q, null, 100, sort);


            var result = new string[hits.ScoreDocs.Length];

            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                ScoreDoc match = hits.ScoreDocs[i];

                Document doc = searcher.Doc(match.Doc);
                result[i] = doc.Get(IdFieldName);
            }
            searcher.Dispose();

            return result;
        }


        public static IList<Document> CreateDocuments(IEnumerable<City> cities, SpatialStrategy strategy)
        {
            var docs = new List<Document>();
            var ctx = strategy.GetSpatialContext();

            foreach (var city in cities)
            {
                docs.Add(CreateDocument(city.Name, ctx.MakePoint(city.Latitude, city.Longitude), strategy));
            }

            return docs;
        }


        public static Document CreateDocument(string name, Shape shape, SpatialStrategy strategy)
        {
            var document = new Document();

            document.Add(new Field(IdFieldName, name, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));

            foreach (var f in strategy.CreateIndexableFields(shape))
            {
                document.Add(f);
            }

            return document;
        }

        public class City
        {
            public City(string name, double latitude, double longitude)
            {
                Name = name;
                Latitude = latitude;
                Longitude = longitude;
            }

            public string Name { get; set; }
            /// <summary>
            /// x-axis
            /// </summary>
            public double Latitude { get; set; }
            /// <summary>
            /// y-axis
            /// </summary>
            public double Longitude { get; set; }
        }
    }
}
