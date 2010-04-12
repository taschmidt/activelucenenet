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
using Lucene.Net.Documents;

namespace ActiveLucene.Net.FieldHandler
{
    public class ListFieldHandlerContext<T, TItem> : IFieldHandlerContext<T> where T : class, IList
    {
        private readonly FieldHandlerContextBase<TItem> _itemFieldHandlerContext;

        protected FieldHandlerConfiguration Configuration { get; private set; }

        public ListFieldHandlerContext(FieldHandlerConfiguration configuration)
        {
            Configuration = configuration;
            _itemFieldHandlerContext = (FieldHandlerContextBase<TItem>)Activator.CreateInstance(
                FieldHandlerHelpers.GetFieldHandlerContextType(typeof(TItem)), Configuration);
        }

        public T GetValue(Document document)
        {
            var list = (IList)Activator.CreateInstance(typeof(T));
            foreach (var value in document.GetValues(Configuration.Name))
            {
                list.Add(_itemFieldHandlerContext.StringToValue(value));
            }

            return (T)list;
        }

        public void SetFields(Document document, T value)
        {
            if (value != null)
            {
                foreach (var item in value)
                {
                    document.Add(new Field(Configuration.Name,
                                           _itemFieldHandlerContext.ValueToString((TItem) item),
                                           Configuration.Store, Configuration.Index));
                }
            }
        }
    }
}
