namespace Com.Lybecker.LuceneLibrary
{
    public static class StemmerUtil
    {
        /**
         * Returns true if the character array starts with the suffix.
         * 
         * @param s Input Buffer
         * @param len length of input buffer
         * @param prefix Prefix string to test
         * @return true if <code>s</code> starts with <code>prefix</code>
         */
        public static bool StartsWith(char[] s, int len, string prefix)
        {
            int prefixLen = prefix.Length;
            if (prefixLen > len)
                return false;
            for (int i = 0; i < prefixLen; i++)
                if (s[i] != prefix[i])
                    return false;
            return true;
        }

        /**
         * Returns true if the character array ends with the suffix.
         * 
         * @param s Input Buffer
         * @param len length of input buffer
         * @param suffix Suffix string to test
         * @return true if <code>s</code> ends with <code>suffix</code>
         */
        public static bool EndsWith(char[] s, int len, string suffix)
        {
            int suffixLen = suffix.Length;
            if (suffixLen > len)
                return false;
            for (int i = suffixLen - 1; i >= 0; i--)
                if (s[len - (suffixLen - i)] != suffix[i])
                    return false;

            return true;
        }

        /**
         * Returns true if the character array ends with the suffix.
         * 
         * @param s Input Buffer
         * @param len length of input buffer
         * @param suffix Suffix string to test
         * @return true if <code>s</code> ends with <code>suffix</code>
         */
        public static bool EndsWith(char[] s, int len, char[] suffix)
        {
            int suffixLen = suffix.Length;
            if (suffixLen > len)
                return false;
            for (int i = suffixLen - 1; i >= 0; i--)
                if (s[len - (suffixLen - i)] != suffix[i])
                    return false;

            return true;
        }

        /**
         * Delete a character in-place
         * 
         * @param s Input Buffer
         * @param pos Position of character to delete
         * @param len length of input buffer
         * @return length of input buffer after deletion
         */
        public static int Delete(char[] s, int pos, int len)
        {
            //assert pos < len;
            if (pos < len - 1)
            { // don't arraycopy if asked to delete last character
                System.Array.Copy(s, pos + 1, s, pos, len - pos - 1);
            }
            return len - 1;
        }

        /**
         * Delete n characters in-place
         * 
         * @param s Input Buffer
         * @param pos Position of character to delete
         * @param len Length of input buffer
         * @param nChars number of characters to delete
         * @return length of input buffer after deletion
         */
        public static int DeleteN(char[] s, int pos, int len, int nChars)
        {
            //assert pos + nChars <= len;
            if (pos + nChars < len)
            { // don't arraycopy if asked to delete the last characters
                System.Array.Copy(s, pos + nChars, s, pos, len - pos - nChars);
            }
            return len - nChars;
        }
    }
}