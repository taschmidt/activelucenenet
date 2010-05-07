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
    public class DisposableIndexWriter<T> : IndexWriter, IDisposable where T : class
    {
        private readonly Action _onExit;
        private IDisposable _writeLock;
        private readonly Document _document = new Document();

        internal DisposableIndexWriter(LockableIndexSearcher indexSearcher, Analyzer analyzer, Action onExit)
            : base(indexSearcher.GetIndexReader().Directory(), analyzer, MaxFieldLength.LIMITED)
        {
            _onExit = onExit;
            _writeLock = indexSearcher.GetWriteLock();
        }

        internal DisposableIndexWriter(Directory directory, Analyzer analyzer, bool create)
            : base(directory, analyzer, create, MaxFieldLength.LIMITED)
        { }

        ~DisposableIndexWriter()
        {
            Dispose();
        }

        public void Dispose()
        {
            Close();

            if (_onExit != null)
                _onExit();

            if (_writeLock != null)
            {
                _writeLock.Dispose();
                _writeLock = null;
            }
        }

        public void AddRecord(T record)
        {
            LuceneMediator<T>.ToDocument(_document, record);
            base.AddDocument(_document);
        }

        public void AddRecord(T record, Analyzer analyzer)
        {
            LuceneMediator<T>.ToDocument(_document, record);
            base.AddDocument(_document, analyzer);
        }
    }
}