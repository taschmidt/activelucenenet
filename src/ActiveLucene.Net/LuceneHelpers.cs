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
using Lucene.Net.Documents;

namespace ActiveLucene.Net
{
    public static class LuceneHelpers
    {
        public static string Parse(this Analyzer analyzer, string s)
        {
            var reader = new StringReader(s);
            var stream = analyzer.TokenStream(null, reader);
            if (stream == null)
                return s;

            var token = stream.Next();
            return token == null ? s : token.TermText();
        }

        public static int GetInt32(this Document doc, string name)
        {
            return GetNullableInt32(doc, name).GetValueOrDefault();
        }

        public static int? GetNullableInt32(this Document doc, string name)
        {
            try
            {
                var str = doc.Get(name);
                return Int32.Parse(str);
            }
            catch
            {
                return null;
            }
        }

        public static Int64 GetInt64(this Document doc, string name)
        {
            return GetNullableInt64(doc, name).GetValueOrDefault();
        }

        public static Int64? GetNullableInt64(this Document doc, string name)
        {
            try
            {
                var str = doc.Get(name);
                return Int64.Parse(str);
            }
            catch
            {
                return null;
            }
        }

        public static DateTime GetDateTime(this Document doc, string name)
        {
            return GetNullableDateTime(doc, name).GetValueOrDefault();
        }

        public static DateTime? GetNullableDateTime(this Document doc, string name)
        {
            try
            {
                var str = doc.Get(name);
                return DateTools.StringToDate(str);
            }
            catch
            {
                return null;
            }
        }

        public static DateTime GetUtcDateTime(this Document doc, string name)
        {
            return GetNullableUtcDateTime(doc, name).GetValueOrDefault();
        }

        public static DateTime? GetNullableUtcDateTime(this Document doc, string name)
        {
            var dt = GetNullableDateTime(doc, name);
            if (!dt.HasValue)
                return null;

            return new DateTime(dt.Value.Ticks, DateTimeKind.Utc);
        }
    }
}