using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Com.Lybecker.LuceneLibrary.UnitTest
{
    [TestFixture]
    public class ScandinavianNormalizationFilterTest : BaseTokenStreamTestCase
    {
        [Test]
        public void Test()
        {
            var analyzer = new ScandinavianNormalizationFilterTestAnalyzer();

            CheckOneTerm(analyzer, "aeäaeeea", "æææeea"); // should not cause ArrayIndexOutOfBoundsException

            CheckOneTerm(analyzer, "aeäaeeeae", "æææeeæ");
            CheckOneTerm(analyzer, "aeaeeeae", "ææeeæ");

            CheckOneTerm(analyzer, "bøen", "bøen");
            CheckOneTerm(analyzer, "bOEen", "bØen");
            CheckOneTerm(analyzer, "åene", "åene");


            CheckOneTerm(analyzer, "blåbærsyltetøj", "blåbærsyltetøj");
            CheckOneTerm(analyzer, "blaabaersyltetöj", "blåbærsyltetøj");
            CheckOneTerm(analyzer, "räksmörgås", "ræksmørgås");
            CheckOneTerm(analyzer, "raeksmörgaos", "ræksmørgås");
            CheckOneTerm(analyzer, "raeksmörgaas", "ræksmørgås");
            CheckOneTerm(analyzer, "raeksmoergås", "ræksmørgås");


            CheckOneTerm(analyzer, "ab", "ab");
            CheckOneTerm(analyzer, "ob", "ob");
            CheckOneTerm(analyzer, "Ab", "Ab");
            CheckOneTerm(analyzer, "Ob", "Ob");

            CheckOneTerm(analyzer, "å", "å");

            CheckOneTerm(analyzer, "aa", "å");
            CheckOneTerm(analyzer, "aA", "å");
            CheckOneTerm(analyzer, "ao", "å");
            CheckOneTerm(analyzer, "aO", "å");

            CheckOneTerm(analyzer, "AA", "Å");
            CheckOneTerm(analyzer, "Aa", "Å");
            CheckOneTerm(analyzer, "Ao", "Å");
            CheckOneTerm(analyzer, "AO", "Å");

            CheckOneTerm(analyzer, "æ", "æ");
            CheckOneTerm(analyzer, "ä", "æ");

            CheckOneTerm(analyzer, "Æ", "Æ");
            CheckOneTerm(analyzer, "Ä", "Æ");

            CheckOneTerm(analyzer, "ae", "æ");
            CheckOneTerm(analyzer, "aE", "æ");

            CheckOneTerm(analyzer, "Ae", "Æ");
            CheckOneTerm(analyzer, "AE", "Æ");


            CheckOneTerm(analyzer, "ö", "ø");
            CheckOneTerm(analyzer, "ø", "ø");
            CheckOneTerm(analyzer, "Ö", "Ø");
            CheckOneTerm(analyzer, "Ø", "Ø");


            CheckOneTerm(analyzer, "oo", "ø");
            CheckOneTerm(analyzer, "oe", "ø");
            CheckOneTerm(analyzer, "oO", "ø");
            CheckOneTerm(analyzer, "oE", "ø");

            CheckOneTerm(analyzer, "Oo", "Ø");
            CheckOneTerm(analyzer, "Oe", "Ø");
            CheckOneTerm(analyzer, "OO", "Ø");
            CheckOneTerm(analyzer, "OE", "Ø");
        }

        public class ScandinavianNormalizationFilterTestAnalyzer : Analyzer
        {
            public override TokenStream TokenStream(string fieldName, TextReader reader)
            {
                TokenStream result = new WhitespaceTokenizer(reader);

                result = new ScandinavianNormalizationFilter(result);

                return result;
            }
        }
    }
}