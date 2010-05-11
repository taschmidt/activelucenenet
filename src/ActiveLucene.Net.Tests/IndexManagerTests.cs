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
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace ActiveLucene.Net.Tests
{
    [TestFixture]
    public class IndexManagerTests
    {
        private readonly string _basePath = Path.Combine(Environment.CurrentDirectory, "index");
        private readonly Analyzer _analyzer = new StandardAnalyzer(Version.LUCENE_CURRENT);

        private IndexManager<object> GetOpenIndexManager()
        {
            var indexManager = new IndexManager<object>(_basePath, _analyzer, false);
            indexManager.Open();
            return indexManager;
        }

        [SetUp]
        public void Init()
        {
            if(Directory.Exists(_basePath))
                Directory.Delete(_basePath, true);
        }

        [Test]
        public void CanCreate()
        {
            var indexManager = new IndexManager<object>(_basePath, _analyzer, false);
            Assert.IsNotNull(indexManager);
        }

        [Test]
        public void CanAddDocuments()
        {
            var indexManager = GetOpenIndexManager();
            using(var indexWriter = indexManager.GetIndexWriter())
            {
                indexWriter.AddDocument(new Document());
                Assert.AreEqual(indexWriter.MaxDoc(), 1);
            }

            indexManager.Close();
        }

        [Test]
        public void CanRebuildRepository()
        {
            var indexManager = GetOpenIndexManager();
            indexManager.OnRebuildRepository += delegate(DisposableIndexWriter<object> indexWriter, object state)
                                                    {
                                                        var doc = new Document();
                                                        doc.Add(new Field("rebuilding", "yes", Field.Store.YES, Field.Index.NOT_ANALYZED));
                                                        indexWriter.AddDocument(doc);
                                                    };
            indexManager.RebuildRepository(null);
            Assert.True(indexManager.CurrentIndexPath.EndsWith("2"));

            using(var indexSearcher = indexManager.GetIndexSearcher())
            {
                Assert.AreEqual(indexSearcher.MaxDoc(), 1);
                var doc = indexSearcher.Doc(0);
                Assert.AreEqual(doc.Get("rebuilding"), "yes");
            }

            indexManager.Close();
        }

        [Test]
        public void CanAddRecords()
        {
            var indexManager = new IndexManager<TestRecord>(_basePath, _analyzer, false);
            indexManager.Open();
            using (var indexWriter = indexManager.GetIndexWriter())
            {
                indexWriter.AddRecord(new TestRecord
                                          {
                                              Data = "foo",
                                              Data2 = "bar"
                                          });
            }

            using (var indexSearcher = indexManager.GetIndexSearcher())
            {
                Assert.AreEqual(indexSearcher.MaxDoc(), 1);
                var obj = LuceneMediator<TestRecord>.ToRecord(indexSearcher.Doc(0));
                Assert.AreEqual(obj.Data, "foo");
                Assert.AreEqual(obj.Data2, "bar");
            }

            indexManager.Close();
        }

        [Test]
        public void CanRetrieveRecords()
        {
            var indexManager = new IndexManager<TestRecord>(_basePath, _analyzer, false);
            indexManager.Open();
            using (var indexWriter = indexManager.GetIndexWriter())
            {
                indexWriter.AddRecord(new TestRecord
                                          {
                                              Data = "foo",
                                              Data2 = "bar"
                                          });
            }

            using (var indexSearcher = indexManager.GetIndexSearcher())
            {
                Assert.AreEqual(indexSearcher.MaxDoc(), 1);
                var obj = indexManager.GetRecord(0);
                Assert.AreEqual(obj.Data, "foo");
                Assert.AreEqual(obj.Data2, "bar");
            }

            indexManager.Close();
        }

        [Test]
        public void CanHandleExceptionDuringRebuild()
        {
            var indexManager = new IndexManager<object>(_basePath, _analyzer, false);
            indexManager.Open();

            Assert.IsTrue(indexManager.CurrentIndexPath.EndsWith("1"));

            indexManager.OnRebuildRepository += delegate
                                                    {
                                                        throw new Exception("Test");
                                                    };
            indexManager.RebuildRepository(null);

            Assert.IsTrue(indexManager.CurrentIndexPath.EndsWith("1"));

            indexManager.Close();
        }

        [Test]
        public void CanGetIndexCount()
        {
            var indexManager = GetOpenIndexManager();
            using(var writer = indexManager.GetIndexWriter())
            {
                writer.AddDocument(new Document());
            }

            var indexCount = indexManager.GetIndexCount();
            Assert.AreEqual(indexCount, 1);

            indexManager.Close();
        }

        [Test]
        public void CanGetIndexSize()
        {
            var indexManager = GetOpenIndexManager();
            var indexSize = indexManager.GetIndexSize();
            Assert.IsTrue(indexSize > 0);

            indexManager.Close();
        }

        [Test]
        public void TestReadLockTimeout()
        {
            var indexManager = GetOpenIndexManager();
            
            using (indexManager.GetIndexWriter())
            {
                Action dlg = () => indexManager.GetIndexSearcher(100);
                var ar = dlg.BeginInvoke(null, null);
                Assert.That(() => dlg.EndInvoke(ar), Throws.TypeOf<TimeoutException>());
            }
        }

        [Test]
        public void TestWriteLockTimeout()
        {
            var indexManager = GetOpenIndexManager();

            using (indexManager.GetIndexSearcher())
            {
                Action dlg = () => indexManager.GetIndexWriter(100);
                var ar = dlg.BeginInvoke(null, null);
                Assert.That(() => dlg.EndInvoke(ar), Throws.TypeOf<TimeoutException>());
            }
        }
    }
}