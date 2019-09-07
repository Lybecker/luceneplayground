using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Com.Lybecker.LuceneLibrary.UnitTest
{
    /// <remarks>
    /// Java Source: http://svn.apache.org/repos/asf/lucene/dev/tags/lucene_solr_4_5_1/lucene/analysis/common/src/test/org/apache/lucene/analysis/miscellaneous/TestScandinavianFoldingFilter.java
    /// </remarks>
    [TestFixture]
    public class ScandinavianFoldingFilterTest : BaseTokenStreamTestCase
    {
        [Test]
        public void Test()
        {
            var analyzer = new ScandinavianFoldingFilterTestAnalyzer();

            CheckOneTerm(analyzer, "aeäaeeea", "aaaeea"); // should not cause ArrayOutOfBoundsException

            CheckOneTerm(analyzer, "aeäaeeeae", "aaaeea");
            CheckOneTerm(analyzer, "aeaeeeae", "aaeea");

            CheckOneTerm(analyzer, "bøen", "boen");
            CheckOneTerm(analyzer, "åene", "aene");


            CheckOneTerm(analyzer, "blåbærsyltetøj", "blabarsyltetoj");
            CheckOneTerm(analyzer, "blaabaarsyltetoej", "blabarsyltetoj");
            CheckOneTerm(analyzer, "blåbärsyltetöj", "blabarsyltetoj");

            CheckOneTerm(analyzer, "raksmorgas", "raksmorgas");
            CheckOneTerm(analyzer, "räksmörgås", "raksmorgas");
            CheckOneTerm(analyzer, "ræksmørgås", "raksmorgas");
            CheckOneTerm(analyzer, "raeksmoergaas", "raksmorgas");
            CheckOneTerm(analyzer, "ræksmörgaos", "raksmorgas");


            CheckOneTerm(analyzer, "ab", "ab");
            CheckOneTerm(analyzer, "ob", "ob");
            CheckOneTerm(analyzer, "Ab", "Ab");
            CheckOneTerm(analyzer, "Ob", "Ob");

            CheckOneTerm(analyzer, "å", "a");

            CheckOneTerm(analyzer, "aa", "a");
            CheckOneTerm(analyzer, "aA", "a");
            CheckOneTerm(analyzer, "ao", "a");
            CheckOneTerm(analyzer, "aO", "a");

            CheckOneTerm(analyzer, "AA", "A");
            CheckOneTerm(analyzer, "Aa", "A");
            CheckOneTerm(analyzer, "Ao", "A");
            CheckOneTerm(analyzer, "AO", "A");

            CheckOneTerm(analyzer, "æ", "a");
            CheckOneTerm(analyzer, "ä", "a");

            CheckOneTerm(analyzer, "Æ", "A");
            CheckOneTerm(analyzer, "Ä", "A");

            CheckOneTerm(analyzer, "ae", "a");
            CheckOneTerm(analyzer, "aE", "a");

            CheckOneTerm(analyzer, "Ae", "A");
            CheckOneTerm(analyzer, "AE", "A");


            CheckOneTerm(analyzer, "ö", "o");
            CheckOneTerm(analyzer, "ø", "o");
            CheckOneTerm(analyzer, "Ö", "O");
            CheckOneTerm(analyzer, "Ø", "O");


            CheckOneTerm(analyzer, "oo", "o");
            CheckOneTerm(analyzer, "oe", "o");
            CheckOneTerm(analyzer, "oO", "o");
            CheckOneTerm(analyzer, "oE", "o");

            CheckOneTerm(analyzer, "Oo", "O");
            CheckOneTerm(analyzer, "Oe", "O");
            CheckOneTerm(analyzer, "OO", "O");
            CheckOneTerm(analyzer, "OE", "O");
        }

        public class ScandinavianFoldingFilterTestAnalyzer : Analyzer
        {
            public override TokenStream TokenStream(string fieldName, TextReader reader)
            {
                TokenStream result = new WhitespaceTokenizer(reader);
                
                result = new ScandinavianFoldingFilter(result);

                return result;
            }
        }
    }
}
