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

        event Action<IndexWriter, object> OnRebuildRepository;
        event Action<IndexSearcher> OnWarmUpIndex;

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
    }

    public class IndexManager : IIndexManager
    {
        protected readonly object _rebuildLock = new object();
        protected readonly object _maintenanceLock = new object();

        protected readonly string _basePath;
        protected readonly Analyzer _analyzer;
        protected readonly bool _readOnly;
        protected LockableIndexSearcher _indexSearcher;

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

        public void BeginRebuildRepository(object state)
        {
            var dlg = new Action<object>(RebuildRepository);
            dlg.BeginInvoke(state, EndRebuildRepository, dlg);
        }

        protected static void EndRebuildRepository(IAsyncResult ar)
        {
            var dlg = ar.AsyncState as Action<object>;
            dlg.EndInvoke(ar);
        }

        public void RebuildRepository(object state)
        {
            if(OnRebuildRepository == null)
                return;

            if (!Monitor.TryEnter(_rebuildLock))
                return;

            try
            {
                var buildingPath = Path.Combine(_basePath, "building" + Guid.NewGuid().ToString("n"));
                var indexWriter = new IndexWriter(FSDirectory.Open(new DirectoryInfo(buildingPath)),
                                                  _analyzer, true, IndexWriter.MaxFieldLength.LIMITED);

                try
                {
                    OnRebuildRepository(indexWriter, state);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    indexWriter.Close();
                    CleanUpDirectories();
                    return;
                }

                indexWriter.Optimize();
                indexWriter.Close();
                Directory.Move(buildingPath, NextHighestNumberedFolder());
                OpenBestRepository();
            }
            finally
            {
                Monitor.Exit(_rebuildLock);
            }
        }

        public event Action<IndexWriter, object> OnRebuildRepository;
        public event Action<IndexSearcher> OnWarmUpIndex;

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
                lock (_maintenanceLock)
                {
                    using (_indexSearcher.GetWriteLock())
                    {
                        _indexSearcher.Close();
                    }

                    _indexSearcher = null;
                }
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
                    using(var indexSearcher = new DisposableIndexSearcher(newIndexSearcher))
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

        public DisposableIndexSearcher GetIndexSearcher()
        {
            using (_indexSearcher.GetReadLock())
            {
                CheckOpen();
                return new DisposableIndexSearcher(_indexSearcher, 5000);
            }
        }

        public DisposableIndexWriter GetIndexWriter()
        {
            return GetIndexWriter(true);
        }

        public DisposableIndexWriter GetIndexWriter(bool shouldVerifyOnExit)
        {
            using (_indexSearcher.GetWriteLock())
            {
                CheckOpen();
                return new DisposableIndexWriter(_indexSearcher, _analyzer, shouldVerifyOnExit ? VerifyReader : (Action) null);
            }
        }

        protected void CheckOpen()
        {
            if (!IsOpen)
                throw new Exception("Index is closed.");
        }
    }

    public interface IIndexManager<T> : IIndexManager
    {
        T GetRecord(int doc);
    }

    public class IndexManager<T> : IndexManager, IIndexManager<T> where T : class
    {
        public IndexManager(string basePath, Analyzer analyzer)
            : base(basePath, analyzer)
        {}

        public IndexManager(string basePath, Analyzer analyzer, bool readOnly)
            : base(basePath, analyzer, readOnly)
        {}

        public T GetRecord(int doc)
        {
            using(var searcher = GetIndexSearcher())
            {
                return LuceneMediator<T>.Get(searcher.Doc(doc));
            }
        }

        public new DisposableIndexWriter<T> GetIndexWriter()
        {
            return GetIndexWriter(true);
        }

        public new DisposableIndexWriter<T> GetIndexWriter(bool shouldVerifyOnExit)
        {
            using (_indexSearcher.GetWriteLock())
            {
                CheckOpen();
                return new DisposableIndexWriter<T>(_indexSearcher, _analyzer, shouldVerifyOnExit ? VerifyReader : (Action)null);
            }
        }
    }
}