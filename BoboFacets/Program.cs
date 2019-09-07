using System;
using System.Collections.Generic;
using System.Linq;
using BoboBrowse.Api;
using BoboBrowse.Facets;
using BoboBrowse.Facets.impl;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Analysis;

namespace BoboFacets
{
    class Program
    {
        static void Main(string[] args)
        {
            Lucene.Net.Util.Version version = Lucene.Net.Util.Version.LUCENE_29;

            var dir = new RAMDirectory();
            Analyzer analyzer = new PerFieldAnalyzerWrapper(new StandardAnalyzer(version),
                                                   new Dictionary<string, Analyzer> { { "organization", new KeywordAnalyzer() } });

            // Add content to the index
            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            foreach (var document in CreateDocuments(CreateTestData()))
            {
                indexWriter.AddDocument(document);
            }

            indexWriter.Commit();
            indexWriter.Dispose();

            var orgFieldName = "organization";
            var titleFieldName = "title";
            var createdAtName = "created_at";

            var rangeFacetName = "rangeFacet";

            //var orgFacetHandler = new PathFacetHandler(orgFieldName);
            //orgFacetHandler.SetSeparator("/");
            var orgFacetHandler = new MultiValueFacetHandler(orgFieldName);
            FacetHandler titleFacetHandler = new MultiValueFacetHandler(titleFieldName);
            var createdAtFacetHandler = new SimpleFacetHandler(createdAtName);
            //var ranges = new List<string> { "[2000/01/01 TO 2000/12/30]", "[2001/01/01 TO 2007/12/30]" };
            var ranges = new List<string> { "[20000101 TO 20001230]", "[20040101 TO *]" };
            var rangeFacetHandler = new RangeFacetHandler(rangeFacetName, createdAtName, ranges);
            

            IndexReader reader = IndexReader.Open(dir, true);

            // decorate it with a bobo index reader
            BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader, new[] { orgFacetHandler, titleFacetHandler, createdAtFacetHandler, rangeFacetHandler });

            // creating a browse request
            var browseRequest = new BrowseRequest { Count = 10, Offset = 0, FetchStoredFields = true };

            // add a selection
            //var orgSelection = new BrowseSelection(orgFieldName);
            //orgSelection.AddValue("A/B");
            //browseRequest.AddSelection(orgSelection);

            var titleSelction = new BrowseSelection(titleFieldName);
            //titleSelction.AddValue("Læge");
            browseRequest.AddSelection(titleSelction);

            browseRequest.AddSelection(new BrowseSelection(rangeFacetName));

            // parse a query
            var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "name", new KeywordAnalyzer());
            Query q = parser.Parse("an*");
            browseRequest.Query = q;

            // add the facet output specs
            var orgSpec = new FacetSpec { OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc };
            browseRequest.SetFacetSpec(orgFieldName, orgSpec);
            var titleSpec = new FacetSpec { MinHitCount = 1, OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc };
            browseRequest.SetFacetSpec(titleFieldName, titleSpec);
            //var createdAtSpec = new FacetSpec { MinHitCount = 1, OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc };
            //browseRequest.SetFacetSpec(createdAtName, createdAtSpec);
            var rangeSpec = new FacetSpec { MinHitCount = 1, OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc};
            browseRequest.SetFacetSpec(rangeFacetName, rangeSpec);

            // perform browse
            IBrowsable browser = new BoboBrowser(boboReader);
            
            BrowseResult result = browser.Browse(browseRequest);

            // Showing results now
            int totalHits = result.NumHits;
            BrowseHit[] hits = result.Hits;

            Dictionary<String, IFacetAccessible> facetMap = result.FacetMap;

            IFacetAccessible orgFacets = facetMap[orgFieldName];
            Console.WriteLine("Facets {0}:", orgFieldName);
            foreach (BrowseFacet facet in orgFacets.GetFacets())
            {
                Console.WriteLine(facet.ToString());
            }

            IFacetAccessible titleFacets = facetMap[titleFieldName];
            Console.WriteLine("Facets {0}:", titleFieldName);
            foreach (BrowseFacet facet in titleFacets.GetFacets())
            {
                Console.WriteLine(facet.ToString());
            }

