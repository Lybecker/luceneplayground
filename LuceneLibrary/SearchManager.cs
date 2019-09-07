using System.Threading;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace Com.Lybecker.LuceneLibrary
{
    /// <summary>
    /// Threadsafe management of IndexReaders and IndexSearcher.s
    /// Uses the Lucene segment level IndexReader cache by using the IndexReader.Open.
    /// Inherite and implement the Warm method for custom IndexReader caching warmup.
    /// </summary>
    public class SearcherManager
    {
        private IndexSearcher _currentSearcher;
        private readonly IndexWriter _writer;
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="dir">Directory to read from</param>
        public SearcherManager(Directory dir)
        {
            _currentSearcher = new IndexSearcher(IndexReader.Open(dir, true));
            Warm(_currentSearcher);
        }

        /// <summary>
        /// Use near realtime search functionality to create the IndexReader.
        /// </summary>
        /// <param name="writer">The current IndexReader.</param>
        public SearcherManager(IndexWriter writer)
        {
            _writer = writer;
            _currentSearcher = new IndexSearcher(writer.GetReader());
            Warm(_currentSearcher);

            writer.MergedSegmentWarmer = new IndexReaderWarmerImpl(this);
        }

        private class IndexReaderWarmerImpl : IndexWriter.IndexReaderWarmer
        {
            private readonly SearcherManager _searcherManager;

            public IndexReaderWarmerImpl(SearcherManager searcherManager)
            {
                _searcherManager = searcherManager;
            }

            public override void Warm(IndexReader reader)
            {
                _searcherManager.Warm(new IndexSearcher(reader));
            }
        }

        /// <summary>
        /// Implement method to auto warm IndexSearcher.
        /// </summary>
        /// <param name="searcher"></param>
        public virtual void Warm(IndexSearcher searcher)
        { }

        private bool _reopening;

        private void StartReopen()
        {
            Monitor.Enter(_syncRoot);
            try
            {
                while (_reopening)
                {
                    Monitor.Wait(_syncRoot);
                }
                _reopening = true;
            }
            finally
            {
                Monitor.Exit(_syncRoot);
            }
        }

        private void DoneReopen()
        {
            Monitor.Enter(_syncRoot);
            try
            {
                _reopening = false;
                Monitor.PulseAll(_syncRoot);
            }
            finally
            {
                Monitor.Exit(_syncRoot);
            }
            _reopening = false;
        }

        /// <summary>
        /// Check if newer version of Lucene index exist.
        /// If it does, create a new IndexSearcher.
        /// </summary>
        public void MaybeReopen()
        {
            StartReopen();

            try
            {
                IndexSearcher searcher = GetSearcher();

                try
                {
                    IndexReader newReader = _currentSearcher.IndexReader.Reopen();
                    if (newReader != _currentSearcher.IndexReader)
                    {
                        var newSearcher = new IndexSearcher(newReader);
                        if (_writer == null)
                        {
                            Warm(newSearcher);
                        }
                        SwapSearcher(newSearcher);
                    }
                }
                finally
                {
                    ReleaseSearcher(searcher);
                }
            }
            finally
            {
                DoneReopen();
            }
        }

        /// <summary>
        /// Get an ready to use instance of IndexSearcher.
        /// Remember to Release the Searcher.
        /// 
        /// Sample usage:
        /// <code>
        ///     var manager = new SearcherManager(dir);
        /// 
        ///     var searcher = manager.GetSearcher();
        ///     try
        ///     {
        ///         searcher.Search(...)
        ///     }
        ///     finally
        ///     {
        ///         manager.ReleaseSearcher(searcher);
        ///     }
        /// </code>
        /// </summary>
        /// <returns></returns>
        public IndexSearcher GetSearcher()
        {
            Monitor.Enter(_syncRoot);
            try
            {
                _currentSearcher.IndexReader.IncRef();
                return _currentSearcher;
            }
            finally
            {
                Monitor.Exit(_syncRoot);
            }
        }

        /// <summary>
        /// Releases the instance of the IndexSearcher
        /// </summary>
        /// <param name="searcher"></param>
        public void ReleaseSearcher(IndexSearcher searcher)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                searcher.IndexReader.DecRef();
            }
            finally
            {
                Monitor.Exit(_syncRoot);
            }
        }

        private void SwapSearcher(IndexSearcher newSearcher)
        {
            Monitor.Enter(_syncRoot);
            try
            {
                ReleaseSearcher(_currentSearcher);
                _currentSearcher = newSearcher;
            }
            finally
            {
                Monitor.Exit(_syncRoot);
            }
        }
    }
}