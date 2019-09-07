using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BoboBrowse.Api;
using BoboBrowse.Facets;
using BoboBrowse.Facets.data;
using BoboBrowse.Facets.impl;
using BoboBrowse.LangUtils;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace BoboFacets
{
    [TestFixture]
    class Class1
    {
        [Test]
        public void testRuntimeFilteredDateRange()
        {
            Lucene.Net.Util.Version version = Lucene.Net.Util.Version.LUCENE_29;

            var dir = new RAMDirectory();
            Analyzer analyzer = new PerFieldAnalyzerWrapper(new StandardAnalyzer(version),
                                                   new Dictionary<string, Analyzer> { { "organization", new KeywordAnalyzer() } });

            // Add content to the index
            var indexWriter = new IndexWriter(dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            foreach (var document in CreateDocuments())
            {
                indexWriter.AddDocument(document);
            }

            indexWriter.Commit();
            indexWriter.Dispose();





            //var ranges = new List<string> { "[2000/01/01 TO 2001/12/30]", "[2007/01/01 TO 2007/12/30]" };
            ////var handler = new FilteredRangeFacetHandler("filtered_date", "date", ranges);
            //var handler = new RangeFacetHandler("date", new PredefinedTermListFactory<DateTime>("yyyy/MM/dd"), ranges);

            //IndexReader reader = IndexReader.Open(dir, true);

            //// decorate it with a bobo index reader
            //BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader, new[] { handler });
            //IBrowsable browser = new BoboBrowser(boboReader);

            //var req = new BrowseRequest();
            //req.SetFacetSpec("filtered_date", new FacetSpec());


            //BrowseResult result = browser.Browse(req);

            //// Showing results now
            //int totalHits = result.NumHits;
            //BrowseHit[] hits = result.Hits;

            //Dictionary<String, IFacetAccessible> facetMap = result.FacetMap;


            // queries
            var dateRange = new List<String>();
            dateRange.Add("[" + DateTools.DateToString(new DateTime(1999, 1, 1), DateTools.Resolution.DAY) + " TO " + DateTools.DateToString(new DateTime(2000, 12, 30), DateTools.Resolution.DAY) + "]");
            dateRange.Add("[" + DateTools.DateToString(new DateTime(2001, 1, 1), DateTools.Resolution.DAY) + " TO *]");


            // color facet handler
            var dateHandler = new RangeFacetHandler("date", dateRange);
            var titleFacet = new SimpleFacetHandler("title");
            var handlerList = new List<FacetHandler>() { dateHandler, titleFacet };


            // opening a lucene index
            IndexReader reader = IndexReader.Open(dir);

            // decorate it with a bobo index reader
            BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader, handlerList);

            // creating a browse request
            var br = new BrowseRequest { Count = 10, Offset = 0 };

            // add a selection
            BrowseSelection sel = new BrowseSelection("title");
            //sel.AddValue("bodyintitle");
            br.AddSelection(sel);

            // parse a query
            var parser = new QueryParser(version, "id", new StandardAnalyzer(version));
            Query q = parser.Parse("*:*");
            br.Query = q;

            // add the facet output specs
            FacetSpec dateSpec = new FacetSpec();
            dateSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("date", dateSpec);
            FacetSpec titleSpec = new FacetSpec();
            titleSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("title", titleSpec);

            // perform browse
            var browser = new BoboBrowser(boboReader);
            BrowseResult result = browser.Browse(br);

            int totalHits = result.NumHits;
            BrowseHit[] hits = result.Hits;

            Dictionary<String, IFacetAccessible> facetMap = result.FacetMap;

            IFacetAccessible dateFacets = facetMap["date"];
            Debug.WriteLine("Facets date:");
            foreach (BrowseFacet facetVal in dateFacets.GetFacets())
            {
                Debug.WriteLine("Facet " + facetVal.Value + "(" + facetVal.HitCount + ")");
            }

            IFacetAccessible orgFacets = facetMap["title"];
            Debug.WriteLine("Facets title:");
            foreach (BrowseFacet facet in orgFacets.GetFacets())
            {
                Debug.WriteLine(facet.ToString());
            }

            int i = 42;
        }

        public static IEnumerable<Document> CreateDocuments()
        {
            var docs = new List<Document>();

            var doc = new Document();
            doc.Add(new Field("id", "1", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("date", DateTools.DateToString(new DateTime(2000, 1, 1), DateTools.Resolution.DAY), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            //doc.Add(new Field("date", "2000/01/01", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("title", "Mr", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            docs.Add(doc);

            doc = new Document();
            doc.Add(new Field("id", "2", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("date", DateTools.DateToString(new DateTime(2001, 1, 1), DateTools.Resolution.DAY), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            //doc.Add(new Field("date", "2001/01/01", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("title", "Mr", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            docs.Add(doc);

            doc = new Document();
            doc.Add(new Field("id", "3", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("date", DateTools.DateToString(new DateTime(2002, 1, 1), DateTools.Resolution.DAY), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            //doc.Add(new Field("date", "2002/01/01", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("title", "Ms", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            docs.Add(doc);

            doc = new Document();
            doc.Add(new Field("id", "4", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("date", DateTools.DateToString(new DateTime(2002, 1, 2), DateTools.Resolution.DAY), Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            //doc.Add(new Field("date", "2001/01/02", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            doc.Add(new Field("title", "Mrs", Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            docs.Add(doc);

            return docs;
        }
    }
}