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
using System.Collections;
using System.Linq;
using Lucene.Net.Documents;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace ActiveLucene.Net.Tests
{
    [TestFixture]
    public class LuceneMediatorTests
    {
        [Test]
        public void CheckStringFields()
        {
            var obj = new TestRecord { Data = "foo", Data2 = "bar" };
            var doc = LuceneMediator<TestRecord>.ToDocument(obj);

            var obj2 = LuceneMediator<TestRecord>.ToRecord(doc);
            Assert.AreEqual(obj2.Data, "foo");
            Assert.AreEqual(obj2.Data2, "bar");
        }

        [Test]
        public void CheckNumericFields()
        {
            var obj = new NumericRecord
                          {
                              IntField = 1,
                              LongField = 2,
                              FloatField = 3.5f,
                              DoubleField = 4.5
                          };
            var doc = LuceneMediator<NumericRecord>.ToDocument(obj);

            var obj2 = LuceneMediator<NumericRecord>.ToRecord(doc);
            Assert.AreEqual(obj2.IntField, 1);
            Assert.AreEqual(obj2.LongField, 2);
            Assert.AreEqual(obj2.FloatField, 3.5f);
            Assert.AreEqual(obj2.DoubleField, 4.5);
        }

        [Test]
        public void CheckDateField()
        {
            var dt = DateTime.Now;
            var obj = new DateRecord { Date = dt };
            var doc = LuceneMediator<DateRecord>.ToDocument(obj);

            var obj2 = LuceneMediator<DateRecord>.ToRecord(doc);
            Assert.AreEqual(DateTools.Round(dt, DateTools.Resolution.SECOND), obj2.Date);
        }

        [Test]
        public void CheckIfDateResolutionWorks()
        {
            var dt = DateTime.Now;
            var obj = new DateRecord { DateDay = dt };
            var doc = LuceneMediator<DateRecord>.ToDocument(obj);

            var obj2 = LuceneMediator<DateRecord>.ToRecord(doc);
            Assert.AreEqual(DateTools.Round(dt, DateTools.Resolution.DAY), obj2.DateDay);
        }

        [Test]
        public void CheckDateKinds()
        {
            var dt = DateTime.Now;
            var dtUtc = DateTime.UtcNow;
            var obj = new DateRecord
                          {
                              Date = dt,
                              UtcDate = dtUtc
                          };
            var doc = LuceneMediator<DateRecord>.ToDocument(obj);

            var obj2 = LuceneMediator<DateRecord>.ToRecord(doc);

            // these pass although both dates come out as DateTimeKind.Unspecified
            // since DateTime.Equals doesn't look at the Kind.
            Assert.AreEqual(DateTools.Round(dt, DateTools.Resolution.SECOND), obj2.Date);
            Assert.AreEqual(DateTools.Round(dtUtc, DateTools.Resolution.SECOND), obj2.UtcDate);
        }

        [Test]
        public void CheckCollectionFields()
        {
            var obj = new CollectionRecord
                          {
                              NumberList = new[] {1, 2, 3}.ToList(),
                              StringArray = new[] {"one", "two", "three"},
                              ArrayList = new ArrayList(new[] {"four", "five", "six"})
                          };
            var doc = LuceneMediator<CollectionRecord>.ToDocument(obj);

            var obj2 = LuceneMediator<CollectionRecord>.ToRecord(doc);
            Assert.That(obj2.NumberList, new CollectionEquivalentConstraint(obj.NumberList));
            Assert.That(obj2.StringArray, new CollectionEquivalentConstraint(obj.StringArray));
            Assert.That(obj2.ArrayList, new CollectionEquivalentConstraint(obj.ArrayList));
        }

        [Test]
        public void CanHandleNulls()
        {
            var obj = new TestRecord();
            Assert.That(delegate { LuceneMediator<TestRecord>.ToDocument(obj); }, new ThrowsNothingConstraint());

            var obj2 = new CollectionRecord();
            Assert.That(delegate { LuceneMediator<CollectionRecord>.ToDocument(obj2); }, new ThrowsNothingConstraint());

            // this shouldn't even matter since they're all value types but might as well test it
            var obj3 = new NumericRecord();
            Assert.That(delegate { LuceneMediator<NumericRecord>.ToDocument(obj3); }, new ThrowsNothingConstraint());
        }
    }
}