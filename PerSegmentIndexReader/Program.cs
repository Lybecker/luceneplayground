using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace PerSegmentIndexReader
{
    class Program
    {
        private const string FieldName = "text";
        private static readonly Lucene.Net.Util.Version LuceneVersion = Lucene.Net.Util.Version.LUCENE_29;

        static void Main(string[] args)
        {
            Directory dir = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(LuceneVersion);

            var indexWriter = new IndexWriter(dir, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);


            indexWriter.AddDocument(CreateDocument("Anders"));
            indexWriter.Commit();
            indexWriter.Close();



            var reader = IndexReader.Open(dir, true);
            var searcher = new IndexSearcher(reader);
            Search(searcher, analyzer, "Anders");


            indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            indexWriter.AddDocument(CreateDocument("Anja"));
            indexWriter.Commit();
            indexWriter.Close();

            if (reader.IsCurrent() == false)
            {
                Console.WriteLine("Reopening the IndexReader");

                reader = reader.Reopen();
                searcher = new IndexSearcher(reader);
            }

            Search(searcher, analyzer, "anja");
        }

        private static Document CreateDocument(string content)
        {
            var document = new Document();
            document.Add(new Field(FieldName, content, Field.Store.YES, Field.Index.ANALYZED));
            return document;
        }

        private static void Search(IndexSearcher searcher, Analyzer analyzer, string searchString)
        {
            var parser = new QueryParser(LuceneVersion, FieldName, analyzer);

            Query q = parser.Parse(searchString);

            TopDocs hits = searcher.Search(q, null, 5, Sort.RELEVANCE);

            Console.WriteLine("Found {0} document(s) that matched query '{1}':", hits.TotalHits, q);
            foreach (ScoreDoc match in hits.ScoreDocs)
            {
                Document doc = searcher.Doc(match.Doc);
                Console.WriteLine("Matched {0}", doc.Get(FieldName));
            }
            searcher.Close();
        }
    }
}