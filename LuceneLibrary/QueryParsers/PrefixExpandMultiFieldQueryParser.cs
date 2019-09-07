using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Version = Lucene.Net.Util.Version;

namespace Com.Lybecker.LuceneLibrary.QueryParsers
{
    /// <summary>
    /// Expands PrefixQueries to BooleanQueryies with TermQueries underneath to allow for scoring.
    /// </summary>
    public class PrefixExpandMultiFieldQueryParser : MultiFieldQueryParser
    {
        private readonly IndexReader _indexReader;

        public PrefixExpandMultiFieldQueryParser(Version matchVersion, string[] fields, Analyzer analyzer, IDictionary<string, float> boosts, IndexReader indexReader)
            : base(matchVersion, fields, analyzer, boosts)
        {
            _indexReader = indexReader;
        }

        public PrefixExpandMultiFieldQueryParser(Version matchVersion, string[] fields, Analyzer analyzer, IndexReader indexReader)
            : base(matchVersion, fields, analyzer)
        {
            _indexReader = indexReader;
        }

        protected override Query GetPrefixQuery(string field, string termStr)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                IList<BooleanClause> clauses = new List<BooleanClause>();
                for (int i = 0; i < fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(ExpandPrefixQuery(fields[i], termStr), Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }

            return ExpandPrefixQuery(field, termStr);
        }

        private Query ExpandPrefixQuery(string field, string termStr)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("field must have a value.");

            var terms = new List<string>();

            using (TokenStream source = base.Analyzer.TokenStream(field, new StringReader(termStr)))
            {
                var term = source.AddAttribute<ITermAttribute>();

                while (source.IncrementToken())
                {
                    terms.Add(term.Term);
                }
            }

            if (terms.Count != 1)
            {
                /* this means that the analyzer used either added or consumed
                       * (common for a stemmer) tokens, and we can't build a PrefixQuery */
                throw new ParseException(string.Format("Cannot build PrefixQuery with analyzer {0} with {1}.",
                                                       Analyzer.GetType(),
                                                       terms.Count > 1 ? "token(s) added" : "token consumed"));
            }

            var prefixQuery = new PrefixQuery(new Term(field, terms[0]));

            //If the user passes a map of boosts
            if (boosts != null)
            {
                prefixQuery.Boost = base.boosts[field];
            }

            var r = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE.Rewrite(_indexReader, prefixQuery);

            return r;
        }
    }
}
