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
using ActiveLucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Version = Lucene.Net.Util.Version;

namespace IndexManagerTester
{
    class Program
    {
        private static IndexManager<TestRecord> _indexManager;
        private static readonly Analyzer _analyzer = new StandardAnalyzer(Version.LUCENE_CURRENT);
        private static bool _done;
        private static readonly Random _random = new Random();

        static void Main()
        {
            var indexPath = Path.Combine(Environment.CurrentDirectory, "index");

            Console.WriteLine("Creating IndexManager...");
            _indexManager = new IndexManager<TestRecord>(indexPath, _analyzer, false);
            _indexManager.Open(true);

            var fxn = new Action(WriterProc);
            fxn.BeginInvoke(null, null);

            for(var i = 0; i < 10; i++)
            {
                fxn = new Action(SearcherProc);
                fxn.BeginInvoke(null, null);
            }

            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs consoleCancelEventArgs)
                                          {
                                              Console.WriteLine("\nCancel event received...");
                                              consoleCancelEventArgs.Cancel = true;
                                              _done = true;
                                          };

            while(!_done)
            {
                Thread.Sleep(1000);
            }

            using(var writer = _indexManager.GetIndexWriter())
            {
                Console.WriteLine("\nClosing IndexManager with {0} documents...", writer.MaxDoc());
                _indexManager.Close();
            }

            Console.WriteLine("Done!");
        }

        static string GetRandomValue()
        {
            return _random.Next(0, 10).ToString();
        }

        static void SearcherProc()
        {
            var parser = new QueryParser(Version.LUCENE_CURRENT, "test-field", _analyzer);

            while (!_done)
            {
                try
                {
                    using (var searcher = _indexManager.GetIndexSearcher())
                    {
                        var value = GetRandomValue();
                        var query = parser.Parse(value);
                        var topDocs = searcher.Search(query, 10);
                        Console.Write("{0}.. ", topDocs.totalHits);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Exception in SearcherProc. {0}", ex);
                }

                Thread.Sleep(_random.Next(50, 150));
            }
        }

        static void WriterProc()
        {
            while (!_done)
            {
                using (var writer = _indexManager.GetIndexWriter())
                {
                    var count = _random.Next(1, 25);

                    for (var i = 0; i < count; i++)
                    {
                        writer.AddRecord(new TestRecord {TestField = GetRandomValue()});
                    }

                    Console.WriteLine("\nAdded {0} documents...", count);

                    if(_random.Next(1, 10) == 5)
                    {
                        Console.WriteLine("Optimizing...");
                        writer.Optimize();
                    }
                }

                Thread.Sleep(_random.Next(500, 1500));
            }
        }
    }

    public class TestRecord
    {
        [LuceneField("test-field", StorageBehavior.Store, IndexBehavior.DoNotAnalyze)]
        public string TestField { get; set; }
    }
}