            if (facetMap.ContainsKey(createdAtName))
            {
                IFacetAccessible createdAtFacets = facetMap[createdAtName];
                Console.WriteLine("Facets {0}:", createdAtName);
                foreach (BrowseFacet facet in createdAtFacets.GetFacets())
                {
                    Console.WriteLine(facet.ToString());
                }
            }


            if (facetMap.ContainsKey(rangeFacetName))
            {
                Console.WriteLine("-------------------------------------");
                IFacetAccessible rangeFacets = facetMap[rangeFacetName];
                Console.WriteLine("Facets {0}:", rangeFacets);
                foreach (BrowseFacet facet in rangeFacets.GetFacets())
                {
                    Console.WriteLine(facet.ToString());
                }
                Console.WriteLine("-------------------------------------");
            }


            Console.WriteLine("Actual items (total: {0}) query: {1}:", totalHits, q);
            for (int i = 0; i < hits.Length; ++i)
            {
                BrowseHit browseHit = hits[i];
                Console.WriteLine("id = {0}, Name = {1}, Organizations = {{{2}}}, Titles = {{{3}}}, Created at = {4}", browseHit.StoredFields.Get("id"), browseHit.StoredFields.Get("name"), string.Join(", ", browseHit.StoredFields.GetValues("organization").Distinct()), string.Join(", ", browseHit.StoredFields.GetValues("title").Distinct()), browseHit.StoredFields.Get("created_at"));
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
                doc.Add(new Field("created_at", DateTools.DateToString(person.CreatedAt, DateTools.Resolution.DAY), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                foreach (var organization in person.Organizations)
                {
                    doc.Add(new Field("organization", organization, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                }
                foreach (var title in person.Titles)
                {
                    doc.Add(new Field("title", title, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
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
                                   Name = "Anders Lybecker",
                                   Organizations = new List<string> { "A", "A/B", "A/B/C", "A/D" },
                                   Titles = new List<string> { "Læge", "Overlæge" },
                                   CreatedAt = new DateTime(2000, 4, 2),
                               },
                           new Person()
                               {
                                   Id = 2,
                                   Name = "Mette Knots",
                                   Organizations = new List<string> { "A", "A/B" },
                                   Titles = new List<string> { "Sygeplejerske" },
                                   CreatedAt = new DateTime(2000, 2, 1),
                               },
                           new Person()
                               {
                                   Id = 3,
                                   Name = "Philip Måløv",
                                   Organizations = new List<string> { "A", "A/D" },
                                   Titles = new List<string> { "Læge" },
                                   CreatedAt = new DateTime(2001, 3, 1),
                               },
                           new Person()
                               {
                                   Id = 4,
                                   Name = "Victoria Hertz Lybecker",
                                   Organizations = new List<string> { "A", "A/B", "A/D" },
                                   Titles = new List<string> { "Sygeplejerske" },
                                   CreatedAt = new DateTime(2001, 1, 1),
                               },
                           new Person()
                               {
                                   Id = 5,
                                   Name = "Andreas Skeel",
                                   Organizations = new List<string> { "A", "A/B" },
                                   Titles = new List<string> { "Læge" },
                                   CreatedAt = new DateTime(2005, 1, 1),
                               },
                           new Person()
                               {
                                   Id = 6,
                                   Name = "Andersine And",
                                   Organizations = new List<string> { "A" },
                                   Titles = new List<string> { "Sygeplejerske" },
                                   CreatedAt = new DateTime(2005, 1, 1),
                               },
                           new Person()
                               {
                                   Id = 7,
                                   Name = "Anja Hertz",
                                   Organizations = new List<string> { "A", "A/D" },
                                   Titles = new List<string> { "Sygeplejerske" },
                                   CreatedAt = new DateTime(2000, 1, 1),
                               },
                           new Person()
                               {
                                   Id = 8,
                                   Name = "Anne Slot",
                                   Organizations = new List<string> { "A", "A/B", "A/B/C" },
                                   Titles = new List<string> { "Overlæge" },
                                   CreatedAt = new DateTime(2000, 1, 1),
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
        public DateTime CreatedAt { get; set; }
    }
}