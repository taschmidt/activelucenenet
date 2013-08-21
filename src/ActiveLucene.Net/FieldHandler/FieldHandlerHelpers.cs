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
using Lucene.Net.Documents;

namespace ActiveLucene.Net.FieldHandler
{
    public static class FieldHandlerHelpers
    {
        public static Type GetFieldHandlerContextType(Type type)
        {
            if (type == typeof(string))
                return typeof(StringFieldHandlerContext);
            if (type == typeof(DateTime))
                return typeof(DateTimeFieldHandlerContext);
            if (type.IsArray)
                return typeof (ArrayFieldHandlerContext<,>).MakeGenericType(type, type.GetElementType());
            if (typeof(IList).IsAssignableFrom(type))
            {
                var itemType = typeof(object);
                if (type.IsGenericType && typeof(IList<>).MakeGenericType(type.GetGenericArguments()[0]).IsAssignableFrom(type))
                    itemType = type.GetGenericArguments()[0];

                return typeof(ListFieldHandlerContext<,>).MakeGenericType(type, itemType);
            }

            return typeof(DefaultFieldHandlerContext<>).MakeGenericType(type);
        }

        public static Field.Store ToFieldStoreConstant(StorageBehavior storageBehavior)
        {
            switch (storageBehavior)
            {
                case StorageBehavior.DoNotStore:
                    return Field.Store.NO;
                case StorageBehavior.Store:
                    return Field.Store.YES;
                default:
                    throw new Exception("Unknown storage behavior");
            }
        }

        public static Field.Index ToFieldIndexConstant(IndexBehavior indexBehavior)
        {
            switch (indexBehavior)
            {
                case IndexBehavior.Analyze:
                    return Field.Index.ANALYZED;
                case IndexBehavior.AnalyzeNoNormalization:
                    return Field.Index.ANALYZED_NO_NORMS;
                case IndexBehavior.DoNotAnalyze:
                    return Field.Index.NOT_ANALYZED;
                case IndexBehavior.DoNotAnalyzeNoNormalization:
                    return Field.Index.NOT_ANALYZED_NO_NORMS;
                case IndexBehavior.DoNotIndex:
                    return Field.Index.NO;
                default:
                    throw new Exception("Unknown index behavior");
            }
        }

        public static DateTools.Resolution ToDateToolsResolutionConstant(DateResolution dateResolution)
        {
            switch (dateResolution)
            {
                case DateResolution.Day:
                    return DateTools.Resolution.DAY;
                case DateResolution.Hour:
                    return DateTools.Resolution.HOUR;
                case DateResolution.Millisecond:
                    return DateTools.Resolution.MILLISECOND;
                case DateResolution.Minute:
                    return DateTools.Resolution.MINUTE;
                case DateResolution.Month:
                    return DateTools.Resolution.MONTH;
                case DateResolution.Second:
                    return DateTools.Resolution.SECOND;
                case DateResolution.Year:
                    return DateTools.Resolution.YEAR;
                default:
                    throw new Exception("Unknown date resolution.");
            }
        }
    }
}
