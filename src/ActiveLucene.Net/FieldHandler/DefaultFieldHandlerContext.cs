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

namespace ActiveLucene.Net.FieldHandler
{
    public class DefaultFieldHandlerContext<T> : FieldHandlerContextBase<T>
    {
        public DefaultFieldHandlerContext(FieldHandlerConfiguration configuration) : base(configuration)
        {}

        public override T StringToValue(string value)
        {
            var type = typeof(T);
            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                type = nullableType;

            return value == null ? default(T) : (T) Convert.ChangeType(value, type);
        }

        public override string ValueToString(T value)
        {
            if (value is ValueType)
                return value.ToString();

            return value != null ? value.ToString() : null;
        }
    }
}
