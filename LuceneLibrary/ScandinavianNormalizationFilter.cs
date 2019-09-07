using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.Lybecker.LuceneLibrary;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Com.Lybecker.LuceneLibrary
{

    /**
     * This filter normalize use of the interchangeable Scandinavian characters æÆäÄöÖøØ
     * and folded variants (aa, ao, ae, oe and oo) by transforming them to åÅæÆøØ.
     * <p/>
     * It's a semantically less destructive solution than {@link ScandinavianFoldingFilter},
     * most useful when a person with a Norwegian or Danish keyboard queries a Swedish index
     * and vice versa. This filter does <b>not</b>  the common Swedish folds of å and ä to a nor ö to o.
     * <p/>
     * blåbærsyltetøj == blåbärsyltetöj == blaabaarsyltetoej but not blabarsyltetoj
     * räksmörgås == ræksmørgås == ræksmörgaos == raeksmoergaas but not raksmorgas
     * <p/>
     * @see ScandinavianFoldingFilter
     */
    /// <remarks>
    /// Java Source: http://svn.apache.org/repos/asf/lucene/dev/tags/lucene_solr_4_5_1/lucene/analysis/common/src/java/org/apache/lucene/analysis/miscellaneous/ScandinavianNormalizationFilter.java
    /// </remarks>
    public class ScandinavianNormalizationFilter : TokenFilter
    {
        public ScandinavianNormalizationFilter(TokenStream input) : base(input) {}

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

                if (buffer[i] == ae_se)
                {
                    buffer[i] = ae;
                }
                else if (buffer[i] == AE_se)
                {
                    buffer[i] = AE;
                }
                else if (buffer[i] == oe_se)
                {
                    buffer[i] = oe;
                }
                else if (buffer[i] == OE_se)
                {
                    buffer[i] = OE;
                }
                else if (length - 1 > i)
                {
                    if (buffer[i] == 'a' && (buffer[i + 1] == 'a' || buffer[i + 1] == 'o' || buffer[i + 1] == 'A' || buffer[i + 1] == 'O'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = aa;
                    }
                    else if (buffer[i] == 'A' && (buffer[i + 1] == 'a' || buffer[i + 1] == 'A' || buffer[i + 1] == 'o' || buffer[i + 1] == 'O'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = AA;
                    }
                    else if (buffer[i] == 'a' && (buffer[i + 1] == 'e' || buffer[i + 1] == 'E'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = ae;
                    }
                    else if (buffer[i] == 'A' && (buffer[i + 1] == 'e' || buffer[i + 1] == 'E'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = AE;
                    }
                    else if (buffer[i] == 'o' && (buffer[i + 1] == 'e' || buffer[i + 1] == 'E' || buffer[i + 1] == 'o' || buffer[i + 1] == 'O'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = oe;
                    }
                    else if (buffer[i] == 'O' && (buffer[i + 1] == 'e' || buffer[i + 1] == 'E' || buffer[i + 1] == 'o' || buffer[i + 1] == 'O'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = OE;
                    }
                }
            }

            charTermAttribute.SetTermLength(length);

            return true;
        }
    }
}