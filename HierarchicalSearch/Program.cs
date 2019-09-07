using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace HierarchicalSearch
{
    class Program
    {
        static void Main(string[] args)
        {
            Lucene.Net.Util.Version version = Lucene.Net.Util.Version.LUCENE_29;

            Directory dir = new RAMDirectory();
            //Analyzer analyzer = new StandardAnalyzer(version);
            Analyzer analyzer = new PerFieldAnalyzerWrapper(new StandardAnalyzer(version),
                                                   new Dictionary<string, Analyzer> { { "organization", new KeywordAnalyzer() } });

            // Add content to the index
            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            //indexWriter.SetInfoStream(new System.IO.StreamWriter(Console.OpenStandardOutput()));

            foreach (var document in CreateDocuments(CreateTestData()))
            {
                indexWriter.AddDocument(document);
            }

            indexWriter.Commit();
            indexWriter.Dispose();


            //// Search for the content
            //var parser = new MultiFieldQueryParser(version, new[] { "organization" }, analyzer);
            //Query q = parser.Parse("A/D");


            //var searcher = new IndexSearcher(dir, true);

            //TopDocs hits = searcher.Search(q, null, 5, Sort.RELEVANCE);

            //Console.WriteLine("Found {0} document(s) that matched query '{1}':", hits.TotalHits, q);
            //foreach (ScoreDoc match in hits.ScoreDocs)
            //{
            //    Document doc = searcher.Doc(match.Doc);

            //    Console.WriteLine("Matched id = {0}, Name = {1}, Organizations = {{{2}}}", doc.Get("id"), doc.Get("name"), string.Join(", ", doc.GetValues("organization")));

            //    //Console.WriteLine(Explain(searcher, q, match));
            //}
            //searcher.Dispose();

            var reader = IndexReader.Open(dir, true);
            var sfs = new SimpleFacetedSearch(reader, new string[] { "organization", "title" });
            
            // then pass in the query into the search like you normally would with a typical search class.
            Query query = new QueryParser(version, "name", new StandardAnalyzer(version)).Parse("An*");
            var hits = sfs.Search(query, 10);

            // what comes back is different than normal.
            // the result documents & hits are grouped by facets.

            // you'll need to iterate over groups of hits-per-facet.

            long totalHits = hits.TotalHitCount;
            foreach (SimpleFacetedSearch.HitsPerFacet hpg in hits.HitsPerFacet)
            {
                long hitCountPerGroup = hpg.HitCount;
                SimpleFacetedSearch.FacetName facetName = hpg.Name;

                Console.WriteLine(">>" + facetName + ": " + hpg.HitCount);

                //foreach (Document doc in hpg.Documents)
                //{
                //    string text = doc.Get("name");

                //    // replace with logging or your desired output writer
                //    Console.WriteLine(">>" + facetName + ": " + text);
                //}
            }
        }

        public static IEnumerable<Document> CreateDocuments(IEnumerable<Person> persons)
        {
            var docs = new List<Document>(persons.Count());

            foreach (var person in persons)
            {
                var doc = new Document();
                doc.Add(new Field("id", person.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                doc.Add(new Field("name", person.Name, Field.Store.YES, Field.Index.ANALYZED));
                foreach (var organization in person.Organizations)
                {
                    doc.Add(new Field("organization", organization, Field.Store.YES, Field.Index.ANALYZED));
                }
                foreach (var title in person.Titles)
                {
                    doc.Add(new Field("title", title, Field.Store.YES, Field.Index.ANALYZED));
                }
                docs.Add(doc);
            }

            return docs;
        }

        public static IEnumerable<Person> CreateTestData()
        {
            return new List<Person>
                       {
                           new Person()
                               {
                                   Id = 1,
                                   Name = "Anders",
                                   Organizations = new List<string> { "A", "A/B", "A/B/C" },
                                   Titles = new List<string> { "Læge", "Overlæge", "Sygeplejerske" },
                               },
                           new Person()
                               {
                                   Id = 2,
                                   Name = "Mette",
                                   Organizations = new List<string> { "A", "A/B" },
                                   Titles = new List<string> { "Sygeplejerske" },
                               },
                           new Person()
                               {
                                   Id = 3,
                                   Name = "Philip",
                                   Organizations = new List<string> { "A", "A/D" },
                                   Titles = new List<string> { "Læge" },
                               },
                           new Person()
                               {
                                   Id = 4,
                                   Name = "Victoria",
                                   Organizations = new List<string> { "A", "A/B", "A/D" },
                                   Titles = new List<string> { "Sygeplejerske" },
                               },
                           new Person()
                               {
                                   Id = 5,
                                   Name = "Andreas",
                                   Organizations = new List<string> { "A", "A/B" },
                                   Titles = new List<string> { "Læge", "Overlæge" },
                               },
                       };
        }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Organizations { get; set; }
        public IEnumerable<string> Titles { get; set; }
    }
}
