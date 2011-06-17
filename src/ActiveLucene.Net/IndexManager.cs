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
using Lucene.Net.Store;
using Directory = System.IO.Directory;
using Version = Lucene.Net.Util.Version;

namespace ActiveLucene.Net
{
    public interface IIndexManager<T> where T : class
    {
        void BeginRebuildRepository(object state);
        void RebuildRepository(object state);

        event RebuildRepositoryDelegate<T> OnRebuildRepository;
        event RepositoryRebuiltDelegate OnRepositoryRebuilt;
        event WarmUpIndexDelegate<T> OnWarmUpIndex;

        bool IsOpen { get; }
        string CurrentIndexPath { get; }
        int GetIndexCount();
        long GetIndexSize();

        void Open();
        void Open(bool alwaysCreate);
        void Close();
        void VerifyReader();
        void CleanUpDirectories();

        DisposableIndexSearcher<T> GetIndexSearcher();
        DisposableIndexWriter<T> GetIndexWriter();
        T GetRecord(int doc);
    }

    public delegate void RebuildRepositoryDelegate<T>(DisposableIndexWriter<T> indexWriter, object context) where T : class;
    public delegate void RepositoryRebuiltDelegate(object context);
    public delegate void WarmUpIndexDelegate<T>(DisposableIndexSearcher<T> indexSearcher) where T : class;


    public class IndexManager<T> : IIndexManager<T> where T : class
    {
        private readonly object _rebuildLock = new object();
        private readonly object _maintenanceLock = new object();

        private readonly string _basePath;
        private readonly Analyzer _analyzer;
        private readonly bool _readOnly;
        private LockableIndexSearcher _indexSearcher;

        public IndexManager(string basePath) : this(basePath, new StandardAnalyzer(Version.LUCENE_CURRENT), false)
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

                var writer = new IndexWriter(FSDirectory.Open(new DirectoryInfo(buildingPath)),
                    _analyzer, true, IndexWriter.MaxFieldLength.LIMITED);
                writer.Close();

                var indexSearcher = new LockableIndexSearcher(buildingPath, false);
                using (var indexWriter = GetIndexWriter(indexSearcher, Timeout.Infinite, null))
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
                        throw;
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

        public event RebuildRepositoryDelegate<T> OnRebuildRepository;
        public event RepositoryRebuiltDelegate OnRepositoryRebuilt;
        public event WarmUpIndexDelegate<T> OnWarmUpIndex;

        public bool IsOpen
        {
            get { return _indexSearcher != null; }
        }

        public int GetIndexCount()
        {
            using(var searcher = GetIndexSearcher())
            {
                return searcher.MaxDoc();
            }
        }

        public long GetIndexSize()
        {
            using(var searcher = GetIndexSearcher())
            {
                var directory = searcher.GetIndexReader().Directory();
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
                    _indexSearcher.Close();
                }

                _indexSearcher = null;
            }
        }

        public void VerifyReader()
        {
            if (!IsOpen || !_indexSearcher.GetIndexReader().IsCurrent())
                OpenBestRepository();
        }

        protected void CreateAndOpenNextRepository()
        {
            lock (_maintenanceLock)
            {
                var writer = new IndexWriter(FSDirectory.Open(new DirectoryInfo(NextHighestNumberedFolder())),
                                             _analyzer, true, IndexWriter.MaxFieldLength.LIMITED);
                writer.Close();
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
                    using(var indexSearcher = new DisposableIndexSearcher<T>(newIndexSearcher.GetReadLock(), newIndexSearcher))
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
                        oldIndexSearcher.Close();
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

        public DisposableIndexSearcher<T> GetIndexSearcher()
        {
            CheckOpen();
            return new DisposableIndexSearcher<T>(_indexSearcher.GetReadLock(), _indexSearcher);
        }

        public DisposableIndexSearcher<T> GetIndexSearcher(int millisecondsTimeout)
        {
            CheckOpen();
            return new DisposableIndexSearcher<T>(_indexSearcher.GetReadLock(millisecondsTimeout), _indexSearcher);
        }

        public DisposableIndexWriter<T> GetIndexWriter()
        {
            return GetIndexWriter(true, Timeout.Infinite);
        }

        public DisposableIndexWriter<T> GetIndexWriter(int millisecondsTimeout)
        {
            return GetIndexWriter(true, millisecondsTimeout);
        }

        public DisposableIndexWriter<T> GetIndexWriter(bool shouldVerifyOnExit)
        {
            return GetIndexWriter(shouldVerifyOnExit, Timeout.Infinite);
        }

        public DisposableIndexWriter<T> GetIndexWriter(bool shouldVerifyOnExit, int millisecondsTimeout)
        {
            CheckOpen();
            return GetIndexWriter(_indexSearcher, millisecondsTimeout, shouldVerifyOnExit ? VerifyReader : (Action) null);
        }

        internal DisposableIndexWriter<T> GetIndexWriter(LockableIndexSearcher indexSearcher, int millisecondsTimeout, Action onExit)
        {
            return new DisposableIndexWriter<T>(indexSearcher.GetWriteLock(millisecondsTimeout), indexSearcher, _analyzer, onExit);
        }

        public T GetRecord(int doc)
        {
            using (var searcher = GetIndexSearcher())
            {
                return searcher.GetRecord(doc);
            }
        }

        protected void CheckOpen()
        {
            if (!IsOpen)
                throw new Exception("Index is closed.");
        }
    }
}
