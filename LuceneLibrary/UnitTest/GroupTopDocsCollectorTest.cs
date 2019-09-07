using System;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Com.Lybecker.LuceneLibrary.UnitTest
{
    [TestFixture]
    public class GroupTopDocsCollectorTest
    {
        private const Lucene.Net.Util.Version Version = Lucene.Net.Util.Version.LUCENE_30;
        private const string TextFieldName = "text";
        private const string GroupFieldName = "group";
        private const string ScoreFieldName = "score";
        private RAMDirectory _directory;
        private IndexSearcher _searcher;

        [SetUp]
        public void Setup()
        {
            _directory = new RAMDirectory();
            var analyzer = new StandardAnalyzer(Version);
            
            var indexWriter = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            indexWriter.AddDocument(CreateDocument("Lego Billund", "Lego", 1));
            indexWriter.AddDocument(CreateDocument("Lego California", "Lego", 2));
            indexWriter.AddDocument(CreateDocument("Disney Paris", "Disney", 2));
            indexWriter.Commit(); // multiple commits to create multiple segments.
            indexWriter.AddDocument(CreateDocument("Disney Orlando", "Disney", 3));
            indexWriter.Commit();
            indexWriter.AddDocument(CreateDocument("Disney Tokyo", "Disney", 1));
            indexWriter.AddDocument(CreateDocument("Bonbon Land", "Bonbon", 2));
            indexWriter.Commit();
            indexWriter.Dispose();

            _searcher = new IndexSearcher(_directory, true);
        }

        [Test, Description("Tester - does not assert anything.")]
        public void Tester()
        {
            var collector = new GroupTopDocsCollector(5, GroupFieldName);

            _searcher.Search(new FieldScoreQuery(ScoreFieldName, FieldScoreQuery.Type.INT), collector);

            var topDocs = collector.GroupTopDocs();

            foreach (var group in topDocs.GroupScoreDocs)
            {
                Document doc = _searcher.Doc(group.Doc);
                Console.WriteLine("'{0}' \t Group '{1}' count: {2} (Max Score {3})", 
                    doc.Get(TextFieldName), group.GroupFieldValue, group.GroupCount, group.Score);
            }
        }

        [Test]
        public void SortByScoreDesc()
        {
            var collector = new GroupTopDocsCollector(100, GroupFieldName);

            _searcher.Search(new FieldScoreQuery(ScoreFieldName, FieldScoreQuery.Type.INT), collector);

            var topDocs = collector.GroupTopDocs();

            ScoreDoc previousResult = null;

            foreach (var result in topDocs.GroupScoreDocs)
            {
                if (previousResult != null)
                    Assert.GreaterOrEqual(previousResult.Score, result.Score);

                previousResult = result;
            }
        }

        [Test]
        public void GroupCountMatches()
        {
            var collector = new GroupTopDocsCollector(100, GroupFieldName);

            _searcher.Search(new FieldScoreQuery(ScoreFieldName, FieldScoreQuery.Type.INT), collector);

            var topDocs = collector.GroupTopDocs();

            Assert.AreEqual(topDocs.GroupScoreDocs.Count(), 3);
            Assert.AreEqual(topDocs.GroupScoreDocs[0].GroupCount, 3, string.Format("Group {0}", topDocs.GroupScoreDocs[0].GroupFieldValue));
            Assert.AreEqual(topDocs.GroupScoreDocs[1].GroupCount, 2, string.Format("Group {0}", topDocs.GroupScoreDocs[1].GroupFieldValue));
            Assert.AreEqual(topDocs.GroupScoreDocs[2].GroupCount, 1, string.Format("Group {0}", topDocs.GroupScoreDocs[2].GroupFieldValue));
        }


        [Test]
        public void HitCountRequestedMoreThanResultCount()
        {
            var requestedHitCount = 100;

            var collector = new GroupTopDocsCollector(requestedHitCount, GroupFieldName);

            _searcher.Search(new MatchAllDocsQuery(), collector);

            var topDocs = collector.GroupTopDocs();

            Assert.Greater(requestedHitCount, topDocs.TotalHits);
        }

        [Test]
        public void HitCountRequestedMatchResultCount()
        {
            var requestedHitCount = 2;

            var collector = new GroupTopDocsCollector(requestedHitCount, GroupFieldName);

            _searcher.Search(new MatchAllDocsQuery(), collector);

            var topDocs = collector.GroupTopDocs();

            Assert.Greater(topDocs.TotalHits, requestedHitCount, "Test data should return more than requested hit count otherwise this test does not make sence.");
            Assert.AreEqual(requestedHitCount, topDocs.GroupScoreDocs.Count());
        }

        private static Document CreateDocument(string text, string group, int score)
        {
            var doc = new Document();
            doc.Add(new Field(TextFieldName, text, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field(GroupFieldName, group, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field(ScoreFieldName, score.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));

            return doc;
        }

        [TearDown]
        public void TearDown()
        {
            _searcher.Dispose();
        }
    }
}
