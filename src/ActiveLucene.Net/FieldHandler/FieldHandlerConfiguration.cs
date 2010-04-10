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
    public class FieldHandlerConfiguration
    {
        public string Name { get; private set; }
        public Field.Store Store { get; private set; }
        public Field.Index Index { get; private set; }
        public DateTools.Resolution DateResolution { get; private set; }

        public FieldHandlerConfiguration(string name, StorageBehavior storageBehavior,
            IndexBehavior indexBehavior, DateResolution dateResolution)
        {
            Name = name;
            Store = ToFieldStoreConstant(storageBehavior);
            Index = ToFieldIndexConstant(indexBehavior);
            DateResolution = ToDateToolsResolutionConstant(dateResolution);
        }

        private static Field.Store ToFieldStoreConstant(StorageBehavior storageBehavior)
        {
            switch (storageBehavior)
            {
                case StorageBehavior.Compress:
                    return Field.Store.COMPRESS;
                case StorageBehavior.DoNotStore:
                    return Field.Store.NO;
                case StorageBehavior.Store:
                    return Field.Store.YES;
                default:
                    throw new Exception("Unknown storage behavior");
            }
        }

        private static Field.Index ToFieldIndexConstant(IndexBehavior indexBehavior)
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

        private static DateTools.Resolution ToDateToolsResolutionConstant(DateResolution dateResolution)
        {
            switch (dateResolution)
            {
                case Net.DateResolution.Day:
                    return DateTools.Resolution.DAY;
                case Net.DateResolution.Hour:
                    return DateTools.Resolution.HOUR;
                case Net.DateResolution.Millisecond:
                    return DateTools.Resolution.MILLISECOND;
                case Net.DateResolution.Minute:
                    return DateTools.Resolution.MINUTE;
                case Net.DateResolution.Month:
                    return DateTools.Resolution.MONTH;
                case Net.DateResolution.Second:
                    return DateTools.Resolution.SECOND;
                case Net.DateResolution.Year:
                    return DateTools.Resolution.YEAR;
                default:
                    throw new Exception("Unknown date resolution.");
            }
        }
    }
}