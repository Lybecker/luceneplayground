using System;
using System.Collections.Generic;
using System.Web;
using Com.Lybecker.AutoSuggest;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace AutoSuggestWeb.Models
{
    public class SuggestHelper
    {
        private const string FieldName = "name";
        private Lucene.Net.Util.Version _version = Lucene.Net.Util.Version.LUCENE_29;
        private AutoSuggest _autoSuggest;

        private static SuggestHelper _instance;
        private static readonly object _syncRoot = new object();

        private SuggestHelper()
        {
            Setup(HttpContext.Current.Server.MapPath("~/CopenhagenMarathon2010.txt"));
        }

        public static SuggestHelper GetInstance()
        {
            if (_instance == null)
            {
                lock (_syncRoot)
                {
                    if (_instance == null)
                        _instance = new SuggestHelper();
                }
            }

            return _instance;
        }

        public IList<Suggestion> Suggest(string term)
        {
            var suggestions = _autoSuggest.SuggestTermsFor(term, 5);

            return suggestions;
        }

        public void Setup(string filePath)
        {
            var sourceDir = new RAMDirectory();
            LoadLuceneIndex(sourceDir, filePath);

            _autoSuggest = CreateSuggest(new RAMDirectory(), sourceDir);

            sourceDir.Dispose();
        }

        private static void LoadLuceneIndex(Directory dir, string filePath)
        {
            Analyzer analyzer = new KeywordAnalyzer(); // new SimpleAnalyzer();

            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            foreach (var name in System.IO.File.ReadLines(filePath))
            {
                var document = new Document();
                document.Add(new Field(FieldName, name, Field.Store.NO, Field.Index.ANALYZED));

                indexWriter.AddDocument(document);
            }

            indexWriter.Commit();
            indexWriter.Dispose();
        }

        private static AutoSuggest CreateSuggest(Directory dir, Directory sourceDir)
        {
            var autoSuggest = new AutoSuggest(dir);
            autoSuggest.IndexDictionary(sourceDir, FieldName);

            return autoSuggest;
        }
    }
}