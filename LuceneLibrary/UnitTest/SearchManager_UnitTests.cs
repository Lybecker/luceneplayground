using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Com.Lybecker.LuceneLibrary.UnitTest
{
    [TestFixture]
    public class SearchManager_UnitTests
    {
        private const string FieldName = "text";
        private static readonly Lucene.Net.Util.Version LuceneVersion = Lucene.Net.Util.Version.LUCENE_29;
        private readonly Analyzer _analyzer = new StandardAnalyzer(LuceneVersion);
        private Directory _dir;
        private IndexWriter _indexWriter;

        [SetUp]
        public void InitTest()
        {
            _dir = new RAMDirectory();
            _indexWriter = new IndexWriter(_dir, _analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
        }

        [Test]
        public void Find_Single_Item()
        {
            var anders = "Anders";

            _indexWriter.AddDocument(CreateDocument(anders));
            _indexWriter.Commit();

            var manager = new SearcherManager(_dir);
            var result = Search(manager, anders);

            CollectionAssert.AreEquivalent(result, new[] { anders });
        }

        [Test]
        public void Find_Item_Add_Item_And_Find_Both_Items()
        {
            var anders = "Anders";
            var anja = "Anja";

            _indexWriter.AddDocument(CreateDocument(anders));
            _indexWriter.Commit();

            var manager = new SearcherManager(_dir);

            var result = Search(manager, anders);

            CollectionAssert.AreEquivalent(result, new[] { anders }, "Did not find {0}", anders);

            _indexWriter.AddDocument(CreateDocument(anja));
            _indexWriter.Commit();

            manager.MaybeReopen();

            result = Search(manager, "An*");

            CollectionAssert.AreEquivalent(result, new[] { anders, anja });
        }

        [TearDown]
        public void TearDownTest()
        {
            _indexWriter.Dispose();
            _dir.Dispose();
        }

        private static Document CreateDocument(string content)
        {
            var document = new Document();
            document.Add(new Field(FieldName, content, Field.Store.YES, Field.Index.ANALYZED));
            return document;
        }

        private string[] Search(SearcherManager manager, string searchString)
        {
            var parser = new QueryParser(LuceneVersion, FieldName, _analyzer);

            Query q = parser.Parse(searchString);

            var searcher = manager.GetSearcher();

            TopDocs hits = searcher.Search(q, null, 100, Sort.RELEVANCE);

            var resultStrings = new List<string>(hits.ScoreDocs.Length);
            resultStrings.AddRange(hits.ScoreDocs.Select(match => searcher.Doc(match.Doc)).Select(doc => doc.Get(FieldName)));

            manager.ReleaseSearcher(searcher);

            return resultStrings.ToArray();
        }
    }
}
