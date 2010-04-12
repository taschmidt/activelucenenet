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
using System.Collections.Generic;

namespace ActiveLucene.Net.Tests
{
    public class TestRecord
    {
        [LuceneField("data")]
        public string Data { get; set; }

        [LuceneField("data2")]
        public string Data2 { get; set; }

    }

    public class NumericRecord
    {
        [LuceneField("int")]
        public int IntField { get; set; }

        [LuceneField("long", IndexBehavior = IndexBehavior.DoNotAnalyze)]
        public long LongField { get; set; }

        [LuceneField("float")]
        public float FloatField { get; set; }

        [LuceneField("double")]
        public double DoubleField { get; set; }
    }

    public class DateRecord
    {
        [LuceneField("date")]
        public DateTime Date { get; set; }

        [LuceneField("utcdate")]
        public DateTime UtcDate { get; set; }

        [LuceneField("date-day", DateResolution = DateResolution.Day)]
        public DateTime DateDay { get; set; }
    }

    public class CollectionRecord
    {
        [LuceneField("number")]
        public List<int> NumberList { get; set; }

        [LuceneField("string")]
        public string[] StringArray { get; set; }

        [LuceneField("string2")]
        public ArrayList ArrayList { get; set; }
    }
}