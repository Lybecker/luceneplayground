using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using Com.Lybecker.LuceneLibrary;
using System.Diagnostics;

namespace Com.Lybecker.LuceneLibrary.UnitTest
{
    [TestFixture]
    public class OpressFieldComparatorTest
    {
        private readonly Lucene.Net.Util.Version _version = Lucene.Net.Util.Version.LUCENE_29;
        private const string IdFieldName = "id";
        private const string NameFieldName = "name";
        private const int MaxResult = 5;
        private Query _query;
        private Directory _dir;
        private Analyzer _analyzer;
        private IndexWriter _indexWriter;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _dir = new RAMDirectory();
            _analyzer = new KeywordAnalyzer();

            _indexWriter = new IndexWriter(_dir, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            _indexWriter.AddDocument(CreateDocument(10, "Anders", 1));
            _indexWriter.AddDocument(CreateDocument(20, "Anders", 2));
            _indexWriter.AddDocument(CreateDocument(30, "Anders", 3));
            _indexWriter.AddDocument(CreateDocument(40, "Anders", 4));
            _indexWriter.AddDocument(CreateDocument(1000, "NeverFind", 10));
            _indexWriter.AddDocument(CreateDocument(1001, "NeverFind", 10));

            _indexWriter.Commit();

            _query = new TermQuery(new Term(NameFieldName, "Anders"));
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            _indexWriter.Dispose();
            _dir.Dispose();
        }

        [TestCase(new[] { 10, 20, 30, 40 })]
        [TestCase(new[] { 40, 30, 20, 10 })]
        [TestCase(new[] { 20, 30, 10, 40 })]
        [TestCase(new[] { 20, 40, 30, 10 })]
        public void SortOrder_Find_All_Test(int[] sortOrder)
        {
            var result = Search(_dir, _query, sortOrder);

            CollectionAssert.AreEqual(sortOrder, result);
        }

        [TestCase(new[] { 10, 20, 30, 40, 50 }, new[] { 10, 20, 30, 40 })]
        [TestCase(new[] { 5, 10, 20, 30, 40 }, new[] { 10, 20, 30, 40 })]
        [TestCase(new[] { 40, 5, 30, 20, 10 }, new[] { 40, 30, 20, 10 })]
        [TestCase(new[] { 20, 30, 10, 5, 40, 50 }, new[] { 20, 30, 10, 40 })]
        public void SortOrder_Find_Only_Those_Exists_Test(int[] requestSortOrder, int[] expectedSortOrder)
        {
            var result = Search(_dir, _query, requestSortOrder);

            CollectionAssert.AreEqual(expectedSortOrder, result);
        }

        [Test]
        public void SortOrder_Find_All_Reversed_Test()
        {
            var requestSortOrder = new[] { 20, 40, 30, 10 };
            var expectedSortOrder = requestSortOrder.Reverse().ToArray();

            var result = Search(_dir, _query, requestSortOrder, true);

            CollectionAssert.AreEqual(expectedSortOrder, result);
        }

        [Test]
        public void SortOrder_Find_With_Duplicate_Request_Sort_Order_Test()
        {
            var requestSortOrder = new[] { 10, 30, 30, 20, 40 };
            var expectedSortOrder = new[] { 10, 30, 20, 40 };

            var result = Search(_dir, _query, requestSortOrder);

            CollectionAssert.AreEqual(expectedSortOrder, result);
        }


        [Test]
        public void SortOrder_Find_Too_Many_Test()
        {
            var sortOrder = new[] { 20, 30 };
            var result = Search(_dir, _query, sortOrder);

            Assert.AreEqual(result.Count(), 4);

            CollectionAssert.IsNotSubsetOf(result, sortOrder);
            Assert.AreEqual(result.ElementAt(0), sortOrder[0]);
            Assert.AreEqual(result.ElementAt(1), sortOrder[1]);
        }

        [Test]
        public void SortOrder_With_Multiple_Segments_Test()
        {
            _indexWriter.AddDocument(CreateDocument(50, "Anders", 5));
            _indexWriter.Commit();

            var sortOrder = new[] { 20, 10, 30, 40, 50 };
            var result = Search(_dir, _query, sortOrder);
            CollectionAssert.AreEqual(sortOrder, result);
        }

        [Test]
        public void SortOrder_With_Multiple_Segments_Near_Realtime_Test()
        {
            _indexWriter.AddDocument(CreateDocument(50, "Anders", 5));

            var searcher = new IndexSearcher(_indexWriter.GetReader());

            var sortOrder = new[] { 20, 10, 30, 40, 50 };
            var result = Search(searcher, _query, sortOrder);
            CollectionAssert.AreEqual(sortOrder, result);

            searcher.Dispose();
        }

        [Test]
        public void SortOrder_With_MultiSearcher_Across_Indexes_Test()
        {
            var dir2 = new RAMDirectory();

            var indexWriter2 = new IndexWriter(dir2, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            indexWriter2.AddDocument(CreateDocument(100, "Anders", 0));

            indexWriter2.Commit();


            var searcher = new IndexSearcher(_dir, true);
            var searcher2 = new IndexSearcher(dir2, true);
            var parallelMultiSearcher = new ParallelMultiSearcher(new Searchable[] { searcher, searcher2 });


            var sortOrder = new[] { 20, 10, 100, 30, 40 };
            var result = Search(parallelMultiSearcher, _query, sortOrder);
            CollectionAssert.AreEqual(sortOrder, result);

            parallelMultiSearcher.Dispose();
            indexWriter2.Dispose();
            dir2.Dispose();
        }

        [Test]
        public void SortOrder_With_MultiIndexReader_Test()
        {
            var dir2 = new RAMDirectory();

            var indexWriter2 = new IndexWriter(dir2, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            indexWriter2.AddDocument(CreateDocument(100, "Anders", 0));
            indexWriter2.Commit();


            var mr = new MultiReader(new[]
                {
                    IndexReader.Open(_dir, true), 
                    IndexReader.Open(dir2, true)
                });
            var searcher = new IndexSearcher(mr);

            var sortOrder = new[] { 20, 10, 100, 30, 40 };
            var result = Search(searcher, _query, sortOrder);
            CollectionAssert.AreEqual(sortOrder, result);

            searcher.Dispose();
            indexWriter2.Dispose();
            dir2.Dispose();
        }

        private static IEnumerable<int> Search(Directory directory, Query query, IEnumerable<int> sortOrder, bool reversed = false)
        {
            var searcher = new IndexSearcher(directory, true);

            return Search(searcher, query, sortOrder, reversed);
        }

        private static IEnumerable<int> Search(Searcher searcher, Query query, IEnumerable<int> sortOrder, bool reversed = false)
        {
            var sort = new Sort(new SortField(IdFieldName, new OpressComparatorSource<int>(sortOrder, Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetInts), reversed));

            TopDocs hits = searcher.Search(query, null, MaxResult, sort);

            var result = new int[hits.ScoreDocs.Length];

            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                ScoreDoc match = hits.ScoreDocs[i];

                Document doc = searcher.Doc(match.Doc);
                result[i] = int.Parse(doc.Get(IdFieldName));

                Trace.WriteLine(string.Format("Matched id = {0}, Name = {1}",
                    doc.Get(IdFieldName), doc.Get(NameFieldName)));
            }
            searcher.Dispose();

            return result;
        }

        private static Document CreateDocument(int id, string name, float docBoost)
        {
            var doc = new Document();
            doc.Add(new Field(IdFieldName, id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field(NameFieldName, name, Field.Store.YES, Field.Index.ANALYZED));
            doc.Boost = docBoost;

            return doc;
        }
    }
}
