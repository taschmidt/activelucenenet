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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using Microsoft.CSharp;

namespace ActiveLucene.Net
{
    public interface ILuceneFieldHandler<T>
    {
        void Set(Document doc, T record);
        T Get(Document doc);
    }

    internal static class LuceneFieldHandlerGenerator<T>
    {
        internal static ILuceneFieldHandler<T> Create()
        {
            var prov = new CSharpCodeProvider(new Dictionary<string, string>
                                                  {
                                                      {"CompilerVersion", "v3.5"}
                                                  });
            var sb = new StringBuilder();

            var typeName = typeof (T).Name + "FieldHandler";
            sb.Append("using System;\n");
            sb.Append("using System.Linq;\n");
            sb.Append("using System.Collections.Generic;\n");
            sb.Append("using Lucene.Net;\n");
            sb.Append("using Lucene.Net.Documents;\n");
            sb.AppendFormat("using {0};\n", typeof (LuceneFieldHandlerGenerator<>).Namespace);
            sb.Append("\n\n");

            sb.AppendFormat("public class {0} : ILuceneFieldHandler<{1}> {{\n\n", typeName, typeof (T).FullName);

            var sbSetMethod = new StringBuilder();
            var sbGetMethod = new StringBuilder();

            // first, walk the properties and set up the Get/Set methods
            foreach (var propertyInfo in typeof (T).GetProperties())
            {
                var attribute = propertyInfo.GetCustomAttributes(typeof (LuceneFieldAttribute), false)
                                    .FirstOrDefault() as LuceneFieldAttribute;
                if (attribute == null || String.IsNullOrEmpty(attribute.Name))
                    continue;

                if (propertyInfo.PropertyType.IsClass)
                    sbSetMethod.AppendFormat("\n\tif(record.{0} != null) {{\n", propertyInfo.Name);

                if (typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType) && propertyInfo.PropertyType != typeof(String))
                {
                    if (!typeof(IList).IsAssignableFrom(propertyInfo.PropertyType))
                        throw new Exception(String.Format("Enumerable types must implement IList (property {0}).", propertyInfo.Name));

                    // collection
                    var collectionType = typeof (object);
                    if (propertyInfo.PropertyType.IsGenericType)
                        collectionType = propertyInfo.PropertyType.GetGenericArguments()[0];
                    else if (propertyInfo.PropertyType.IsArray)
                        collectionType = propertyInfo.PropertyType.GetElementType();

                    // set method
                    sbSetMethod.AppendFormat("\tforeach(var x in record.{0}) {{\n", propertyInfo.Name);
                    sbSetMethod.AppendFormat("\t\tvar fld = {0};\n", GetNewFieldDeclaration(collectionType, attribute));
                    sbSetMethod.AppendFormat("\t\t{0};\n", GetFieldSetterCode(collectionType, "fld", "x", attribute.DateResolution));
                    sbSetMethod.Append("\t\tdoc.Add(fld);\n");
                    sbSetMethod.Append("\t}\n");

                    // get method
                    sbGetMethod.AppendFormat("\n\trecord.{0} = doc.GetValues(\"{1}\")\n", propertyInfo.Name,
                                             attribute.Name);
                    sbGetMethod.AppendFormat("\t\t.Select(str => {0}).{1};\n",
                                             GetFieldGetterCode(collectionType, "str"),
                                             propertyInfo.PropertyType.IsArray ? "ToArray()" : "ToList()");
                }
                else
                {
                    // not a collection
                    var memberFieldName = GetUniqueVariableName();
                    sb.AppendFormat("private readonly {2} {0} = {1};\n",
                                    memberFieldName, GetNewFieldDeclaration(propertyInfo.PropertyType, attribute),
                                    IsNumericFieldType(propertyInfo.PropertyType) ? "NumericField" : "Field");

                    // set method
                    sbSetMethod.AppendFormat("\t{0};\n", GetFieldSetterCode(propertyInfo.PropertyType,
                                                                            memberFieldName,
                                                                            "record." + propertyInfo.Name,
                                                                            attribute.DateResolution));
                    sbSetMethod.AppendFormat("\tdoc.Add({0});\n", memberFieldName);

                    // get method
                    var tempVarName = GetUniqueVariableName();
                    sbGetMethod.AppendFormat("\n\tvar {0} = doc.Get(\"{1}\");\n", tempVarName, attribute.Name)
                        .AppendFormat("\trecord.{0} = {1};\n", propertyInfo.Name, GetFieldGetterCode(propertyInfo.PropertyType, tempVarName));
                }

                if (propertyInfo.PropertyType.IsClass)
                    sbSetMethod.Append("\t}\n");
            }

