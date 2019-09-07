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

namespace Filters
{
    class Program
    {
        static void Main(string[] args)
        {
            Lucene.Net.Util.Version version = Lucene.Net.Util.Version.LUCENE_29;

            Directory dir = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(version);

            var docs = CreateDocuments();
            AddToIndex(docs, dir, analyzer);

            // Search for the content
            var parser = new MultiFieldQueryParser(version, new[] { "name" }, analyzer);
            Query q = parser.Parse("An*");

            Filter filter = TermRangeFilter.More("date", DateTools.DateToString(new DateTime(2011, 1, 1), DateTools.Resolution.DAY));

            var searcher = new IndexSearcher(dir, true);

            TopDocs hits = searcher.Search(q, filter, 5, Sort.RELEVANCE);

            Console.WriteLine("Found {0} document(s) that matched query '{1}':", hits.TotalHits, q);
            foreach (ScoreDoc match in hits.ScoreDocs)
            {
                Document doc = searcher.Doc(match.Doc);
                Console.WriteLine("Matched id = {0}, Name = {1}", doc.Get("id"), doc.Get("name"));
            }
            searcher.Close();
        }

        private static void AddToIndex(IEnumerable<Document> docs, Directory dir, Analyzer analyzer)
        {
            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            foreach (var document in docs)
            {
                indexWriter.AddDocument(document);
            }

            indexWriter.Commit();
            indexWriter.Close();
        }

        public static IEnumerable<Document> CreateDocuments()
        {
            var docs = new List<Document>();

            var doc = new Document();
            doc.Add(new Field("id", "0", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("name", "Anders", Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(
                new Field(
                    "date",
                    DateTools.DateToString(new DateTime(2010,1,1), DateTools.Resolution.DAY),
                    Field.Store.NO,
                    Field.Index.NOT_ANALYZED_NO_NORMS));
            docs.Add(doc);

            doc = new Document();
            doc.Add(new Field("id", "1", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("name", "Anja", Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(
                new Field(
                    "date",
                    DateTools.DateToString(new DateTime(2011, 1, 1), DateTools.Resolution.DAY),
                    Field.Store.NO,
                    Field.Index.NOT_ANALYZED_NO_NORMS));
            docs.Add(doc);

            doc = new Document();
            doc.Add(new Field("id", "2", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("name", "Andersine", Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(
                new Field(
                    "date",
                    DateTools.DateToString(new DateTime(2012, 1, 1), DateTools.Resolution.DAY),
                    Field.Store.NO,
                    Field.Index.NOT_ANALYZED_NO_NORMS));
            docs.Add(doc);

            return docs;
        }
    }
}