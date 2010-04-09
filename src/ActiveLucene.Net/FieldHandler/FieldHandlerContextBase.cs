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
    public interface IFieldHandlerContext
    {
    }

    public interface IFieldHandlerContext<T> : IFieldHandlerContext
    {
        T GetValue(Document document);
        void SetFields(Document document, T value);
    }

    public abstract class FieldHandlerContextBase<T> : IFieldHandlerContext<T>
    {
        public abstract T GetValue(Document document);
        public abstract void SetFields(Document document, T value);

        public FieldHandlerConfiguration Configuration { get; private set; }

        protected FieldHandlerContextBase(FieldHandlerConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}
