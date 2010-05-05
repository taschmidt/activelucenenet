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
using Lucene.Net.Documents;

namespace ActiveLucene.Net.FieldHandler
{
    public abstract class FieldHandlerContextBase<T> : IFieldHandlerContext<T>
    {
        protected Field Field;

        public abstract T StringToValue(string value);
        public abstract string ValueToString(T value);

        protected readonly FieldHandlerConfiguration Configuration;

        protected FieldHandlerContextBase(FieldHandlerConfiguration configuration)
        {
            Configuration = configuration;
            Field = new Field(Configuration.Name, "", Configuration.Store, Configuration.Index);
        }

        public T GetValue(Document document)
        {
            return IfNotNull(document.Get(Configuration.Name), StringToValue);
        }

        public void SetFields(Document document, T value)
        {
            if(value is ValueType || value != null)
                document.Add(Field.Set(ValueToString(value)));
        }

        protected T IfNotNull(string str, Func<string, T> stringToValue)
        {
            return !String.IsNullOrEmpty(str) ? stringToValue(str) : default(T);
        }
    }
}
