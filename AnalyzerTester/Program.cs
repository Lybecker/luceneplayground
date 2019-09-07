using System;
using System.Collections.Generic;
using System.Linq;
using Com.Lybecker.LuceneLibrary;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;

namespace Com.Lybecker.AnalyzerTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Lucene.Net.Util.Version version = Lucene.Net.Util.Version.LUCENE_29;

            var values = new List<string>()
                             {
                                 "ab",
                                 "a b",
                                 "a-b",
                                 "a_b",
                                 "a/b",
                                 "a.b",
                             };

            var util = new Util();

            Analyzer analyzer = new StandardAnalyzer(version);

            using (var tester = new AnalyzerTester(version, analyzer, values))
            {
                PrintTestName("StandardAnalyzer");
                //util.PrintTerms(tester.GetIndexReader(), AnalyzerTester.FieldName);

                foreach (var value in values)
                {
                    SearchAndPrintResult(tester.Search, analyzer, value);
                }
                SearchAndPrintResult(tester.Search, analyzer, "a*");
                SearchAndPrintResult(tester.Search, analyzer, "a*b");
                SearchAndPrintResult(tester.Search, analyzer, "a?b");
            }

            analyzer = new WhitespaceAnalyzer();

            using (var tester = new AnalyzerTester(version, analyzer, values))
            {
                PrintTestName("WhitespaceAnalyzer");
                //util.PrintTerms(tester.GetIndexReader(), AnalyzerTester.FieldName);
                //var x = util.GetDocumentFieldValues(tester.GetIndexReader(), AnalyzerTester.FieldName);

                foreach (var value in values)
                {
                    SearchAndPrintResult(tester.Search, analyzer, value);
                }
                SearchAndPrintResult(tester.Search, analyzer, "a*");
                SearchAndPrintResult(tester.Search, analyzer, "a*b");
                SearchAndPrintResult(tester.Search, analyzer, "a?b");
            }
        }

        static void PrintTestName(string name)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(name);
            Console.ResetColor();
        }

        static void SearchAndPrintResult(Func<Analyzer, string, IEnumerable<string>> searcher, Analyzer analyzer, string queryString)
        {

            var result = searcher(analyzer, queryString).ToList();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Result for '{0}' (Total: {1})", queryString, result.Count());
            Console.ResetColor();
            result.ForEach(Console.WriteLine);
        }


        static void SearchAndPrintResult(Func<Query, IEnumerable<string>> searcher, Query query)
        {
            var result = searcher(query).ToList();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Result for '{0}' (Total: {1})", query, result.Count);
            Console.ResetColor();
            result.ForEach(Console.WriteLine);
        }
    }
}