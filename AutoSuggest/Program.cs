using System;
using System.Collections.Generic;
using Com.Lybecker.LuceneLibrary;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace Com.Lybecker.AutoSuggest
{

    class Program
    {
        private const string FieldName = "name";
        Lucene.Net.Util.Version _version = Lucene.Net.Util.Version.LUCENE_29;

        static void Main(string[] args)
        {
            Directory dir = new RAMDirectory();

            CreateLuceneIndex(dir);

            var autoSuggest = new AutoSuggest(new RAMDirectory());
            autoSuggest.IndexDictionary(dir, FieldName);

            while (true)
            {
                Console.Write("Enter text: ");
                string term = Console.ReadLine();

                Console.WriteLine("Suggestions for term: {0}", term);
                autoSuggest.SuggestTermsFor(term, 5).ForEach(Console.WriteLine);
            }
        }

        public static void CreateLuceneIndex(Directory dir)
        {
            var filepath = System.IO.Path.Combine(Environment.CurrentDirectory, "CopenhagenMarathon2010.txt");

            Analyzer analyzer = new KeywordAnalyzer(); // new SimpleAnalyzer();

            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            
            foreach (var name in System.IO.File.ReadLines(filepath))
            {
                var document = new Document();
                document.Add(new Field(FieldName, name, Field.Store.NO, Field.Index.ANALYZED));

                indexWriter.AddDocument(document);
            }

            indexWriter.Commit();
            indexWriter.Dispose();
        }
    }
}