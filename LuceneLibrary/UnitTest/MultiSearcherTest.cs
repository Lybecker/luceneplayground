using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Com.Lybecker.LuceneLibrary.UnitTest
{
    [TestFixture]
    public class MultiSearcherTest
    {
        private readonly Lucene.Net.Util.Version _version = Lucene.Net.Util.Version.LUCENE_30;
        private const string IdFieldName = "id";
        private const string NameFieldName = "name";
        private const int MaxResult = 5;
        private RAMDirectory _directory1;
        private RAMDirectory _directory2;
        private IndexSearcher _searcher1;
        private IndexSearcher _searcher2;
        private PrefixQuery _query;

        [SetUp]
        public void Setup()
        {
            _directory1 = new RAMDirectory();
            _directory2 = new RAMDirectory();
            var analyzer = new KeywordAnalyzer();

            var indexWriter = new IndexWriter(_directory1, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            indexWriter.AddDocument(CreateDocument(10, "Anders"));
            indexWriter.AddDocument(CreateDocument(30, "Anne"));
            indexWriter.Commit();
            indexWriter.Dispose();

            var indexWriter2 = new IndexWriter(_directory2, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            indexWriter2.AddDocument(CreateDocument(40, "Andreas"));
            indexWriter2.AddDocument(CreateDocument(20, "Anja"));
            indexWriter2.AddDocument(CreateDocument(50, "Abe"));
            indexWriter2.Commit();
            indexWriter2.Dispose();

            _query = new PrefixQuery(new Term(NameFieldName, "A"));

            _searcher1 = new IndexSearcher(_directory1, true);
            _searcher2 = new IndexSearcher(_directory2, true);
        }

        [TearDown]
        public void TearDown()
        {
            _searcher1.Dispose();
            _searcher2.Dispose();

            _directory1.Dispose();
            _directory2.Dispose();
        }

        [Test]
        public void MultiSearcher_Sort_By_String_Test()
        {
            var multiSearcher = new MultiSearcher(
                new Searchable[] { _searcher1, _searcher2 });

            var result = Search(multiSearcher, _query, new Sort(new SortField(NameFieldName, SortField.STRING)));

            // Anders, Andreas, Anja, Anne
            var expected = new[] { 50, 10, 40, 20, 30 };

            CollectionAssert.AreEqual(expected, result);

            multiSearcher.Dispose();
        }

        [Test]
        public void ParallelMultiSearcher_Sort_By_String_Test()
        {
            var parallelMultiSearcher = new ParallelMultiSearcher(
                new Searchable[] { _searcher1, _searcher2 });

            var result = Search(parallelMultiSearcher, _query, new Sort(new SortField(NameFieldName, SortField.STRING)));

            // Anders, Andreas, Anja, Anne
            var expected = new[] { 50, 10, 40, 20, 30 };

            CollectionAssert.AreEqual(expected, result);

            parallelMultiSearcher.Dispose();
        }

        [Test]
        public void MultiSearcher_Sort_By_Int_Test()
        {
            var multiSearcher = new MultiSearcher(
                new Searchable[] { _searcher1, _searcher2 });

            var result = Search(multiSearcher, _query, new Sort(new SortField(IdFieldName, SortField.INT)));

            var expected = new[] { 10, 20, 30, 40, 50 };

            CollectionAssert.AreEqual(expected, result);

            multiSearcher.Dispose();
        }

        [Test]
        public void ParallelMultiSearcher_Sort_By_Int_Test()
        {
            var parallelMultiSearcher = new ParallelMultiSearcher(
                new Searchable[] { _searcher1, _searcher2 });

            var result = Search(parallelMultiSearcher, _query, new Sort(new SortField(IdFieldName, SortField.INT)));

            var expected = new[] { 10, 20, 30, 40, 50 };

            CollectionAssert.AreEqual(expected, result);

            parallelMultiSearcher.Dispose();
        }

        [Test]
        public void MultiSearcher_Sort_By_Custom_Int_Comparator_Test()
        {
            var multiSearcher = new MultiSearcher(
                new Searchable[] { _searcher1, _searcher2 });

            var result = Search(multiSearcher, _query, new Sort(new SortField(IdFieldName, new MyIntComparatorSource())));

            var expected = new[] { 10, 20, 30, 40, 50 };

            CollectionAssert.AreEqual(expected, result);

            multiSearcher.Dispose();
        }

        [Test]
        public void ParallelMultiSearcher_Sort_By_Custom_Int_Comparator_Test()
        {
            var parallelMultiSearcher = new ParallelMultiSearcher(
                new Searchable[] { _searcher1, _searcher2 });

            var result = Search(parallelMultiSearcher, _query, new Sort(new SortField(IdFieldName, new MyIntComparatorSource())));

            var expected = new[] { 10, 20, 30, 40, 50 };

            CollectionAssert.AreEqual(expected, result);

            parallelMultiSearcher.Dispose();
        }

        [Test]
        public void MultiSearcher_Sort_By_Custom_String_Comparator_Test()
        {
            var multiSearcher = new MultiSearcher(
                new Searchable[] { _searcher1, _searcher2 });

            var result = Search(multiSearcher, _query, new Sort(new SortField(NameFieldName, new MyStringComparatorSource())));

            // Abe, Anders, Andreas, Anja, Anne
            var expected = new[] { 50, 10, 40, 20, 30 };

            CollectionAssert.AreEqual(expected, result);

            multiSearcher.Dispose();
        }

        [Test]
        public void ParallelMultiSearcher_Sort_By_Custom_String_Comparator_Test()
        {
            var parallelMultiSearcher = new ParallelMultiSearcher(
                new Searchable[] { _searcher1, _searcher2 });

            var result = Search(parallelMultiSearcher, _query, new Sort(new SortField(NameFieldName, new MyStringComparatorSource())));

            // Abe, Anders, Andreas, Anja, Anne
            var expected = new[] { 50, 10, 40, 20, 30 };

            CollectionAssert.AreEqual(expected, result);

            parallelMultiSearcher.Dispose();
        }

        private static IEnumerable<int> Search(Searcher searcher, Query query, Sort sort)
        {
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

        private static Document CreateDocument(int id, string name)
        {
            var doc = new Document();
            doc.Add(new Field(IdFieldName, id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field(NameFieldName, name, Field.Store.YES, Field.Index.ANALYZED));

            return doc;
        }
    }
}
