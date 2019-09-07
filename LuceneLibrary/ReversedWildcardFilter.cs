/**
 * Reverse token string, for example "country" => "yrtnuoc".
 * <p>
 * If <code>marker</code> is supplied, then tokens will be also prepended by
 * that character. For example, with a marker of &#x5C;u0001, "country" =>
 * "&#x5C;u0001yrtnuoc". This is useful when implementing efficient leading
 * wildcards search.
 * </p>
 */
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Com.Lybecker.LuceneLibrary
{
    public class ReverseStringFilter : TokenFilter
    {

        private readonly ITermAttribute _termAtt;
        private readonly char _marker;
        private const char NOMARKER = '\uFFFF';

        /// <summary>
        /// Example marker character: U+0001 (START OF HEADING)
        /// </summary>
        public const char START_OF_HEADING_MARKER = '\u0001';

        /// <summary>
        /// Example marker character: U+001F (INFORMATION SEPARATOR ONE)
        /// </summary>
        public const char INFORMATION_SEPARATOR_MARKER = '\u001F';

        /// <summary>
        /// Example marker character: U+EC00 (PRIVATE USE AREA: EC00) 
        /// </summary>
        public const char PUA_EC00_MARKER = '\uEC00';

        /// <summary>
        /// Example marker character: U+200F (RIGHT-TO-LEFT MARK)
        /// </summary>
        public const char RTL_DIRECTION_MARKER = '\u200F';

        /// <summary>
        /// Create a new ReverseStringFilter that reverses all tokens in the supplied {@link TokenStream}.
        /// The reversed tokens will not be marked. 
        /// </summary>
        /// <param name="input">{@link TokenStream} to filter</param>
        public ReverseStringFilter(TokenStream input) : this(input, NOMARKER) { }

        /// <summary>
        /// Create a new ReverseStringFilter that reverses and marks all tokens in the supplied {@link TokenStream}.
        /// The reversed tokens will be prepended (marked) by the <code>marker</code> character.
        /// </summary>
        /// <param name="input">{@link TokenStream} to filter</param>
        /// <param name="marker">marker A character used to mark reversed tokens</param>
        public ReverseStringFilter(TokenStream input, char marker)
            : base(input)
        {
            this._marker = marker;
            _termAtt = AddAttribute<ITermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                int len = _termAtt.TermLength();
                if (_marker != NOMARKER)
                {
                    len++;
                    _termAtt.ResizeTermBuffer(len);
                    _termAtt.TermBuffer()[len - 1] = _marker;
                }
                Reverse(_termAtt.TermBuffer(), len);
                _termAtt.SetTermLength(len);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string Reverse(string input)
        {
            char[] charInput = input.ToCharArray();
            Reverse(charInput);
            return new string(charInput);
        }

        public static void Reverse(char[] buffer)
        {
            Reverse(buffer, buffer.Length);
        }

        public static void Reverse(char[] buffer, int len)
        {
            Reverse(buffer, 0, len);
        }

        public static void Reverse(char[] buffer, int start, int len)
        {
            if (len <= 1) return;
            int num = len >> 1;
            for (int i = start; i < (start + num); i++)
            {
                char c = buffer[i];
                buffer[i] = buffer[start * 2 + len - i - 1];
                buffer[start * 2 + len - i - 1] = c;
            }
        }
    }
}