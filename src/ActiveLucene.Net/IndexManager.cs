// Copyright 2010 Tim Schmidt and Kevin Dotzenrod
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = System.IO.Directory;
using Version = Lucene.Net.Util.Version;

namespace ActiveLucene.Net
{
    public interface IIndexManager
    {
        void BeginRebuildRepository(object state);
        void RebuildRepository(object state);

        event RebuildRepositoryDelegate OnRebuildRepository;
        event RepositoryRebuiltDelegate OnRepositoryRebuilt;
        event WarmUpIndexDelegate OnWarmUpIndex;

        bool IsOpen { get; }
        string CurrentIndexPath { get; }
        int GetIndexCount();
        long GetIndexSize();

        void Open();
        void Open(bool alwaysCreate);
        void Close();
        void VerifyReader();
        void CleanUpDirectories();

        DisposableIndexSearcher GetIndexSearcher();
        DisposableIndexWriter GetIndexWriter();
        DisposableIndexSearcher<T> GetIndexSearcher<T>() where T : class;
        DisposableIndexWriter<T> GetIndexWriter<T>() where T : class;
        T GetRecord<T>(int doc) where T : class;
    }

    public interface IIndexManager<T> : IIndexManager where T : class
    {
    }

    public delegate void RebuildRepositoryDelegate(DisposableIndexWriter indexWriter, object context);
    public delegate void RepositoryRebuiltDelegate(object context);
    public delegate void WarmUpIndexDelegate(DisposableIndexSearcher indexSearcher);

    public class IndexManager : IIndexManager
    {
        private readonly object _rebuildLock = new object();
        private readonly object _maintenanceLock = new object();

        private readonly string _basePath;
        private readonly Analyzer _analyzer;
        private readonly bool _readOnly;
        private LockableIndexSearcher _indexSearcher;

        public IndexManager(string basePath) : this(basePath, new StandardAnalyzer(Version.LUCENE_29), false)
        {}

        public IndexManager(string basePath, Analyzer analyzer) : this(basePath, analyzer, false)
        {}

        public IndexManager(string basePath, Analyzer analyzer, bool readOnly)
        {
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            _basePath = basePath;
            _analyzer = analyzer;
            _readOnly = readOnly;
        }

        public void BeginRebuildRepository(object context)
        {
            var dlg = new Action<object>(RebuildRepository);
            dlg.BeginInvoke(context, EndRebuildRepository, dlg);
        }

        protected static void EndRebuildRepository(IAsyncResult ar)
        {
            var dlg = (Action<object>)ar.AsyncState;
            dlg.EndInvoke(ar);
        }

        public void RebuildRepository(object context)
        {
            if(OnRebuildRepository == null)
                return;

            if (!Monitor.TryEnter(_rebuildLock))
                return;

            try
            {
                var buildingPath = Path.Combine(_basePath, "building" + Guid.NewGuid().ToString("n"));

                using (new IndexWriter(FSDirectory.Open(new DirectoryInfo(buildingPath)),
                                       _analyzer, true, IndexWriter.MaxFieldLength.LIMITED))
                {}

                var indexSearcher = new LockableIndexSearcher(buildingPath, false);
                using (var indexWriter = GetIndexWriter<object>(indexSearcher, Timeout.Infinite, null))
                {
                    try
                    {
                        OnRebuildRepository(indexWriter, context);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex);
                        indexWriter.Dispose();
                        CleanUpDirectories();
                        return;
                    }

                    indexWriter.Optimize();
                }

                Directory.Move(buildingPath, NextHighestNumberedFolder());
                OpenBestRepository();

                if (OnRepositoryRebuilt != null)
                    OnRepositoryRebuilt(context);
            }
            finally
            {
                Monitor.Exit(_rebuildLock);
            }
        }

        public event RebuildRepositoryDelegate OnRebuildRepository;
        public event RepositoryRebuiltDelegate OnRepositoryRebuilt;
        public event WarmUpIndexDelegate OnWarmUpIndex;

        public bool IsOpen
        {
            get { return _indexSearcher != null; }
        }

        public int GetIndexCount()
        {
            using (var searcher = GetIndexSearcher())
            {
                return searcher.IndexReader.NumDocs();
            }
        }

        public long GetIndexSize()
        {
            using(var searcher = GetIndexSearcher())
            {
                var directory = searcher.IndexReader.Directory();
                return directory.ListAll().Sum(file => new FileInfo(Path.Combine(CurrentIndexPath, file)).Length);
            }
        }

        public void Open()
        {
            Open(false);
        }

        public void Open(bool alwaysCreate)
        {
            if(alwaysCreate || !TryOpenBestRepository())
                CreateAndOpenNextRepository();
        }

        public void Close()
        {
            if (IsOpen)
            {
                using (_indexSearcher.GetWriteLock())
                {
                    _indexSearcher.Dispose();
                }

                _indexSearcher = null;
            }
        }

        public void VerifyReader()
        {
            if (!IsOpen || !_indexSearcher.IndexReader.IsCurrent())
                OpenBestRepository();
        }

        protected void CreateAndOpenNextRepository()
        {
            lock (_maintenanceLock)
            {
                using (new IndexWriter(FSDirectory.Open(new DirectoryInfo(NextHighestNumberedFolder())),
                                       _analyzer, true, IndexWriter.MaxFieldLength.LIMITED))
                {}

                OpenBestRepository();
            }
        }

