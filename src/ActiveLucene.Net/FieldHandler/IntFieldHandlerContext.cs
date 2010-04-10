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

namespace ActiveLucene.Net.FieldHandler
{
    public class IntFieldHandlerContext : FieldHandlerContextBase<int>
    {
        private NumericField _field;

        public override void Init()
        {
            _field = new NumericField(Configuration.Name, Configuration.Store, Configuration.Index != Field.Index.NO);
        }

        public override int GetValue(Document document)
        {
            return IfNotNull(document.Get(Configuration.Name), int.Parse);
        }

        public override void SetFields(Document document, int value)
        {
            document.Add(_field.SetIntValue(value));
        }
    }
}