            // append the Set method
            sb.AppendFormat("\npublic void Set(Document doc, {0} record) {{\n\n", typeof (T).FullName);
            sb.Append("\tdoc.GetFields().Clear();\n");
            sb.Append(sbSetMethod.ToString());
            sb.Append("\n}\n");

            // append the Get method
            sb.AppendFormat("\npublic {0} Get(Document doc) {{\n\n", typeof (T).FullName);
            sb.AppendFormat("\tvar record = new {0}();\n", typeof (T).FullName);
            sb.Append(sbGetMethod.ToString());
            sb.Append("\n\treturn record;\n");
            sb.Append("\n}\n");

            // end class
            sb.Append("\n}\n");

            var compilerParams = new CompilerParameters(new[]
                                                            {
                                                                "System.Core.dll",

                                                                // add Lucene assembly
                                                                typeof(Document).Assembly.Location,

                                                                // add this assembly
                                                                typeof (LuceneFieldHandlerGenerator<>).Assembly.Location,

                                                                // add the referenced type's assembly
                                                                typeof (T).Assembly.Location
                                                            })
                                     {
                                         CompilerOptions = "/optimize"
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

            return (ILuceneFieldHandler<T>)Activator.CreateInstance(compilerResults.CompiledAssembly.GetType(typeName));
        }
        
        private static readonly Random _random = new Random();

        private static string GetUniqueVariableName()
        {
            return "x" + _random.Next().ToString("x");
        }

        private static string GetNewFieldDeclaration(Type type, LuceneFieldAttribute attribute)
        {
            if(IsNumericFieldType(type))
            {
                var str = String.Format("new NumericField(\"{0}\", {1}, {2})",
                                        attribute.Name,
                                        ToFieldStoreConstant(attribute.StorageBehavior),
                                        (attribute.IndexBehavior == IndexBehavior.DoNotIndex) ? "false" : "true");

                return str;
            }

            return String.Format("new Field(\"{0}\", \"\", {1}, {2})",
                                 attribute.Name,
                                 ToFieldStoreConstant(attribute.StorageBehavior),
                                 ToFieldIndexConstant(attribute.IndexBehavior));
        }

        private static string GetFieldSetterCode(Type type, string strField, string value, DateResolution resolution)
        {
            if (type == typeof(string))
                return String.Format("{0}.SetValue({1})", strField, value);
            if (IsNumericFieldType(type))
                return String.Format("{0}.{1}", strField, GetNumericFieldSetterCode(type, value));
            if (type == typeof(DateTime))
                return String.Format("{0}.SetValue(DateTools.DateToString({1}, {2}))",
                                     strField, value, ToDateToolsResolution(resolution));

            return String.Format("{0}.SetValue({1}.ToString())", strField, value);
        }

        private static string GetNumericFieldSetterCode(Type type, string value)
        {
            if (type == typeof(int))
                return String.Format("SetIntValue({0})", value);
            if (type == typeof(long))
                return String.Format("SetLongValue({0})", value);
            if (type == typeof(float))
                return String.Format("SetFloatValue({0})", value);
            if (type == typeof(double))
                return String.Format("SetDoubleValue({0})", value);

            throw new Exception("Unknown type for numeric field setter");
        }

        private static bool IsNumericFieldType(Type type)
        {
            return type == typeof (int) || type == typeof (long) || type == typeof (float) || type == typeof (double);
        }

        private static string GetFieldGetterCode(Type type, string strVarName)
        {
            if (type == typeof (string))
                return strVarName;
            if (type == typeof (DateTime))
                return String.Format("!String.IsNullOrEmpty({0}) ? DateTools.StringToDate({0}) : default(DateTime)", strVarName);

            return String.Format("!String.IsNullOrEmpty({0}) ? ({1})Convert.ChangeType({0}, typeof({1})) : default({1})",
                                 strVarName, type.Name);
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

        private static string ToDateToolsResolution(DateResolution dateResolution)
        {
            return "DateTools.Resolution." + dateResolution.ToString().ToUpper();
        }
    }
}