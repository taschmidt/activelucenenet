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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using Microsoft.CSharp;

namespace ActiveLucene.Net.FieldHandler
{
    public interface IFieldHandler<T>
    {
        T ToRecord(Document doc);
        void ToDocument(Document doc, T record);
    }

    internal static class FieldHandlerGenerator<T>
    {
        internal static IFieldHandler<T> Create()
        {
            var typeName = typeof(T).Name + "FieldHandler";
            var fieldHandler = new CodeTypeDeclaration(typeName) {IsClass = true};
            fieldHandler.BaseTypes.Add(typeof (IFieldHandler<>).MakeGenericType(typeof (T)));

            var ctor = new CodeConstructor {Attributes = MemberAttributes.Public};

            var documentToRecordMethod = new CodeMemberMethod {Name = "ToRecord", Attributes = MemberAttributes.Public};
            documentToRecordMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Document), "doc"));
            documentToRecordMethod.ReturnType = new CodeTypeReference(typeof (T));
            documentToRecordMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof (T), "record",
                                                                          new CodeObjectCreateExpression(typeof (T))));

            var recordToDocumentMethod = new CodeMemberMethod {Name = "ToDocument", Attributes = MemberAttributes.Public};
            recordToDocumentMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof (Document), "doc"));
            recordToDocumentMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof (T), "record"));
            recordToDocumentMethod.Statements.Add(new CodeSnippetStatement("\tdoc.GetFields().Clear();"));

            var ns = new CodeNamespace
                         {
                             Imports =
                                 {
                                     new CodeNamespaceImport("System"),
                                     new CodeNamespaceImport("System.Linq"),
                                     new CodeNamespaceImport("System.Collections.Generic"),
                                     new CodeNamespaceImport("Lucene.Net"),
                                     new CodeNamespaceImport("Lucene.Net.Documents")
                                 },
                             Types = {fieldHandler}
                         };

            var compileUnit = new CodeCompileUnit
                                  {
                                      Namespaces =
                                          {
                                              ns,
                                          },
                                      ReferencedAssemblies =
                                          {
                                              "System.Core.dll",

                                              // add Lucene assembly
                                              typeof (Document).Assembly.Location,

                                              // add this assembly
                                              typeof (FieldHandlerGenerator<>).Assembly.Location,

                                              // add the referenced type's assembly
                                              typeof (T).Assembly.Location
                                          }
                                  };

            foreach (var propertyInfo in typeof(T).GetProperties())
            {
                var attribute = propertyInfo.GetCustomAttributes(typeof(LuceneFieldAttribute), false)
                    .Cast<LuceneFieldAttribute>()
                    .FirstOrDefault();

                if (attribute == null || String.IsNullOrEmpty(attribute.Name))
                    continue;

                var ctxConfiguration = new CodeObjectCreateExpression(
                    typeof(FieldHandlerConfiguration),
                    new CodePrimitiveExpression(attribute.Name),
                    new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(StorageBehavior)), attribute.StorageBehavior.ToString()),
                    new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(IndexBehavior)), attribute.IndexBehavior.ToString()),
                    new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(DateResolution)), attribute.DateResolution.ToString()));

                var ctxType = FieldHandlerHelpers.GetFieldHandlerContextType(propertyInfo.PropertyType);
                var memberField = new CodeMemberField(ctxType, GetUniqueVariableName())
                                      {
                                          Attributes = MemberAttributes.Private,
                                          InitExpression = new CodeObjectCreateExpression(ctxType, ctxConfiguration)
                                      };

                fieldHandler.Members.Add(memberField);

                documentToRecordMethod.Statements.Add(new CodeSnippetStatement(String.Format("\trecord.{0} = {1}.GetValue(doc);\n", propertyInfo.Name, memberField.Name)));
                recordToDocumentMethod.Statements.Add(new CodeSnippetStatement(String.Format("\t{0}.SetFields(doc, record.{1});\n", memberField.Name, propertyInfo.Name)));
            }

            documentToRecordMethod.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("record")));

            fieldHandler.Members.AddRange(new[] { ctor, documentToRecordMethod, recordToDocumentMethod });

            var compilerParams = new CompilerParameters
                                     {
                                         CompilerOptions = "/optimize",
                                     };

            var prov = new CSharpCodeProvider(new Dictionary<string, string>
                                                  {
                                                      {"CompilerVersion", "v3.5"}
                                                  });
            var sb = new StringBuilder();
            using(var writer = new StringWriter(sb))
            {
                prov.GenerateCodeFromCompileUnit(compileUnit, writer,
                                                 new CodeGeneratorOptions
                                                     {
                                                         BlankLinesBetweenMembers = true,
                                                         BracingStyle = "C",
                                                     });
            }

            var compilerResults = prov.CompileAssemblyFromDom(compilerParams, compileUnit);
            if (compilerResults.Errors.HasErrors)
            {
                var sbErrors = new StringBuilder("Errors in field handler compilation.\n\n");
                foreach (var error in compilerResults.Errors)
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
    }
}