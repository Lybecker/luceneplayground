using System;
using System.Collections.Generic;
using System.Text;
using Com.Lybecker.LuceneLibrary;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace AnalyzerSearcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var version = Lucene.Net.Util.Version.LUCENE_30;

            Directory dir = new RAMDirectory();
            //Analyzer analyzer = new PerFieldAnalyzerWrapper(
            //    new StandardAnalyzer(version),
            //    new Dictionary<string, Analyzer>() { { "text", new KeywordAnalyzer() } });


            Analyzer analyzer = new StandardAnalyzer(version);

            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);


            var document = new Document();
            //document.Add(new Field("text", "CD-ROM anders ABC65/66.txt.zip ", Field.Store.YES, Field.Index.ANALYZED));
            document.Add(new Field("text", "12345", Field.Store.YES, Field.Index.ANALYZED));
            document.Add(new Field("text2", "Anders", Field.Store.YES, Field.Index.ANALYZED));

            indexWriter.AddDocument(document);
            indexWriter.Commit();
            indexWriter.Dispose();

            var parser = new QueryParser(version, "text", analyzer);
            //var parser = new Lucene.Net.QueryParsers.MultiFieldQueryParser(version, new string[] { "text", "text2" }, analyzer);
            //var parser = new ExtendedMultiFieldQueryParser(version, new[] { "text", "text2" }, analyzer);
            //parser.SetAllowLeadingWildcard(true);
            //parser.ReverseFields = new[] { "text" };

            Query q = parser.Parse("12345");
            //Query q = parser.Parse("*21");

            //q = new PrefixQuery(new Term("text", "54"));
            var searcher = new IndexSearcher(dir, true);

            TopDocs hits = searcher.Search(q, null, 5, Sort.RELEVANCE);

            Console.WriteLine("Found {0} document(s) that matched query '{1}':", hits.TotalHits, q);
            foreach (ScoreDoc match in hits.ScoreDocs)
            {
                Document doc = searcher.Doc(match.Doc);
                Console.WriteLine("Matched {0}", doc.Get("text"));
            }
            searcher.Dispose();
        }
    }
}