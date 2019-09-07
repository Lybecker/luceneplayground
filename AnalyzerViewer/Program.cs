using System;
using System.Collections.Generic;
using System.IO;
using Com.Lybecker.LuceneLibrary;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Version = Lucene.Net.Util.Version;

namespace Com.Lybecker.AnalyzerViewer
{
    class Program
    {
        static void Main(string[] args)
        {
            Action<Analyzer, String> displayAction = DisplayTokens;

            var version = Lucene.Net.Util.Version.LUCENE_30;

            var text = "Høje Taastrup Århus René";

            Console.WriteLine("Original string: {0}", text);
            Console.WriteLine();

            Analyzer analyzer = new KeywordAnalyzer();
            displayAction(analyzer, text);
            analyzer = new WhitespaceAnalyzer();
            displayAction(analyzer, text);
            analyzer = new SimpleAnalyzer();
            displayAction(analyzer, text);
            analyzer = new StopAnalyzer(version);
            displayAction(analyzer, text);
            analyzer = new StandardAnalyzer(version);
            displayAction(analyzer, text);
            analyzer = new SnowballAnalyzer(Version.LUCENE_30, "Danish"); // http://snowball.tartarus.org/
            displayAction(analyzer, text);
            analyzer = new TestAnalyzer(version);
            displayAction(analyzer, text);

            //analyzer = new LowerCaseKeywordAnalyzer();
            //displayAction(analyzer, text);
            //analyzer = new EdgeNGramAnalyzer(version);
            //displayAction(analyzer, text);
            //analyzer = new ReverseAnalyzer(version);
            //displayAction(analyzer, text);

            //new PerFieldAnalyzerWrapper() //Different fields require different analyzers
        }

        public static void DisplayTokens(Analyzer analyzer, String text)
        {
            Console.WriteLine("Analyzing with {0}", analyzer.GetType().FullName);
            DisplayTokens(analyzer.TokenStream("contents", new StringReader(text)));
            Console.WriteLine();
            Console.WriteLine();
        }

        public static void DisplayTokens(TokenStream tokenStream)
        {
            var term = tokenStream.AddAttribute<ITermAttribute>();
            while (tokenStream.IncrementToken())
            {
                Console.Write("[" + term.Term + "] ");
            }
        }

        public static void DisplayTokensWithFullDetails(Analyzer analyzer, String text)
        {
            Console.WriteLine("Analyzing with {0}", analyzer.GetType().FullName);

            TokenStream stream = analyzer.TokenStream("contents", new StringReader(text));

            var term = stream.AddAttribute<ITermAttribute>();
            var posIncr = stream.AddAttribute<IPositionIncrementAttribute>();
            var offset = stream.AddAttribute<IOffsetAttribute>();
            var type = stream.AddAttribute<ITypeAttribute>();

            int position = 0;
            while (stream.IncrementToken())
            {
                int increment = posIncr.PositionIncrement;
                if (increment > 0)
                {
                    position = position + increment;
                    Console.Write(position + ": ");
                }

                Console.WriteLine("[{0}:{1}->{2}:{3}] ", term.Term, offset.StartOffset, offset.EndOffset, type.Type);
            }
            Console.WriteLine();
        }
    }

    public class TestAnalyzer : Analyzer
    {
        private readonly Lucene.Net.Util.Version _version;
        private readonly bool _enablePositionIncrements;
        private readonly ISet<string> _stopWords;

        public TestAnalyzer(Lucene.Net.Util.Version version, bool enablePositionIncrements, ISet<string> stopWords)
        {
            _version = version;
            _enablePositionIncrements = enablePositionIncrements;

            if (stopWords == null)
                _stopWords = StopAnalyzer.ENGLISH_STOP_WORDS_SET;
            else
                _stopWords = stopWords;
        }

        public TestAnalyzer(Lucene.Net.Util.Version version) : this(version, false, null) { }

        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            TokenStream result = new StandardTokenizer(_version, reader);

            result = new StandardFilter(result);
            result = new LowerCaseFilter(result);
            //result = new ScandinavianNormalizationFilter(result);
            //result = new ScandinavianFoldingFilter(result);
            //result = new ASCIIFoldingFilter(result); // ø -> o, é -> e etc.
            result = new StopFilter(_enablePositionIncrements, result, _stopWords);

            return result;
        }
    }
}