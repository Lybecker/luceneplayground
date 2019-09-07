using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace Com.Lybecker.AnalyzerTester
{
    public class AnalyzerTester : IDisposable
    {
        private readonly Lucene.Net.Util.Version _version;
        private readonly Directory _dir;
        public const string FieldName = "text";

        public AnalyzerTester(Lucene.Net.Util.Version version, Analyzer analyzer, IEnumerable<string> values)
        {
            _version = version;
            _dir = new RAMDirectory();
            var indexWriter = new IndexWriter(_dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            foreach (var value in values)
                indexWriter.AddDocument(CreateDocument(value));

            indexWriter.Commit();
            indexWriter.Dispose();
        }

        private static Document CreateDocument(string value)
        {
            var document = new Document();
            document.Add(new Field(FieldName, value, Field.Store.YES, Field.Index.ANALYZED));

            return document;
        }

        public IndexReader GetIndexReader()
        {
            return IndexReader.Open(_dir, true);
        }

        public IEnumerable<string> Search(QueryParser queryParser, string queryString)
        {
            Query query = queryParser.Parse(queryString);

            return Search(query);
        }

        public IEnumerable<string> Search(Analyzer analyzer, string queryString)
        {
            var queryParser = new QueryParser(_version, FieldName, analyzer);

            return Search(queryParser, queryString);
        }

        public IEnumerable<string> Search(Query query)
        {
            Console.WriteLine("Query: {0}", query);

            var searcher = new IndexSearcher(GetIndexReader());

            TopDocs hits = searcher.Search(query, null, 100, Sort.RELEVANCE);

            var results = new List<string>();

            foreach (ScoreDoc match in hits.ScoreDocs)
            {
                Document doc = searcher.Doc(match.Doc);
                results.Add(doc.Get(FieldName));
            }
            searcher.Dispose();

            return results;
        }

        public void Dispose()
        {
            _dir.Dispose();
        }
    }
}