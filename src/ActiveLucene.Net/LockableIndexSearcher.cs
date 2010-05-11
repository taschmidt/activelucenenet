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
using System.Threading;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace ActiveLucene.Net
{
    public class LockableIndexSearcher : IndexSearcher
    {
        private readonly ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public LockableIndexSearcher(string path, bool readOnly)
            : base(FSDirectory.Open(new DirectoryInfo(path)), readOnly)
        {
        }

        public LockableIndexSearcher(Directory directory, bool readOnly)
            : base(directory, readOnly)
        {}

        public IDisposable GetReadLock()
        {
            return new ReadLock(_readWriteLock);
        }

        public IDisposable GetReadLock(int millisecondsTimeout)
        {
            return new ReadLock(_readWriteLock, millisecondsTimeout);
        }

        public IDisposable GetWriteLock()
        {
            return new WriteLock(_readWriteLock);
        }

        public IDisposable GetWriteLock(int millisecondsTimeout)
        {
            return new WriteLock(_readWriteLock, millisecondsTimeout);
        }

        internal class ReadLock : IDisposable
        {
            private ReaderWriterLockSlim _readWriteLock;

            internal ReadLock(ReaderWriterLockSlim readWriteLock)
            {
                readWriteLock.EnterUpgradeableReadLock();
                _readWriteLock = readWriteLock;
            }

            internal ReadLock(ReaderWriterLockSlim readWriteLock, int millisecondsTimeout)
            {
                if(!readWriteLock.TryEnterUpgradeableReadLock(millisecondsTimeout))
                    throw new TimeoutException("Timed out obtaining read lock.");
                _readWriteLock = readWriteLock;
            }

            ~ReadLock()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (_readWriteLock != null)
                {
                    _readWriteLock.ExitUpgradeableReadLock();
                    _readWriteLock = null;
                }
            }
        }

        internal class WriteLock : IDisposable
        {
            private ReaderWriterLockSlim _readWriteLock;

            internal WriteLock(ReaderWriterLockSlim readWriteLock)
            {
                readWriteLock.EnterWriteLock();
                _readWriteLock = readWriteLock;
            }

            internal WriteLock(ReaderWriterLockSlim readWriteLock, int millisecondsTimeout)
            {
                if (!readWriteLock.TryEnterWriteLock(millisecondsTimeout))
                    throw new TimeoutException("Timed out obtaining write lock.");
                _readWriteLock = readWriteLock;
            }

            ~WriteLock()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (_readWriteLock != null)
                {
                    _readWriteLock.ExitWriteLock();
                    _readWriteLock = null;
                }
            }
        }
    }
}