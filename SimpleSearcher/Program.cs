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

namespace SimpleSearcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Lucene.Net.Util.Version version = Lucene.Net.Util.Version.LUCENE_30;

            Directory dir = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(version);

            // Add content to the index
            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            //indexWriter.SetInfoStream(new System.IO.StreamWriter(Console.OpenStandardOutput()));
            
            foreach (var document in CreateDocuments())
            {
                indexWriter.AddDocument(document);
            }

            indexWriter.Commit();
            indexWriter.Dispose();


            // Search for the content
            var parser = new MultiFieldQueryParser(version, new[] { "biography" }, analyzer);
            Query q = parser.Parse("Microsoft"); //-bill +id:0


            var searcher = new IndexSearcher(dir, true);

            TopDocs hits = searcher.Search(q, null, 5, Sort.RELEVANCE);

            Console.WriteLine("Found {0} document(s) that matched query '{1}':", hits.TotalHits, q);
            foreach (ScoreDoc match in hits.ScoreDocs)
            {
                Document doc = searcher.Doc(match.Doc);

                Console.WriteLine("Matched id = {0}, Name = {1}", doc.Get("id"), doc.Get("name"));

                //Console.WriteLine(Explain(searcher, q, match));
            }
            searcher.Close();
        }

        public static Document[] CreateDocuments()
        {
            var docs = new Document[3];

            docs[0] = new Document();
            docs[0].Add(new Field("id", "0", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            docs[0].Add(new Field("name", "Anders Lybecker", Field.Store.YES, Field.Index.ANALYZED));
            docs[0].Add(new Field("company", "Kring Development", Field.Store.NO, Field.Index.NOT_ANALYZED));
            docs[0].Add(new Field("skills", ".Net, SQL, Lucene, programming", Field.Store.NO, Field.Index.ANALYZED));
            docs[0].Add(new Field("biography", "Anders Lybecker is a solution architect at Kring Development A/S; his primary expertise is the Microsoft .Net framework and SQL Server which he has been working with since the start of this century - writing C# code with the .Net framework since early beta 1 in 2001. He holds a degree in software engineering specializing in software development.", Field.Store.NO, Field.Index.ANALYZED));
            //docs[0].SetBoost(2);

            docs[1] = new Document();
            docs[1].Add(new Field("id", "1", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            docs[1].Add(new Field("name", "Doug Cutting", Field.Store.YES, Field.Index.ANALYZED));
            docs[1].Add(new Field("company", "Cloudera", Field.Store.NO, Field.Index.NOT_ANALYZED));
            docs[1].Add(new Field("skills", "Lucene, Hadoop, Java", Field.Store.NO, Field.Index.ANALYZED));
            docs[1].Add(new Field("biography", "Douglas Reed Cutting is an advocate and creator of open-source search technology. He has never worked at Microsoft.", Field.Store.NO, Field.Index.ANALYZED));


            docs[2] = new Document();
            docs[2].Add(new Field("id", "2", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            docs[2].Add(new Field("name", "Bill Gates", Field.Store.YES, Field.Index.ANALYZED));
            docs[2].Add(new Field("company", "Microsoft", Field.Store.NO, Field.Index.NOT_ANALYZED));
            docs[2].Add(new Field("skills", "Money, Philanthropy, Licenses", Field.Store.NO, Field.Index.ANALYZED));
            docs[2].Add(new Field("biography", "William Henry 'Bill' Gates III (born October 28, 1955) is an American business magnate, philanthropist, and chairman of Microsoft, the software company he founded with Paul Allen. He is consistently ranked among the world's wealthiest people and was the wealthiest overall from 1995 to 2009, excluding 2008, when he was ranked third, and 2010, when he was ranked second behind Mexico's Carlos Slim Helu. During his career at Microsoft, Gates held the positions of CEO and chief software architect, and remains the largest individual shareholder with more than 8 percent of the common stock. He has also authored or co-authored several books.", Field.Store.NO, Field.Index.ANALYZED));

            return docs;
        }
        public static string Explain(IndexSearcher searcher, Query query, ScoreDoc match)
        {
            Explanation explanation = searcher.Explain(query, match.Doc);
            return explanation.ToString();
        }
    }
}
