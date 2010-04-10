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
using System.Linq;
using Lucene.Net.Documents;
using NUnit.Framework;

namespace ActiveLucene.Net.Tests
{
    [TestFixture]
    public class LuceneMediatorTests
    {
        [Test]
        public void CanSetFields()
        {
            var obj = new TestRecord { Data = "foo", Data2 = "bar" };
            var doc = LuceneMediator<TestRecord>.RecordToDocument(obj);

            Assert.AreEqual(doc.GetFields().Count, 2);
            Assert.AreEqual(doc.Get("data"), "foo");
            Assert.AreEqual(doc.Get("data2"), "bar");
        }

        [Test]
        public void CanGetFields()
        {
            var obj = new TestRecord { Data = "foo", Data2 = "bar" };
            var doc = LuceneMediator<TestRecord>.RecordToDocument(obj);

            var obj2 = LuceneMediator<TestRecord>.DocumentToRecord(doc);
            Assert.AreEqual(obj2.Data, "foo");
            Assert.AreEqual(obj2.Data2, "bar");
        }

        [Test]
        public void CanSetNumericFields()
        {
            var obj = new NumericRecord
                          {
                              IntField = 1,
                              LongField = 2,
                              FloatField = 3.5f,
                              DoubleField = 4.5
                          };
            var doc = LuceneMediator<NumericRecord>.RecordToDocument(obj);

            Assert.AreEqual(doc.GetFields().Count, 4);

            Assert.AreEqual(doc.Get("int"), "1");
            Assert.IsFalse(doc.GetFieldable("int").IsIndexed());
            Assert.AreEqual(doc.Get("long"), "2");
            Assert.IsTrue(doc.GetFieldable("long").IsIndexed());
            Assert.AreEqual(doc.Get("float"), "3.5");
            Assert.IsFalse(doc.GetFieldable("float").IsIndexed());
            Assert.AreEqual(doc.Get("double"), "4.5");
            Assert.IsFalse(doc.GetFieldable("double").IsIndexed());
        }

        [Test]
        public void CanGetNumericFields()
        {
            var obj = new NumericRecord
                          {
                              IntField = 1,
                              LongField = 2,
                              FloatField = 3.5f,
                              DoubleField = 4.5
                          };
            var doc = LuceneMediator<NumericRecord>.RecordToDocument(obj);

            var obj2 = LuceneMediator<NumericRecord>.DocumentToRecord(doc);
            Assert.AreEqual(obj2.IntField, 1);
            Assert.AreEqual(obj2.LongField, 2);
            Assert.AreEqual(obj2.FloatField, 3.5f);
            Assert.AreEqual(obj2.DoubleField, 4.5);
        }

        [Test]
        public void CanSetDateField()
        {
            var dt = DateTime.Now;
            var obj = new DateRecord { Date = dt };
            var doc = LuceneMediator<DateRecord>.RecordToDocument(obj);

            Assert.AreEqual(doc.GetFields().Count, 3);
            Assert.AreEqual(doc.Get("date"), DateTools.DateToString(dt, DateTools.Resolution.SECOND));
        }

        [Test]
        public void CanGetDateField()
        {
            var dt = DateTime.Now;
            var obj = new DateRecord { Date = dt };
            var doc = LuceneMediator<DateRecord>.RecordToDocument(obj);

            var obj2 = LuceneMediator<DateRecord>.DocumentToRecord(doc);
            Assert.AreEqual(DateTools.Round(dt, DateTools.Resolution.SECOND), obj2.Date);
        }

        [Test]
        public void CheckIfDateResolutionWorks()
        {
            var dt = DateTime.Now;
            var obj = new DateRecord { DateDay = dt };
            var doc = LuceneMediator<DateRecord>.RecordToDocument(obj);

            var obj2 = LuceneMediator<DateRecord>.DocumentToRecord(doc);
            Assert.AreEqual(DateTools.Round(dt, DateTools.Resolution.DAY), obj2.DateDay);
        }

        [Test]
        public void CheckDateTypes()
        {
            var dt = DateTime.Now;
            var dtUtc = DateTime.UtcNow;
            var obj = new DateRecord
                          {
                              Date = dt,
                              UtcDate = dtUtc
                          };
            var doc = LuceneMediator<DateRecord>.RecordToDocument(obj);

            var obj2 = LuceneMediator<DateRecord>.DocumentToRecord(doc);

            // these pass although both dates come out as DateTimeKind.Unspecified
            // since DateTime.Equals doesn't look at the Kind.
            Assert.AreEqual(DateTools.Round(dt, DateTools.Resolution.SECOND), obj2.Date);
            Assert.AreEqual(DateTools.Round(dtUtc, DateTools.Resolution.SECOND), obj2.UtcDate);
        }

        [Test]
        public void CanSetCollectionFields()
        {
            var obj = new CollectionRecord
                          {
                              NumberList = new[] { 1, 2, 3 }.ToList(),
                              StringArray = new[] { "one", "two", "three" }
                          };
            var doc = LuceneMediator<CollectionRecord>.RecordToDocument(obj);

            var fields = doc.GetValues("number");
            obj.NumberList.ForEach(x => Assert.Contains(x.ToString(), fields));

            fields = doc.GetValues("string");
            obj.StringArray.ToList().ForEach(x => Assert.Contains(x, fields));
        }

        [Test]
        public void CanGetCollectionFields()
        {
            var obj = new CollectionRecord { NumberList = new[] { 1, 2, 3 }.ToList() };
            var doc = LuceneMediator<CollectionRecord>.RecordToDocument(obj);

            var obj2 = LuceneMediator<CollectionRecord>.DocumentToRecord(doc);
            Assert.Contains(1, obj2.NumberList);
            Assert.Contains(2, obj2.NumberList);
            Assert.Contains(3, obj2.NumberList);
        }
    }
}