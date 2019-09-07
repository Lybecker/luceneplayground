using System.IO;
using Lucene.Net.Analysis;

namespace Com.Lybecker.LuceneLibrary
{
    public class LowerCaseKeywordAnalyzer : KeywordAnalyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            var tokenizer = base.TokenStream(fieldName, reader);
            return new LowerCaseFilter(tokenizer);
        }
    }
}
