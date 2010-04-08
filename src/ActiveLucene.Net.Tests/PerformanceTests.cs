using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
            PerfTest(LuceneMediator<TestRecord>.Set, LuceneMediator<TestRecord>.Get);
        }

        [Test]
        public void PropertyInfoPerfTest()
        {
            var dataProp = typeof(TestRecord).GetProperty("Data");
            var data2Prop = typeof(TestRecord).GetProperty("Data2");
            var dataField = new Field("data", "", Field.Store.YES, Field.Index.NO);
            var data2Field = new Field("data2", "", Field.Store.YES, Field.Index.NO);

            PerfTest((doc, rec) =>
            {
                doc.GetFields().Clear();

                dataField.SetValue((string)dataProp.GetValue(rec, null));
                doc.Add(dataField);

                data2Field.SetValue((string)data2Prop.GetValue(rec, null));
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
