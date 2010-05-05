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
using Lucene.Net.Documents;
using NUnit.Framework;

namespace ActiveLucene.Net.Tests
{
    [TestFixture]
    public class PerformanceTests
    {
        [Test]
        public void LuceneMediatorPerfTest()
        {
            PerfTest(LuceneMediator<TestRecord>.ToDocument, LuceneMediator<TestRecord>.ToRecord);
        }

        [Test, Ignore("No need to run the slow reflection one every time.")]
        public void ReflectionPropertyInfoPerfTest()
        {
            var dataProp = typeof (TestRecord).GetProperty("Data");
            var data2Prop = typeof (TestRecord).GetProperty("Data2");
            var dataField = new Field("data", "", Field.Store.YES, Field.Index.NO);
            var data2Field = new Field("data2", "", Field.Store.YES, Field.Index.NO);

            PerfTest((doc, rec) =>
                         {
                             doc.GetFields().Clear();

                             dataField.SetValue((string) dataProp.GetValue(rec, null));
                             doc.Add(dataField);

                             data2Field.SetValue((string) data2Prop.GetValue(rec, null));
                             doc.Add(data2Field);
                         },
                     doc =>
                         {
                             var obj = new TestRecord();
                             dataProp.SetValue(obj, dataField.StringValue(), null);
                             data2Prop.SetValue(obj, data2Field.StringValue(), null);
                             return obj;
                         });
        }

        private static void PerfTest(Action<Document, TestRecord> setProc, Func<Document, TestRecord> getProc)
        {
            var rec = new TestRecord
            {
                Data = "foo",
                Data2 = "bar"
            };
            var doc = new Document();

            var swTotal = Stopwatch.StartNew();

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 1000000; i++)
            {
                setProc(doc, rec);
            }
            Trace.WriteLine(sw.Elapsed, "Set");

            sw = Stopwatch.StartNew();
            for (var i = 0; i < 1000000; i++)
            {
                getProc(doc);
            }
            Trace.WriteLine(sw.Elapsed, "Get");

            Trace.WriteLine(swTotal.Elapsed, "Total");
        }
    }
}
