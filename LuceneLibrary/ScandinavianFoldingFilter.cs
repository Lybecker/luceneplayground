using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Com.Lybecker.LuceneLibrary
{
 /**
 * This filter folds Scandinavian characters åÅäæÄÆ->a and öÖøØ->o.
 * It also discriminate against use of double vowels aa, ae, ao, oe and oo, leaving just the first one.
 * <p/>
 * It's is a semantically more destructive solution than {@link ScandinavianNormalizationFilter} but
 * can in addition help with matching raksmorgas as räksmörgås.
 * <p/>
 * blåbærsyltetøj == blåbärsyltetöj == blaabaarsyltetoej == blabarsyltetoj
 * räksmörgås == ræksmørgås == ræksmörgaos == raeksmoergaas == raksmorgas
 * <p/>
 * Background:
 * Swedish åäö are in fact the same letters as Norwegian and Danish åæø and thus interchangeable
 * when used between these languages. They are however folded differently when people type
 * them on a keyboard lacking these characters.
 * <p/>
 * In that situation almost all Swedish people use a, a, o instead of å, ä, ö.
 * <p/>
 * Norwegians and Danes on the other hand usually type aa, ae and oe instead of å, æ and ø.
 * Some do however use a, a, o, oo, ao and sometimes permutations of everything above.
 * <p/>
 * This filter solves that mismatch problem, but might also cause new.
 * <p/>
 * @see ScandinavianNormalizationFilter
 */
    /// <remarks>
    /// Java Source: http://svn.apache.org/repos/asf/lucene/dev/tags/lucene_solr_4_5_1/lucene/analysis/common/src/java/org/apache/lucene/analysis/miscellaneous/ScandinavianFoldingFilter.java
    /// </remarks>
    public class ScandinavianFoldingFilter : TokenFilter
    {
        public ScandinavianFoldingFilter(TokenStream input) : base(input) {}

        private const char AA = '\u00C5'; // Å
        private const char aa = '\u00E5'; // å
        private const char AE = '\u00C6'; // Æ
        private const char ae = '\u00E6'; // æ
        private const char AE_se = '\u00C4'; // Ä
        private const char ae_se = '\u00E4'; // ä
        private const char OE = '\u00D8'; // Ø
        private const char oe = '\u00F8'; // ø
        private const char OE_se = '\u00D6'; // Ö
        private const char oe_se = '\u00F6'; //ö

        public override bool IncrementToken()
        {
            if (!input.IncrementToken())
            {
                return false;
            }

            var charTermAttribute = AddAttribute<ITermAttribute>();

            char[] buffer = charTermAttribute.TermBuffer();
            int length = charTermAttribute.TermLength();


            int i;
            for (i = 0; i < length; i++)
            {

                if (buffer[i] == aa
                    || buffer[i] == ae_se
                    || buffer[i] == ae)
                {

                    buffer[i] = 'a';

                }
                else if (buffer[i] == AA
                         || buffer[i] == AE_se
                         || buffer[i] == AE)
                {

                    buffer[i] = 'A';

                }
                else if (buffer[i] == oe
                         || buffer[i] == oe_se)
                {

                    buffer[i] = 'o';

                }
                else if (buffer[i] == OE
                         || buffer[i] == OE_se)
                {

                    buffer[i] = 'O';

                }
                else if (length - 1 > i)
                {

                    if ((buffer[i] == 'a' || buffer[i] == 'A')
                        && (buffer[i + 1] == 'a'
                            || buffer[i + 1] == 'A'
                            || buffer[i + 1] == 'e'
                            || buffer[i + 1] == 'E'
                            || buffer[i + 1] == 'o'
                            || buffer[i + 1] == 'O')
                        )
                    {

                        length = StemmerUtil.Delete(buffer, i + 1, length);

                    }
                    else if ((buffer[i] == 'o' || buffer[i] == 'O')
                             && (buffer[i + 1] == 'e'
                                 || buffer[i + 1] == 'E'
                                 || buffer[i + 1] == 'o'
                                 || buffer[i + 1] == 'O')
                        )
                    {

                        length = StemmerUtil.Delete(buffer, i + 1, length);

                    }
                }
            }
            charTermAttribute.SetTermLength(length);

            return true;
        }
    }
}