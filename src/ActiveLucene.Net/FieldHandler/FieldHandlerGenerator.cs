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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using Microsoft.CSharp;

namespace ActiveLucene.Net.FieldHandler
{
    public interface IFieldHandler<T>
    {
        void Set(Document doc, T record);
        T Get(Document doc);
    }

    public class FieldHandlerConfiguration
    {
        public string Name { get; set; }
        public Field.Store Store { get; set; }
        public Field.Index Index { get; set; }
        public DateTools.Resolution DateResolution { get; set; }
    }

    internal static class FieldHandlerGenerator<T>
    {
        internal static IFieldHandler<T> Create()
        {
            var prov = new CSharpCodeProvider(new Dictionary<string, string>
                                                  {
                                                      {"CompilerVersion", "v3.5"}
                                                  });

            var sb = new StringBuilder();

            var typeName = typeof(T).Name + "FieldHandler";
            sb.Append("using System;\n");
            sb.Append("using System.Linq;\n");
            sb.Append("using System.Collections.Generic;\n");
            sb.Append("using Lucene.Net;\n");
            sb.Append("using Lucene.Net.Documents;\n");
            sb.AppendFormat("using {0};\n", typeof(FieldHandlerGenerator<>).Namespace);
            sb.Append("\n\n");

            sb.AppendFormat("public class {0} : IFieldHandler<{1}> {{\n\n", typeName, typeof(T).FullName);

            var sbSetMethod = new StringBuilder();
            var sbGetMethod = new StringBuilder();

            // first, walk the properties and set up the Get/Set methods
            foreach (var propertyInfo in typeof(T).GetProperties())
            {
                var attribute = propertyInfo.GetCustomAttributes(typeof (LuceneFieldAttribute), false)
                    .Cast<LuceneFieldAttribute>()
                    .FirstOrDefault();

                if (attribute == null || String.IsNullOrEmpty(attribute.Name))
                    continue;

                var memberName = GetUniqueVariableName();

                if (propertyInfo.PropertyType == typeof(string))
                {
                    sb.AppendFormat("private readonly StringFieldHandlerContext {0} = new StringFieldHandlerContext({1});\n",
                                    memberName, ToFieldHandlerConfiguration(attribute));
                }

                sbGetMethod.AppendFormat("\trecord.{0} = {1}.GetValue(doc);\n", propertyInfo.Name, memberName);
                sbSetMethod.AppendFormat("\t{0}.SetFields(doc, record.{1});\n", memberName, propertyInfo.Name);
            }

            // append the Set method
            sb.AppendFormat("\npublic void Set(Document doc, {0} record) {{\n\n", typeof(T).FullName);
            sb.AppendFormat("\tdoc.GetFields().Clear();\n");
            sb.Append(sbSetMethod.ToString());
            sb.Append("\n}\n");

            // append the Get method
            sb.AppendFormat("\npublic {0} Get(Document doc) {{\n\n", typeof(T).FullName);
            sb.AppendFormat("\tvar record = new {0}();\n", typeof (T).FullName);
            sb.Append(sbGetMethod.ToString());
            sb.AppendFormat("\treturn record;\n");
            sb.Append("\n}\n");

            // end class
            sb.Append("\n}\n");

            var compilerParams = new CompilerParameters(new[]
                                                            {
                                                                "System.Core.dll",

                                                                // add Lucene assembly
                                                                typeof(Document).Assembly.Location,

                                                                // add this assembly
                                                                typeof (FieldHandlerGenerator<>).Assembly.Location,

                                                                // add the referenced type's assembly
                                                                typeof (T).Assembly.Location
                                                            })
                                     {
                                         CompilerOptions = "/optimize",
                                     };

            var compilerResults = prov.CompileAssemblyFromSource(compilerParams, sb.ToString());
            if(compilerResults.Errors.HasErrors)
            {
                var sbErrors = new StringBuilder("Errors in field handler compilation.\n\n");
                foreach(var error in compilerResults.Errors)
                {
                    sbErrors.AppendLine("\t" + error);
                }

                throw new Exception(sbErrors.ToString());
            }

            return (IFieldHandler<T>)Activator.CreateInstance(compilerResults.CompiledAssembly.GetType(typeName));
        }
        
        private static readonly Random _random = new Random();

        private static string GetUniqueVariableName()
        {
            return "x" + _random.Next().ToString("x");
        }

        private static bool IsNumericFieldType(Type type)
        {
            return type == typeof (int) || type == typeof (long) || type == typeof (float) || type == typeof (double);
        }

        private static string ToFieldStoreConstant(StorageBehavior storageBehavior)
        {
            switch (storageBehavior)
            {
                case StorageBehavior.Compress:
                    return "Field.Store.COMPRESS";
                case StorageBehavior.DoNotStore:
                    return "Field.Store.NO";
                case StorageBehavior.Store:
                    return "Field.Store.YES";
                default:
                    throw new Exception("Unknown storage behavior");
            }
        }

        private static string ToFieldIndexConstant(IndexBehavior indexBehavior)
        {
            switch (indexBehavior)
            {
                case IndexBehavior.Analyze:
                    return "Field.Index.ANALYZED";
                case IndexBehavior.AnalyzeNoNormalization:
                    return "Field.Index.ANALYZED_NO_NORMS";
                case IndexBehavior.DoNotAnalyze:
                    return "Field.Index.NOT_ANALYZED";
                case IndexBehavior.DoNotAnalyzeNoNormalization:
                    return "Field.Index.NOT_ANALYZED_NO_NORMS";
                case IndexBehavior.DoNotIndex:
                    return "Field.Index.NO";
                default:
                    throw new Exception("Unknown index behavior");
            }
        }

        private static string ToDateToolsResolutionConstant(DateResolution dateResolution)
        {
            return "DateTools.Resolution." + dateResolution.ToString().ToUpper();
        }

        private static string ToFieldHandlerConfiguration(LuceneFieldAttribute fieldAttribute)
        {
            return String.Format("new FieldHandlerConfiguration{{Name=\"{0}\", Store={1}, Index={2}, DateResolution={3}}}",
                                 fieldAttribute.Name,
                                 ToFieldStoreConstant(fieldAttribute.StorageBehavior),
                                 ToFieldIndexConstant(fieldAttribute.IndexBehavior),
                                 ToDateToolsResolutionConstant(fieldAttribute.DateResolution));
        }
    }
}