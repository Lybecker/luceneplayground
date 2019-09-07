using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace NumericRangeQuery
{
    class Program
    {
        static Lucene.Net.Util.Version Version = Lucene.Net.Util.Version.LUCENE_29;
        const string TextFieldName = "text";
        const string DateFieldName = "date";
        private const string NumericDateFormat = "yyyyMMdd";

        static void Main(string[] args)
        {
            Directory dir = new RAMDirectory();
            Analyzer analyzer = new SimpleAnalyzer();

            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            foreach (var document in CreateDocuments())
            {
                indexWriter.AddDocument(document);
            }
            indexWriter.Commit();
            indexWriter.Dispose();



            var parser = new QueryParser(Version, TextFieldName, analyzer);
            Query q = parser.Parse("Anders");

            var fromFilter = DateTime2Int(new DateTime(2010, 3, 1));
            var toFilter = DateTime2Int(new DateTime(2010, 4, 1));

            Filter filter = NumericRangeFilter.NewIntRange(DateFieldName, fromFilter, toFilter, true, true);
            //Filter filter = NumericRangeFilter.NewIntRange(DateFieldName, fromFilter, null, true, true); // openended

            var searcher = new IndexSearcher(dir, true);

            TopDocs hits = searcher.Search(q, filter, 5, Sort.RELEVANCE);

            Console.WriteLine("Found {0} document(s) that matched query '{1}' with filter {2}:", hits.TotalHits, q, filter);
            foreach (ScoreDoc match in hits.ScoreDocs)
            {
                Document doc = searcher.Doc(match.Doc);

                Console.WriteLine("Matched = {0} with date {1}", doc.Get(TextFieldName), doc.Get(DateFieldName));

            }
            searcher.Dispose();
        }

        static int DateTime2Int(DateTime dateTime)
        {
            return int.Parse(dateTime.ToString(NumericDateFormat));
        }

        static IEnumerable<Document> CreateDocuments()
        {
            var docs = new List<Document>();

            docs.Add(CreateDocument("Anders Jan", new DateTime(2010, 1, 1)));
            docs.Add(CreateDocument("Anders Feb", new DateTime(2010, 2, 1)));
            docs.Add(CreateDocument("Anders Mar", new DateTime(2010, 3, 1)));
            docs.Add(CreateDocument("Anders Apr", new DateTime(2010, 4, 1)));

            return docs;
        }

        private static Document CreateDocument(string content, DateTime filterDate)
        {
            var document = new Document();
            document.Add(new Field(TextFieldName, content, Field.Store.YES, Field.Index.ANALYZED));

            var numfield = new NumericField(DateFieldName, Field.Store.YES, true);
            numfield.SetIntValue(int.Parse(filterDate.ToString(NumericDateFormat)));
            
            document.Add(numfield);
            return document;
        }
    }
}
