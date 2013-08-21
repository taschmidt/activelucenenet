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
            Assert.That(obj2.Data, Is.EqualTo("foo"));
            Assert.That(obj2.Data2, Is.EqualTo("bar"));
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
            Assert.That(obj2.IntField, Is.EqualTo(1));
            Assert.That(obj2.LongField, Is.EqualTo(2));
            Assert.That(obj2.FloatField, Is.EqualTo(3.5f));
            Assert.That(obj2.DoubleField, Is.EqualTo(4.5));
        }

        [Test]
        public void CheckDateField()
        {
            var dt = DateTime.Now;
            var obj = new DateRecord { Date = dt };
            var doc = LuceneMediator<DateRecord>.ToDocument(obj);

            var obj2 = LuceneMediator<DateRecord>.ToRecord(doc);
            Assert.That(obj2.Date, Is.EqualTo(DateTools.Round(dt, DateTools.Resolution.SECOND)));
        }

        [Test]
        public void CheckIfDateResolutionWorks()
        {
            var dt = DateTime.Now;
            var obj = new DateRecord { DateDay = dt };
            var doc = LuceneMediator<DateRecord>.ToDocument(obj);

            var obj2 = LuceneMediator<DateRecord>.ToRecord(doc);
            Assert.That(obj2.DateDay, Is.EqualTo(DateTools.Round(dt, DateTools.Resolution.DAY)));
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
            Assert.That(obj2.NumberList, Is.EquivalentTo(obj.NumberList));
            Assert.That(obj2.StringArray, Is.EquivalentTo(obj.StringArray));
            Assert.That(obj2.ArrayList, Is.EquivalentTo(obj.ArrayList));
        }

        [Test]
        public void CanHandleNulls()
        {
            var obj = new TestRecord();
            Assert.That(new TestDelegate(() => LuceneMediator<TestRecord>.ToDocument(obj)), Throws.Nothing);

            var obj2 = new CollectionRecord();
            Assert.That(new TestDelegate(() => LuceneMediator<CollectionRecord>.ToDocument(obj2)), Throws.Nothing);

            // this shouldn't even matter since they're all value types but might as well test it
            var obj3 = new NumericRecord();
            Assert.That(new TestDelegate(() => LuceneMediator<NumericRecord>.ToDocument(obj3)), Throws.Nothing);
        }

        [Test]
        public void CheckBooleanFields()
        {
            var obj = new BooleanRecord {Boolean = true};
            var doc = LuceneMediator<BooleanRecord>.ToDocument(obj);
            Assert.IsTrue(LuceneMediator<BooleanRecord>.ToRecord(doc).Boolean);

            obj = new BooleanRecord { Boolean = false };
            doc = LuceneMediator<BooleanRecord>.ToDocument(obj);
            Assert.IsFalse(LuceneMediator<BooleanRecord>.ToRecord(doc).Boolean);
        }

        [Test]
        public void DontAddNullFields()
        {
            var obj = new TestRecord {Data = "foo"};
            var doc = LuceneMediator<TestRecord>.ToDocument(obj);
            Assert.That(doc.GetFields().Cast<Field>().All(f => f.Name != "data2"));
        }

        [Test]
        public void DocumentBoost()
        {
            var obj = new TestRecord {Boost = 2.0f};
            var doc = LuceneMediator<TestRecord>.ToDocument(obj);
            Assert.That(doc.Boost, Is.EqualTo(2.0f));

            var obj2 = LuceneMediator<TestRecord>.ToRecord(doc);
            Assert.That(obj2.Boost, Is.EqualTo(2.0f));
        }

        [Test]
        public void DocumentBoostWithNonFloat()
        {
            var obj = new BooleanRecord {Boost = "2"};
            var doc = LuceneMediator<BooleanRecord>.ToDocument(obj);
            Assert.That(doc.Boost, Is.EqualTo(2.0f));

            var obj2 = LuceneMediator<BooleanRecord>.ToRecord(doc);
            Assert.That(obj2.Boost, Is.EqualTo("2"));
        }

        [Test]
        public void TwoDocumentBoostsThrowException()
        {
            var obj = new TwoBoostsRecord();
            Assert.That(new TestDelegate(() => LuceneMediator<TwoBoostsRecord>.ToDocument(obj)), Throws.Exception);
        }

        [Test]
        public void CheckNullableFields()
        {
            var obj = new NullableRecord {Int = 1, Long = 2L, Float = 3.0f, Double = 4.0f};
            var doc = LuceneMediator<NullableRecord>.ToDocument(obj);

            var obj2 = LuceneMediator<NullableRecord>.ToRecord(doc);
            Assert.That(obj2.Int, Is.EqualTo(1));
            Assert.That(obj2.Long, Is.EqualTo(2L));
            Assert.That(obj2.Float, Is.EqualTo(3.0f));
            Assert.That(obj2.Double, Is.EqualTo(4.0f));
        }
    }
}