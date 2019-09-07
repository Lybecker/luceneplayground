using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

namespace Com.Lybecker.LuceneLibrary
{
    public class ReverseAnalyzer : Analyzer
    {
        private readonly Lucene.Net.Util.Version _version;

        public ReverseAnalyzer(Lucene.Net.Util.Version version)
        {
            _version = version;
        }

        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            TokenStream result = new StandardTokenizer(_version, reader);

            result = new StandardFilter(result);
            result = new LowerCaseFilter(result);
            result = new ReverseStringFilter(result);

            return result;
        }
    }
}