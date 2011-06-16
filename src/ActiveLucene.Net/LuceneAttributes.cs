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

namespace ActiveLucene.Net
{
    public enum StorageBehavior
    {
        Store,
        DoNotStore,
        Compress
    }

    public enum IndexBehavior
    {
        Analyze,
        AnalyzeNoNormalization,
        DoNotIndex,
        DoNotAnalyze,
        DoNotAnalyzeNoNormalization
    }

    public enum DateResolution
    {
        Day,
        Hour,
        Millisecond,
        Minute,
        Month,
        Second,
        Year
    }

    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
    public class LuceneDocumentAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class LuceneFieldAttribute : Attribute
    {
        public string Name;
        public StorageBehavior StorageBehavior;
        public IndexBehavior IndexBehavior;
        public DateResolution DateResolution = DateResolution.Second;

        public LuceneFieldAttribute(string name) : this(name, StorageBehavior.Store, IndexBehavior.Analyze)
        {
        }

        public LuceneFieldAttribute(string name, StorageBehavior storageBehavior, IndexBehavior indexBehavior)
        {
            Name = name;
            StorageBehavior = storageBehavior;
            IndexBehavior = indexBehavior;
        }
    }

    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
    public class LuceneDocumentBoostAttribute : Attribute
    {
    }
}