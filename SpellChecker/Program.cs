using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using SpellChecker.Net.Search.Spell;

namespace SpellChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory luceneDir = new RAMDirectory();
            Directory spellDir = new RAMDirectory();


            CreateLuceneIndex(luceneDir);
            Net.Search.Spell.SpellChecker spell = GetSpellChecker(luceneDir, spellDir);


            var word = "dammark";


            string[] similarWords = spell.SuggestSimilar(word, 10);

            // show the similar words
            for (int wordIndex = 0; wordIndex < similarWords.Length; wordIndex++)
                Console.WriteLine("{0} is similar to {1}", similarWords[wordIndex], word);

        }

        public static void CreateLuceneIndex(Directory dir)
        {
            var countriesFilepath = System.IO.Path.Combine(Environment.CurrentDirectory, "Countries.txt");
            Lucene.Net.Util.Version version = Lucene.Net.Util.Version.LUCENE_29;

            Analyzer analyzer = new KeywordAnalyzer();

            // Add content to the index
            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            foreach (var country in System.IO.File.ReadLines(countriesFilepath))
            {
                var document = new Document();
                document.Add(new Field("name", country, Field.Store.NO, Field.Index.NOT_ANALYZED_NO_NORMS));

                indexWriter.AddDocument(document);
            }

            indexWriter.Commit();
            indexWriter.Close();
        }

        public static Net.Search.Spell.SpellChecker GetSpellChecker(Directory luceneDir, Directory spellDir)
        {
            var indexReader = IndexReader.Open(luceneDir, true);

            var spell = new Net.Search.Spell.SpellChecker(spellDir);

            spell.IndexDictionary(new LuceneDictionary(indexReader, "name"));

            return spell;
        }
    }
}
