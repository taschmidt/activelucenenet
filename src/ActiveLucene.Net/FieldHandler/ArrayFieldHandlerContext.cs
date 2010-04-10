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

namespace ActiveLucene.Net.FieldHandler
{
    public class ArrayFieldHandlerContext<T, TItem> : IFieldHandlerContext<T> where T : class, IList
    {
        private FieldHandlerContextBase<TItem> _itemFieldHandlerContext;

        protected FieldHandlerConfiguration Configuration { get; private set; }

        public void Init(FieldHandlerConfiguration configuration)
        {
            if(!typeof(T).IsArray)
                throw new Exception("ArrayFieldHandlerContext used on a type that isn't an array.");

            Configuration = configuration;
            _itemFieldHandlerContext = (FieldHandlerContextBase<TItem>)Activator.CreateInstance(
                FieldHandlerHelpers.GetFieldHandlerContextType(typeof(TItem)));
        }

        public T GetValue(Document document)
        {
            var values = document.GetValues(Configuration.Name);
            var array = Array.CreateInstance(typeof (TItem), values.Length);
            values.Aggregate(0, (i, value) =>
                                    {
                                        array.SetValue(_itemFieldHandlerContext.StringToValue(value), i);
                                        return ++i;
                                    });

            return array as T;
        }

        public void SetFields(Document document, T value)
        {
            if (value != null)
            {
                foreach (var item in value)
                {
                    document.Add(new Field(Configuration.Name,
                                           _itemFieldHandlerContext.ValueToString((TItem)item),
                                           Configuration.Store, Configuration.Index));
                }
            }
        }
    }
}
