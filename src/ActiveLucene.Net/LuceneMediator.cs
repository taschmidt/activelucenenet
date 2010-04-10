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

using ActiveLucene.Net.FieldHandler;
using Lucene.Net.Documents;

namespace ActiveLucene.Net
{
    public static class LuceneMediator<T> where T : class
    {
        private static readonly IFieldHandler<T> _fieldHandler = FieldHandlerGenerator<T>.Create();

        public static T DocumentToRecord(Document doc)
        {
            return _fieldHandler.DocumentToRecord(doc);
        }

        public static Document RecordToDocument(T record)
        {
            var doc = new Document();
            _fieldHandler.RecordToDocument(doc, record);
            return doc;
        }

        public static void RecordToDocument(Document doc, T record)
        {
            _fieldHandler.RecordToDocument(doc, record);
        }
    }
}