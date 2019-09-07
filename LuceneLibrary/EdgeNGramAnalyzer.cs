using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Standard;

namespace Com.Lybecker.LuceneLibrary
{
    public class EdgeNGramAnalyzer : Analyzer
    {
        private readonly Lucene.Net.Util.Version _version;
        private readonly bool _enablePositionIncrements;
        private readonly ISet<string> _stopWords;

        public EdgeNGramAnalyzer(Lucene.Net.Util.Version version, bool enablePositionIncrements, ISet<string> stopWords)
        {
            _version = version;
            _enablePositionIncrements = enablePositionIncrements;

            if (stopWords == null)
                _stopWords = StopAnalyzer.ENGLISH_STOP_WORDS_SET;
            else
                _stopWords = stopWords;
        }

        public EdgeNGramAnalyzer(Lucene.Net.Util.Version version) : this(version, false, null) { }

        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            TokenStream result = new StandardTokenizer(_version, reader);

            result = new StandardFilter(result);
            result = new LowerCaseFilter(result);
            //result = new ASCIIFoldingFilter(result); // ø -> o, é -> e etc.
            result = new StopFilter(_enablePositionIncrements, result, _stopWords);
            result = new EdgeNGramTokenFilter(result, Side.FRONT, 1, 20);

            return result;
        }
    }
}