        protected bool TryOpenBestRepository()
        {
            try
            {
                OpenBestRepository();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected void OpenBestRepository()
        {
            lock (_maintenanceLock)
            {
                var bestFolder = HighestNumberedFolder();
                if (bestFolder == null)
                    throw new Exception("No repository folders found.");

                var newIndexSearcher = new LockableIndexSearcher(bestFolder, _readOnly);
                if(OnWarmUpIndex != null)
                {
                    using(var indexSearcher = new DisposableIndexSearcher(newIndexSearcher.GetReadLock(), newIndexSearcher))
                    {
                        OnWarmUpIndex(indexSearcher);
                    }
                }

                var oldIndexSearcher = _indexSearcher;
                _indexSearcher = newIndexSearcher;
                CurrentIndexPath = bestFolder;

                if(oldIndexSearcher != null)
                {
                    using(oldIndexSearcher.GetWriteLock())
                    {
                        oldIndexSearcher.Dispose();
                    }
                }

                CleanUpDirectories();
            }
        }

        protected string HighestNumberedFolder()
        {
            var highestNumber = -1;
            string highestFolder = null;
            foreach (var filePath in Directory.GetDirectories(_basePath))
            {
                var fileName = Path.GetFileName(filePath);

                int fileNumber;
                if (!Int32.TryParse(fileName, out fileNumber))
                    continue;

                if (highestNumber < fileNumber)
                {
                    highestNumber = fileNumber;
                    highestFolder = Path.Combine(_basePath, fileName);
                }
            }

            return highestFolder;
        }

        protected string NextHighestNumberedFolder()
        {
            var currentHighest = HighestNumberedFolder();
            int nextHighest = 1;
            if (currentHighest != null)
            {
                nextHighest = Int32.Parse(Path.GetFileName(currentHighest)) + 1;
            }

            return Path.Combine(_basePath, nextHighest.ToString("00000000"));
        }

        public void CleanUpDirectories()
        {
            if (!Monitor.TryEnter(_rebuildLock))
                return;

            try
            {
                lock (_maintenanceLock)
                {
                    foreach (var directory in Directory.GetDirectories(_basePath))
                    {
                        if (!directory.Equals(CurrentIndexPath, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                Directory.Delete(directory, true);
                            }
                            catch
                            { }
                        }
                    }
                }
            }
            finally
            {
                Monitor.Exit(_rebuildLock);
            }
        }

        public string CurrentIndexPath { get; private set; }

        public DisposableIndexSearcher GetIndexSearcher()
        {
            CheckOpen();
            return new DisposableIndexSearcher(_indexSearcher.GetReadLock(), _indexSearcher);
        }

        public DisposableIndexSearcher GetIndexSearcher(int millisecondsTimeout)
        {
            CheckOpen();
            return new DisposableIndexSearcher(_indexSearcher.GetReadLock(millisecondsTimeout), _indexSearcher);
        }

        public DisposableIndexSearcher<T> GetIndexSearcher<T>() where T : class
        {
            CheckOpen();
            return new DisposableIndexSearcher<T>(_indexSearcher.GetReadLock(), _indexSearcher);
        }

        public DisposableIndexWriter GetIndexWriter()
        {
            return GetIndexWriter(true, Timeout.Infinite);
        }

        public DisposableIndexWriter<T> GetIndexWriter<T>() where T : class
        {
            CheckOpen();
            return new DisposableIndexWriter<T>(_indexSearcher.GetWriteLock(), _indexSearcher, _analyzer, (Action) null);
        }

        public DisposableIndexWriter GetIndexWriter(int millisecondsTimeout)
        {
            return GetIndexWriter(true, millisecondsTimeout);
        }

        public DisposableIndexWriter GetIndexWriter(bool shouldVerifyOnExit)
        {
            return GetIndexWriter(shouldVerifyOnExit, Timeout.Infinite);
        }

        public DisposableIndexWriter GetIndexWriter(bool shouldVerifyOnExit, int millisecondsTimeout)
        {
            CheckOpen();
            return GetIndexWriter<object>(_indexSearcher, millisecondsTimeout, shouldVerifyOnExit ? VerifyReader : (Action) null);
        }

        internal DisposableIndexWriter<T> GetIndexWriter<T>(LockableIndexSearcher indexSearcher, int millisecondsTimeout, Action onExit) where T : class
        {
            return new DisposableIndexWriter<T>(indexSearcher.GetWriteLock(millisecondsTimeout), indexSearcher, _analyzer, onExit);
        }

        public T GetRecord<T>(int doc) where T : class
        {
            using (var searcher = GetIndexSearcher())
            {
                return LuceneMediator<T>.ToRecord(searcher.Doc(doc));
            }
        }

        protected void CheckOpen()
        {
            if (!IsOpen)
                throw new Exception("Index is closed.");
        }
    }

    public class IndexManager<T> : IndexManager where T : class
    {
        public IndexManager(string basePath) : base(basePath)
        {
        }

        public IndexManager(string basePath, Analyzer analyzer) : base(basePath, analyzer)
        {
        }

        public IndexManager(string basePath, Analyzer analyzer, bool readOnly) : base(basePath, analyzer, readOnly)
        {
        }

        public T GetRecord(int doc)
        {
            using (var searcher = GetIndexSearcher())
            {
                return LuceneMediator<T>.ToRecord(searcher.Doc(doc));
            }
        }
    }
}
