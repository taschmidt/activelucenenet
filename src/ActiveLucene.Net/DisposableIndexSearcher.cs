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
using Lucene.Net.Search;

namespace ActiveLucene.Net
{
    public class DisposableIndexSearcher<T> : IndexSearcher, IDisposable where T : class
    {
        private IDisposable _readLock;

        internal DisposableIndexSearcher(LockableIndexSearcher indexSearcher)
            : base(indexSearcher.GetIndexReader())
        {
            _readLock = indexSearcher.GetReadLock();
        }

        internal DisposableIndexSearcher(LockableIndexSearcher indexSearcher, int millisecondsTimeout)
            : base(indexSearcher.GetIndexReader())
        {
            _readLock = indexSearcher.GetReadLock(millisecondsTimeout);
        }

        ~DisposableIndexSearcher()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_readLock != null)
            {
                Close();
                _readLock.Dispose();
                _readLock = null;
            }
        }
    }
}