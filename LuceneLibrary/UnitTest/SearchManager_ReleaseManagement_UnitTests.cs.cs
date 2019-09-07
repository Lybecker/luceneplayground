using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Com.Lybecker.LuceneLibrary.UnitTest
{
    [TestFixture]
    public class SearchManager_ReleaseManagement_UnitTests
    {
        [Test]
        public void Check_IndexReader_Gets_Closed()
        {
            var dir = new RAMDirectory();
            var writer = new IndexWriter(dir, new SimpleAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
            var manager = new SearcherManager(dir);

            var searcher1 = manager.GetSearcher();

            manager.ReleaseSearcher(searcher1);
            manager.ReleaseSearcher(searcher1); // The SearcherManager ctor creates an IndexReader.

            Assert.Throws<AlreadyClosedException>(() => manager.ReleaseSearcher(searcher1));
        }

        [Test]
        public void Check_IndexReader_Get_Closed_When_Creating_New()
        {
            var dir = new RAMDirectory();
            var writer = new IndexWriter(dir, new SimpleAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
            var manager = new SearcherManager(dir);

            var searcher1 = manager.GetSearcher();

            // Change the index
            writer.AddDocument(new Document());
            writer.Commit();

            // Reopen the IndexReader
            manager.MaybeReopen();

            var searcher2 = manager.GetSearcher();
            Assert.AreNotEqual(searcher1.IndexReader, searcher2.IndexReader, "A new IndexReader was not created.");
            manager.ReleaseSearcher(searcher2);

            manager.ReleaseSearcher(searcher1);
            Assert.Throws<AlreadyClosedException>(() => manager.ReleaseSearcher(searcher1));

        }
    }
}
