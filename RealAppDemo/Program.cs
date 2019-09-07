using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Miracle.HK.ReportArchive.Search.Contract;
using Directory = Lucene.Net.Store.Directory;

namespace RealAppDemo
{
    class Program
    {
        static Guid _fileId = Guid.Parse("4be1c0b8-b675-496e-81d4-fed5dfc7abec");

        static void DisplayResults(SearchHits<Page> result)
        {
            Console.WriteLine("Displaying page {0} of {1} sorted by relevans:", result.Page, result.NumberOfPages);

            Console.WriteLine(String.Join(", ", result.Items.Select(x => x.PageNumber)));

            Console.WriteLine();
            Console.WriteLine("Returned {0} out of {1} results in {2} ms.", result.Pagesize, result.TotalHits, result.QueryExecutionTime.TotalMilliseconds);
        }

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("Enter a querystring:");
                var queryString = Console.ReadLine();

                var proxy = new KDListManagerClient();
                try
                {
                    proxy.Open();
                    SearchHits<Page> result = proxy.Search(new PageQueryParameter(queryString, _fileId));

                    DisplayResults(result);
                }
                catch (Exception)
                {
                    Console.WriteLine("Cannot process...");
                }
                finally
                {
                    if (proxy.State ==  CommunicationState.Faulted)
                        proxy.Abort();
                    else
                        proxy.Close();
                }
                Console.WriteLine();
            }










            //return;

            //Lucene.Net.Util.Version version = Lucene.Net.Util.Version.LUCENE_29;

            //Directory dir = FSDirectory.Open(new DirectoryInfo(@"C:\HK Data\FullIndex\FileIndex"));
            //Analyzer analyzer = new StandardAnalyzer(version);

            //var parser = new MultiFieldQueryParser(version, new[] { "NumberOfPages" }, analyzer);
            //Query q = parser.Parse("[100000 TO 9999999999999999]");

            //var searcher = new IndexSearcher(dir, true);

            //TopDocs hits = searcher.Search(q, null, 10, new Sort(new SortField("NumberOfPages", true)));

            //Console.WriteLine("Found {0} document(s) that matched query '{1}':", hits.TotalHits, q);
            //foreach (ScoreDoc match in hits.ScoreDocs)
            //{
            //    Document doc = searcher.Doc(match.Doc);
            //    Console.WriteLine("Matched id = {0}, NumberOfPages = {1}", doc.Get("Id"), doc.Get("NumberOfPages"));

            //    //Console.WriteLine(Explain(searcher, q, match));
            //}

            //searcher.Close();

            //dir.Close();
        }
    }
}
