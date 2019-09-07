using System;
using System.Collections.Generic;
using System.Linq;
using Com.Lybecker.LuceneLibrary;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace Com.Lybecker.AutoSuggest
{
    public sealed class AutoSuggest {

        private const string NGramTermField = "ngramterm";
        private const string TermFreqField = "termFreq";
        private const int SortTypeInteger = 4;

        private readonly Directory _directory;
        private IndexReader _indexReader;
        private Searcher _indexSearcher;

        public AutoSuggest(Directory directory)
        {
            _directory = directory;
        }

        public List<Suggestion> SuggestTermsFor(string term, int maxSuggestions)  {
            Query query = new TermQuery(new Term(NGramTermField, term));
            var sort = new Sort(new SortField(TermFreqField, SortTypeInteger, true));

            Searcher searcher = GetCurrentIndexSearcher();

            TopDocs docs = searcher.Search(query, null, maxSuggestions, sort);
            
            var suggestions = new List<Suggestion>();
            
            foreach (var scoreDoc in docs.ScoreDocs)
            {
                var doc = searcher.Doc(scoreDoc.Doc);
                
                suggestions.Add(
                    new Suggestion(){
                        Term = doc.Get(NGramTermField), 
                        Occurrence = int.Parse(doc.Get(TermFreqField))
                    });
            }

            return suggestions;
        }

        public void IndexDictionary(IndexReader sourceReader, string fieldToAutocomplete)
        {
            Dictionary<string, int> termsMap = GetTermsMap(sourceReader, fieldToAutocomplete);

            BuildAutoSuggestIndex(_directory, termsMap);
        }

        /// <summary>
        /// Create edge n-grams for each term. Store the terms (n-grams) and the term frequency in the directory.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="termsMap"></param>
        private static void BuildAutoSuggestIndex(Directory directory, Dictionary<string, int> termsMap)
        {
            var analyzer = new EdgeNGramAnalyzer(Lucene.Net.Util.Version.LUCENE_29);

            var writer = new IndexWriter(directory, analyzer, true, IndexWriter.MaxFieldLength.LIMITED);

            writer.SetRAMBufferSizeMB(64d);
            writer.MergeFactor = 300;

            foreach (var term in termsMap.Keys)
            {
                var doc = new Document();
                doc.Add(new Field(NGramTermField, term, Field.Store.YES, Field.Index.ANALYZED_NO_NORMS));
                doc.Add(new Field(TermFreqField, termsMap[term].ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));

                writer.AddDocument(doc);
            }

            writer.Optimize();
            writer.Commit();
            writer.Dispose();
        }

        public void IndexDictionary(Directory sourceDirectory, string fieldToAutocomplete)
        {
            IndexReader sourceReader = IndexReader.Open(sourceDirectory, true);

            IndexDictionary(sourceReader, fieldToAutocomplete);

            sourceReader.Dispose();
        }

        /// <summary>
        /// Iterate each term in fieldToAutocomplete from the sourceReader.
        /// </summary>
        /// <param name="indexReader"></param>
        /// <param name="fieldToAutocomplete"></param>
        /// <returns>Term and term frequency</returns>
        private static Dictionary<string, int> GetTermsMap(IndexReader indexReader, string fieldToAutocomplete)
        {
            var util = new Util();

            return util.GetTerms(indexReader, fieldToAutocomplete, minTermLength: 0)
                .ToDictionary(key => key.Term, value => value.DocFreq);
        }

        private Searcher GetCurrentIndexSearcher()
        {
            if (_indexReader == null || _indexReader.IsCurrent() == false)
            {
                _indexReader = IndexReader.Open(_directory, true);
                _indexSearcher = new IndexSearcher(_indexReader);
            }

            return _indexSearcher;
        }
    }

    public class Suggestion
    {
        public string Term { get; set; }
        public int Occurrence { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Term, Occurrence);
        }
    }
}