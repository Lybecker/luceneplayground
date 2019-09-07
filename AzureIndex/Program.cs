using System;
using System.Configuration;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;

namespace AzureIndex
{
    class Program
    {
        static void Main(string[] args)
        {
            var version = Lucene.Net.Util.Version.LUCENE_30;

            // default AzureDirectory stores cache in local temp folder
            CloudStorageAccount cloudAccount = CloudStorageAccount.DevelopmentStorageAccount;
            CloudStorageAccount.TryParse(CloudConfigurationManager.GetSetting("LuceneBlobStorage"), out cloudAccount);
            //AzureDirectory azureDirectory = new AzureDirectory(cloudStorageAccount, "TestTest", new RAMDirectory());
            //AzureDirectory azureDirectory = new AzureDirectory(cloudStorageAccount, "TestTest", FSDirectory.Open(@"c:\test"));

            var cacheDirectory = new RAMDirectory();

            var indexName = "MyLuceneIndex";
            var azureDirectory = new AzureDirectory(cloudAccount, indexName, cacheDirectory);

            var analyzer = new StandardAnalyzer(version);

            // Add content to the index
            var indexWriter = new IndexWriter(azureDirectory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            indexWriter.UseCompoundFile = false;

            foreach (var document in CreateDocuments())
            {
                indexWriter.AddDocument(document);
            }

            indexWriter.Commit();
            indexWriter.Close();

            // Search for the content
            var parser = new QueryParser(version, "text", analyzer);
            Query q = parser.Parse("azure");

            var searcher = new IndexSearcher(azureDirectory, true);

            TopDocs hits = searcher.Search(q, null, 5, Sort.RELEVANCE);

            Console.WriteLine("Found {0} document(s) that matched query '{1}':", hits.TotalHits, q);
            foreach (ScoreDoc match in hits.ScoreDocs)
            {
                Document doc = searcher.Doc(match.Doc);

                Console.WriteLine("Matched id = {0}, Text = {1}", doc.Get("id"), doc.Get("text"));

            }
            searcher.Dispose();

            Console.ReadLine();
        }

        public static Document[] CreateDocuments()
        {
            var docs = new Document[3];

            docs[0] = new Document();
            docs[0].Add(new Field("id", "0", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            docs[0].Add(new Field("text", "Azure in the wild", Field.Store.YES, Field.Index.ANALYZED));

            docs[1] = new Document();
            docs[1].Add(new Field("id", "1", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            docs[1].Add(new Field("text", "Lucene rules", Field.Store.YES, Field.Index.ANALYZED));


            docs[2] = new Document();
            docs[2].Add(new Field("id", "2", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            docs[2].Add(new Field("text", "Lucene and Azure - a perfect match.", Field.Store.YES, Field.Index.ANALYZED));

            return docs;
        }
    }
}
