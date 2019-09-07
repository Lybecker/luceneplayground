using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace NearRealTimeSearch
{
    class Program
    {
        //http://files.meetup.com/1460078/J-Rutherglen%20LuceneNearRealtimeSearch2.ppt
        static void Main(string[] args)
        {
            Lucene.Net.Util.Version version = Lucene.Net.Util.Version.LUCENE_29;

            Directory dir = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(version);

            var writer = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.LIMITED);
            
            for (int i = 0; i < 10; i++)
            {
                var doc2 = new Document();
                doc2.Add(new Field("id", "" + i, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                doc2.Add(new Field("text", "aaa", Field.Store.NO, Field.Index.ANALYZED));
                writer.AddDocument(doc2);
            }

            IndexReader reader = writer.GetReader();
            Searcher searcher = new IndexSearcher(reader);

            Query query = new TermQuery(new Term("text", "aaa"));
            TopDocs docs = searcher.Search(query, 1);
            //assertEquals(10, docs.totalHits);

            writer.DeleteDocuments(new Term("id", "7"));

            var doc = new Document();
            doc.Add(new Field("id", "11", Field.Store.YES,Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("text","bbb",Field.Store.NO,Field.Index.ANALYZED));
            writer.AddDocument(doc);

            IndexReader newReader = reader.Reopen();
            //assertFalse(reader == newReader);
            reader.Close();
            searcher = new IndexSearcher(newReader);

            TopDocs hits = searcher.Search(query, 10);
            //assertEquals(9, hits.totalHits);

            query = new TermQuery(new Term("text", "bbb"));
            hits = searcher.Search(query, 1);
            //assertEquals(1, hits.totalHits);

            newReader.Close();
            writer.Rollback();
            writer.Close();
        }
    }
}

// IndexWriter.IndexReaderWarmer AND IndexWriter.SetMergedSegmentWarmer

/*
 This capability is referred to as near real-time search, and not simply real-time search, because it’s 
not possible to make strict guarantees about the turnaround time, in the same sense as a hard real-time 
operating system is able to do.  Lucene’s near real-time search is more like a soft real-time operating 
system.  For example, if Java decides to run a major garbage collection cycle, or if a large segment merge 
has just completed, or if your machine is struggling because there’s not enough RAM, the turnaround time 
of the near real-time reader can be much longer. However, in practice the turnaround time can be very 
fast (10s of milliseconds or less), depending on your indexing and searching throughput, and how 
frequently you obtain a new near real-time reader. 
 In the past, without this feature, you would have to call commit on the writer, and then reopen on 
your reader, but this can be time consuming since  commit must sync all new files in the index, an 
operation that is often very costly on certain operating systems and filesystems as it usually means the 
underlying IO device must physically write all buffered bytes to stable storage.  Near real-time search 
enables you to search segments that are newly created but not yet committed.  Section 11.1.3 gives some 
tips for further reducing the index-to-search turnaround time. 


 The important method is IndexWriter.getReader.  This method flushes any buffered changes to 
the Directory, and then creates a new IndexReader that includes the changes.  If further changes are 
made through the IndexWriter, you use the reopen method in the IndexReader to get a new reader.  
If there are changes, a new reader is returned, and you should then close the old reader.  The reopen 
method is very efficient: for any unchanged parts of the index, it shares the open files and caches with the 
previous reader.  Only newly created files since the last open or reopen will be opened.  This results in 
very fast, often sub-second, turnaround.  Section 11.2.2 provides further examples of how to use the 
reopen method with a near real-time reader. 
 Next we look at how Lucene scores each document that matches the search. 
*/