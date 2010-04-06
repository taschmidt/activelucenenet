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

using Lucene.Net.Documents;

namespace ActiveLucene.Net
{
    public static class LuceneMediator<T> where T : class
    {
        private static readonly ILuceneFieldHandler<T> _fieldHandler = LuceneFieldHandlerGenerator<T>.Create();

        public static void Set(Document doc, T record)
        {
            _fieldHandler.Set(doc, record);
        }

        public static T Get(Document doc)
        {
            return _fieldHandler.Get(doc);
        }
    }
}