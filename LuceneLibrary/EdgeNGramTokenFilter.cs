/**
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 * Tokenizes the given token into n-grams of given size(s).
 * <p>
 * This {@link TokenFilter} create n-grams from the beginning edge or ending edge of a input token.
 * </p>
 */
using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Com.Lybecker.LuceneLibrary
{
    public sealed class EdgeNGramTokenFilter : TokenFilter
    {
        public static readonly Side DefaultSide = Side.Front;
        public static readonly int DefaultMaxGramSize = 1;
        public static readonly int DefaultMinGramSize = 1;

        /** Specifies which side of the input the n-gram should be generated from */
        public enum Side
        {
            /** Get the n-gram from the front of the input */
            Front,

            /** Get the n-gram from the end of the input */
            Back
        }

        private readonly int _minGram;
        private readonly int _maxGram;
        private readonly Side _side;
        private char[] _curTermBuffer;
        private int _curTermLength;
        private int _curGramSize;
        private int _tokStart;

        private readonly ITermAttribute _termAtt;
        private readonly IOffsetAttribute _offsetAtt;

        /**
     * Creates EdgeNGramTokenFilter that can generate n-grams in the sizes of the given range
     *
     * @param input {@link TokenStream} holding the input to be tokenized
     * @param side the {@link Side} from which to chop off an n-gram
     * @param minGram the smallest n-gram to generate
     * @param maxGram the largest n-gram to generate
     */
        public EdgeNGramTokenFilter(TokenStream input, Side side, int minGram, int maxGram)
            : base(input)
        {
            if (!(side == Side.Front || side == Side.Back))
            {
                throw new ArgumentException("side must be either front or back");
            }

            if (minGram < 1)
            {
                throw new ArgumentException("minGram must be greater than zero");
            }

            if (minGram > maxGram)
            {
                throw new ArgumentException("minGram must not be greater than maxGram");
            }

            _minGram = minGram;
            _maxGram = maxGram;
            _side = side;
            _termAtt = AddAttribute<ITermAttribute>();
            _offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        public override bool IncrementToken()
        {
            while (true)
            {
                if (_curTermBuffer == null)
                {
                    if (!input.IncrementToken())
                    {
                        return false;
                    }
                    else
                    {
                        _curTermBuffer = (char[])_termAtt.TermBuffer().Clone();
                        _curTermLength = _termAtt.TermLength();
                        _curGramSize = _minGram;
                        _tokStart = _offsetAtt.StartOffset;
                    }
                }
                if (_curGramSize <= _maxGram)
                {
                    if (!(_curGramSize > _curTermLength         // if the remaining input is too short, we can't generate any n-grams
                          || _curGramSize > _maxGram))
                    {       // if we have hit the end of our n-gram size range, quit
                        // grab gramSize chars from front or back
                        int start = _side == Side.Front ? 0 : _curTermLength - _curGramSize;
                        int end = start + _curGramSize;
                        ClearAttributes();
                        _offsetAtt.SetOffset(_tokStart + start, _tokStart + end);
                        _termAtt.SetTermBuffer(_curTermBuffer, start, _curGramSize);
                        _curGramSize++;
                        return true;
                    }
                }
                _curTermBuffer = null;
            }
        }

        public override void Reset()
        {
            base.Reset();
            _curTermBuffer = null;
        }
    }
}