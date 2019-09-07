using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace MoreLikeThis
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory dir = new RAMDirectory();

            CreateDocuments(dir);

            IndexReader reader = IndexReader.Open(dir, true);

            var searcher = new IndexSearcher(reader);

            int numDocs = reader.MaxDoc;

            var mlt = new Lucene.Net.Search.Similar.MoreLikeThis(reader);
            mlt.SetFieldNames(new String[] { "name" });
            mlt.MinTermFreq = 1;
            mlt.MinDocFreq = 1;

            for (int docId = 0; docId < numDocs; docId++)
            {
                Document doc = reader.Document(docId);
                Console.WriteLine(doc.Get("name"));

                Query query = mlt.Like(docId);
                Console.WriteLine("  query = {0}", query);

                TopDocs similarDocs = searcher.Search(query, 10);
                if (similarDocs.TotalHits == 0)
                    Console.WriteLine("  None like this");
                for (int i = 0; i < similarDocs.ScoreDocs.Length; i++)
                {
                    if (similarDocs.ScoreDocs[i].Doc != docId)
                    { 
                        doc = reader.Document(similarDocs.ScoreDocs[i].Doc);
                        Console.WriteLine("  -> {0}", doc.Get("name"));
                    }
                }
                Console.WriteLine();
            }

            searcher.Dispose();
            reader.Dispose();
            dir.Dispose();
        }

        static void CreateDocuments(Directory dir)
        {
            var writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);

            //Add series of docs with misspelt names
            AddDoc(writer, "jonathon smythe", "1");
            AddDoc(writer, "jonathan smith", "2");
            AddDoc(writer, "johnathon smyth", "3");
            AddDoc(writer, "johnny smith", "4");
            AddDoc(writer, "jonny smith", "5");
            AddDoc(writer, "johnathon smythe", "6");

            writer.Commit();
            writer.Dispose();
        }

        static void AddDoc(IndexWriter writer, String name, String id)
        {
            var doc = new Document();
            doc.Add(new Field("id", id, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("name", name, Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);
        }
    }
}
