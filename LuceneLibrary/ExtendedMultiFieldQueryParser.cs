using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace AnalyzerSearcher
{
    public class ExtendedMultiFieldQueryParser : MultiFieldQueryParser
    {
        public string[] ReverseFields { get; set; }

        public ExtendedMultiFieldQueryParser(Lucene.Net.Util.Version matchVersion, string[] fields, Analyzer analyzer)
            : base(matchVersion, fields, analyzer)
        {
            ReverseFields = new string[0];
        }

        protected override Query GetWildcardQuery(string field, string termStr)
        {
            if (field == null)
            {
                var clauses = new List<BooleanClause>();
                for (int i = 0; i < fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(GetWildcardQuery(fields[i], termStr), Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }

            if (IsPrefixQuery(termStr) && ReverseFields.Contains(field))
                return GetPrefixQueryWithReveredTerm(field, termStr);
            
            return base.GetWildcardQuery(field, termStr);
        }

        private static bool IsPrefixQuery(string termStr)
        {
            if (termStr.Length > 0)
                return (termStr[0] == '*');

            return false;
        }

        private Query GetPrefixQueryWithReveredTerm(System.String field, System.String termStr)
        {
            if ("*".Equals(field))
            {
                if ("*".Equals(termStr))
                    return NewMatchAllDocsQuery();
            }

            termStr = termStr.ToLower();

            // Remove the leading *
            termStr = termStr.Remove(0, 1);

            char[] chars = termStr.ToCharArray();
            Array.Reverse(chars);

            var t = new Term(field, new string(chars));

            return new PrefixQuery(t);
        }
    }
}