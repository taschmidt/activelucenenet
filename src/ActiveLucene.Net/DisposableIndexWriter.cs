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
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace ActiveLucene.Net
{
    public class DisposableIndexWriter : IndexWriter, IDisposable
    {
        private readonly Action _onExit;
        private IDisposable _writeLock;

        internal DisposableIndexWriter(IDisposable writeLock, LockableIndexSearcher indexSearcher, Analyzer analyzer, Action onExit)
            : base(indexSearcher.GetIndexReader().Directory(), analyzer, MaxFieldLength.LIMITED)
        {
            _onExit = onExit;
            _writeLock = writeLock;
        }

        ~DisposableIndexWriter()
        {
            Dispose();
        }

        public new void Dispose()
        {
            if (_writeLock != null)
            {
                Close();

                if (_onExit != null)
                    _onExit();

                _writeLock.Dispose();
                _writeLock = null;
            }
        }

        public void AddRecord<T>(T record) where T : class
        {
            base.AddDocument(LuceneMediator<T>.ToDocument(record));
        }

        public void AddRecord<T>(T record, Analyzer analyzer) where T : class
        {
            base.AddDocument(LuceneMediator<T>.ToDocument(record), analyzer);
        }
    }

    public class DisposableIndexWriter<T> : DisposableIndexWriter where T : class
    {
        internal DisposableIndexWriter(IDisposable writeLock, LockableIndexSearcher indexSearcher, Analyzer analyzer, Action onExit)
            : base(writeLock, indexSearcher, analyzer, onExit)
        {
        }

        public void AddRecord(T record)
        {
            base.AddDocument(LuceneMediator<T>.ToDocument(record));
        }

        public void AddRecord(T record, Analyzer analyzer)
        {
            base.AddDocument(LuceneMediator<T>.ToDocument(record), analyzer);
        }
    }
}