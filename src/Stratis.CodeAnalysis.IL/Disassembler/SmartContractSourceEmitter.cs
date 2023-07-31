#region Attribution
// Based on PeToText.CSharpSourceEmitter
// Pe2Text.CSharpSourceEmitter has the following copyright notice 
//
//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics.Contracts;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.Cci;
using Microsoft.Cci.MetadataReader;
using Microsoft.Cci.ILToCodeModel;
using Microsoft.Cci.Contracts;
using CSharpSourceEmitter;

namespace Stratis.DevEx.CodeAnalysis.IL
{
    public class SmartContractSourceEmitter : SourceEmitter
    {
        #region Constructor
        public SmartContractSourceEmitter(SmartContractSourceEmitterOutput sourceEmitterOutput, IMetadataHost host, PdbReader? pdbReader, bool printCompilerGeneratedMembers, bool noIL, string? classPattern, string? methodPattern)
          : base(sourceEmitterOutput)
        {
            this.host = host;
            this.pdbReader = pdbReader;
            this.noIL = noIL;
            this.printCompilerGeneratedMembers = printCompilerGeneratedMembers;
            //this.scsourceEmitterOutput = sourceEmitterOutput;
            this.scsourceEmitterOutput = sourceEmitterOutput;
            if (classPattern is not null) this.classPattern = new Regex(classPattern, RegexOptions.Singleline | RegexOptions.Compiled);
            if (methodPattern is not null) this.methodPattern = new Regex(methodPattern, RegexOptions.Singleline | RegexOptions.Compiled);
            
        }
        #endregion

        #region Methods

        #region Traversers
        public override void TraverseChildren(INamespaceTypeDefinition namespaceTypeDefinition)
        {
            //var td = (ITypeDefinition)namespaceTypeDefinition;
            if (namespaceTypeDefinition.IsClass && classPattern is not null && classPattern.IsMatch(namespaceTypeDefinition.GetName()))
            {
                Runtime.Debug("Traversing class {0} that matches pattern {1}...", namespaceTypeDefinition.GetName(), classPattern);
                scsourceEmitterOutput.SetCurrentClass(namespaceTypeDefinition.GetName());
                base.TraverseChildren(namespaceTypeDefinition);
            }
            else if (namespaceTypeDefinition.IsClass && classPattern is not null && !classPattern.IsMatch(namespaceTypeDefinition.GetName()))
            {
                Runtime.Debug("Not traversing class {0} that does not match pattern {1}.", namespaceTypeDefinition.GetName(), classPattern);
                return;
            }
            else
            {
                Runtime.Debug("Traversing class {0}...", namespaceTypeDefinition.GetName());
                scsourceEmitterOutput.SetCurrentClass(namespaceTypeDefinition.GetName());
                base.TraverseChildren(namespaceTypeDefinition);
            }
        }

        

        public override void TraverseChildren(IMethodDefinition method)
        {
            if (methodPattern is not null && methodPattern.IsMatch(method.Name.Value))
            {
                Runtime.Debug("Traversing method {0} that matches pattern {1}...", method.Name.Value, methodPattern);
                base.TraverseChildren(method);
            }
            else if (methodPattern is not null && !methodPattern.IsMatch(method.Name.Value))
            {
                Runtime.Debug("Not traversing method {0} that does not match pattern {1}.", method.Name, methodPattern);
                return;
            }
            else
            {
                Runtime.Debug("Traversing method {0}...", method.Name.Value);
                base.TraverseChildren(method);
            }
        }

        public override void TraverseChildren(IPropertyDefinition prop)
        {
            if (methodPattern is not null && methodPattern.IsMatch(prop.Name.Value))
            {
                Runtime.Debug("Traversing property {0} that matches pattern {1}...", prop.Name.Value, methodPattern);
                base.TraverseChildren(prop);
            }
            else if (methodPattern is not null && !methodPattern.IsMatch(prop.Name.Value))
            {
                Runtime.Debug("Not traversing property {0} that does not match pattern {1}.", prop.Name, methodPattern);
                return;
            }
            else
            {
                Runtime.Debug("Traversing property {0}...", prop.Name.Value);
                base.TraverseChildren(prop);

            }
        }

        public override void Traverse(IMethodBody methodBody)
        {
            PrintToken(CSharpToken.LeftCurly);
            ISourceMethodBody? sourceMethodBody = (ISourceMethodBody)methodBody;
            if (sourceMethodBody == null)
            {
                var options = DecompilerOptions.Loops;
                if (!this.printCompilerGeneratedMembers)
                    options |= (DecompilerOptions.AnonymousDelegates | DecompilerOptions.Iterators);
                sourceMethodBody = new SourceMethodBody(methodBody, this.host, this.pdbReader, this.pdbReader, options);
            }
            if (this.noIL)
                this.Traverse(sourceMethodBody.Block.Statements);
            else
            {
                /*
                this.Traverse(sourceMethodBody.Block);
                PrintToken(CSharpToken.NewLine);

                if (this.pdbReader != null)
                     PrintScopes(methodBody);
                else
                    PrintLocals(methodBody.LocalVariables);
                */
                int gasCost = 0;
                foreach (IOperation operation in methodBody.Operations)
                {
                    if (this.pdbReader != null)
                    {

                        foreach (IPrimarySourceLocation psloc in this.pdbReader.GetPrimarySourceLocationsFor(operation.Location))
                        {
                            PrintSourceLocation(psloc);
                        }
                    }
                    PrintOperation(operation);
                    if (operation.OperationCode == OperationCode.Call || operation.OperationCode == OperationCode.Callvirt)
                    {
                        gasCost += 5;
                    }
                    else
                    {
                        gasCost += 1;
                    }
                }
                PrintInstructionCount(methodBody.Operations);
                PrintTotalGasCost(gasCost);
            }
            PrintToken(CSharpToken.RightCurly);
        }

        public override void PrintTypeDefinitionLeftCurly(ITypeDefinition typeDefinition)
        {
            this.PrintToken(CSharpToken.LeftCurly);
        }

        public override void PrintTypeDefinitionRightCurly(ITypeDefinition typeDefinition)
        {
            this.PrintToken(CSharpToken.RightCurly);
        }
        #endregion

        #region Printers
        private void PrintOperation(IOperation operation)
        {
            scsourceEmitterOutput.Write("IL_" + operation.Offset.ToString("x4") + ": ", true, Color.Blue);
            scsourceEmitterOutput.Write(operation.OperationCode.ToString(), Color.Magenta);
            if (operation is ILocalDefinition ld)
                scsourceEmitterOutput.Write(" " + this.GetLocalName(ld), Color.Red);
            else if (operation.Value is string)
                scsourceEmitterOutput.Write(" \"" + operation.Value + "\"", Color.Brown);
            else if (operation.Value is not null)
            {
                if (OperationCode.Br_S <= operation.OperationCode && operation.OperationCode <= OperationCode.Blt_Un)
                    scsourceEmitterOutput.Write(" IL_" + ((uint)operation.Value).ToString("x4"));
                else if (operation.OperationCode == OperationCode.Switch)
                {
                    foreach (uint i in (uint[])operation.Value)
                        scsourceEmitterOutput.Write(" IL_" + i.ToString("x4"));
                }
                else if (operation.Value is Microsoft.Cci.MutableCodeModel.MethodDefinition md)
                {
                    string mdt = md.Name.Value.StartsWith("get_") || md.Name.Value.StartsWith("set_") ? (md.Name.Value.StartsWith("get_") ? "get" : "set") : "method";
                    scsourceEmitterOutput.Write($" [{mdt}] ", Color.Cyan);
                    scsourceEmitterOutput.Write(md.Type.ToString() + " ", Color.Cyan);
                    scsourceEmitterOutput.Write($"{md.ContainingTypeDefinition.GetName()}.", Color.Pink);
                    scsourceEmitterOutput.Write(md.Name.Value.Replace("get_", "").Replace("set_", ""), Color.Yellow);
                    if (md.Parameters is not null && md.Parameters.Any())
                    {
                        scsourceEmitterOutput.Write("(" + md.Parameters.Select(p => p.Type.ToString() ?? "").JoinWith(",") + ")", Color.LimeGreen);
                    }
                    else
                    {
                        scsourceEmitterOutput.Write("()");
                    }
                }
                else if (operation.Value is Microsoft.Cci.MutableCodeModel.MethodReference mr)
                {
                    string mrt = mr.Name.Value.StartsWith("get_") || mr.Name.Value.StartsWith("set_") ? (mr.Name.Value.StartsWith("get_") ? "get" : "set") : "method";
                    scsourceEmitterOutput.Write($" [{mrt}] ", Color.Cyan);
                    scsourceEmitterOutput.Write(mr.Type.ToString() + " ", Color.Cyan);
                    scsourceEmitterOutput.Write($"{mr.ContainingType.ToString()}.", Color.Pink);
                    scsourceEmitterOutput.Write(mr.Name.Value.Replace("get_", "").Replace("set_", ""), Color.Yellow);
                    if (mr.Parameters is not null && mr.Parameters.Any())
                    {
                        scsourceEmitterOutput.Write("(" + mr.Parameters.Select(p => p.Type.ToString()!).JoinWith(", ") + ")", Color.LimeGreen);
                    }
                    else
                    {
                        scsourceEmitterOutput.Write("()");
                    }
                }
                else if (operation.Value is Microsoft.Cci.MutableCodeModel.FieldDefinition fd)
                {
                    scsourceEmitterOutput.Write(" [field] ", Color.Cyan);
                    scsourceEmitterOutput.Write(fd.Type.ToString() + " ", Color.Cyan);
                    scsourceEmitterOutput.Write(fd.ContainingTypeDefinition.GetName() + ".", Color.Pink);
                    scsourceEmitterOutput.Write(fd.Name.ToString()!, Color.Yellow);

                }
                else if (operation.Value is Microsoft.Cci.MutableCodeModel.FieldReference fr)
                {
                    scsourceEmitterOutput.Write(" [field] ", Color.Cyan);
                    scsourceEmitterOutput.Write(fr.Type.ToString() + " ", Color.Cyan);
                    scsourceEmitterOutput.Write(fr.ContainingType.ToString() + ".", Color.Pink);
                    scsourceEmitterOutput.Write(fr.Name.ToString()!, Color.Yellow);

                }
                else if (operation.Value is Microsoft.Cci.MutableCodeModel.ParameterDefinition pd)
                {
                    scsourceEmitterOutput.Write(" [param] ", Color.Cyan);
                    scsourceEmitterOutput.Write(pd.Type.ToString() + " ", Color.Pink);
                    scsourceEmitterOutput.Write(pd.Name.ToString()!, Color.Yellow);

                }
                else if (operation.Value is Microsoft.Cci.MutableCodeModel.LocalDefinition _ld)
                {
                    scsourceEmitterOutput.Write(" [var] ", Color.Cyan);
                    scsourceEmitterOutput.Write(_ld.Type.ToString() + " ", Color.Cyan);
                    scsourceEmitterOutput.Write(_ld.Name.ToString()!, Color.Yellow);
                }
                else
                {
                    scsourceEmitterOutput.Write(/*operation.Value.GetType().ToString() +*/ " " + operation.Value);
                }
            }
            scsourceEmitterOutput.WriteLine("", false);
        }
        public override bool PrintToken(CSharpToken token)
        {
            switch (token)
            {
                #region Punctuation
                case CSharpToken.Assign:
                    scsourceEmitterOutput.Write("=");
                    break;
                case CSharpToken.NewLine:
                    scsourceEmitterOutput.WriteLine("");
                    break;
                case CSharpToken.Indent:
                    scsourceEmitterOutput.Write("", true);
                    break;
                case CSharpToken.Space:
                    scsourceEmitterOutput.Write(" ");
                    break;
                case CSharpToken.Dot:
                    scsourceEmitterOutput.Write(".");
                    break;
                case CSharpToken.LeftCurly:
                    if (this.LeftCurlyOnNewLine)
                    {
                        if (!this.scsourceEmitterOutput.CurrentLineEmpty)
                            PrintToken(CSharpToken.NewLine);
                    }
                    else
                    {
                        PrintToken(CSharpToken.Space);
                    }
                    scsourceEmitterOutput.WriteLine("{", this.LeftCurlyOnNewLine);
                    scsourceEmitterOutput.IncreaseIndent();
                    break;
                case CSharpToken.RightCurly:
                    scsourceEmitterOutput.DecreaseIndent();
                    scsourceEmitterOutput.WriteLine("}", true);
                    break;
                case CSharpToken.LeftParenthesis:
                    scsourceEmitterOutput.Write("(");
                    break;
                case CSharpToken.RightParenthesis:
                    scsourceEmitterOutput.Write(")");
                    break;
                case CSharpToken.LeftAngleBracket:
                    scsourceEmitterOutput.Write("<");
                    break;
                case CSharpToken.RightAngleBracket:
                    scsourceEmitterOutput.Write(">");
                    break;
                case CSharpToken.LeftSquareBracket:
                    scsourceEmitterOutput.Write("[");
                    break;
                case CSharpToken.RightSquareBracket:
                    scsourceEmitterOutput.Write("]");
                    break;
                case CSharpToken.Semicolon:
                    scsourceEmitterOutput.WriteLine(";");
                    break;
                case CSharpToken.Colon:
                    scsourceEmitterOutput.Write(":");
                    break;
                case CSharpToken.Comma:
                    scsourceEmitterOutput.Write(",");
                    break;
                case CSharpToken.Tilde:
                    scsourceEmitterOutput.Write("~");
                    break;
                #endregion

                #region Keywords
                case CSharpToken.Public:
                    scsourceEmitterOutput.Write("public ", Color.DarkBlue);
                    break;
                case CSharpToken.Private:
                    scsourceEmitterOutput.Write("private ", Color.DarkBlue);
                    break;
                case CSharpToken.Internal:
                    scsourceEmitterOutput.Write("internal ");
                    break;
                case CSharpToken.Protected:
                    scsourceEmitterOutput.Write("protected ", Color.DarkBlue);
                    break;
                case CSharpToken.Static:
                    scsourceEmitterOutput.Write("static ", Color.DarkBlue);
                    break;
                case CSharpToken.Abstract:
                    scsourceEmitterOutput.Write("abstract ", Color.DarkBlue);
                    break;
                case CSharpToken.Extern:
                    scsourceEmitterOutput.Write("extern ", Color.DarkBlue);
                    break;
                case CSharpToken.Unsafe:
                    scsourceEmitterOutput.Write("unsafe ", Color.DarkBlue);
                    break;
                case CSharpToken.ReadOnly:
                    scsourceEmitterOutput.Write("readonly ", Color.DarkBlue);
                    break;
                case CSharpToken.Fixed:
                    scsourceEmitterOutput.Write("fixed ", Color.DarkBlue);
                    break;
                case CSharpToken.New:
                    scsourceEmitterOutput.Write("new ", Color.DarkBlue);
                    break;
                case CSharpToken.Sealed:
                    scsourceEmitterOutput.Write("sealed ", Color.DarkBlue);
                    break;
                case CSharpToken.Virtual:
                    scsourceEmitterOutput.Write("virtual ", Color.DarkBlue);
                    break;
                case CSharpToken.Override:
                    scsourceEmitterOutput.Write("override ", Color.DarkBlue);
                    break;
                case CSharpToken.Class:
                    scsourceEmitterOutput.Write("class ", Color.DarkBlue);
                    break;
                case CSharpToken.Interface:
                    scsourceEmitterOutput.Write("interface ", Color.DarkBlue);
                    break;
                case CSharpToken.Struct:
                    scsourceEmitterOutput.Write("struct ", Color.DarkBlue);
                    break;
                case CSharpToken.Enum:
                    scsourceEmitterOutput.Write("enum ", Color.DarkBlue);
                    break;
                case CSharpToken.Delegate:
                    scsourceEmitterOutput.Write("delegate ", Color.DarkBlue);
                    break;
                case CSharpToken.Event:
                    scsourceEmitterOutput.Write("event ", Color.DarkBlue);
                    break;
                case CSharpToken.Namespace:
                    scsourceEmitterOutput.Write("namespace ", Color.DarkBlue);
                    break;
                case CSharpToken.Null:
                    scsourceEmitterOutput.Write("null", Color.DarkBlue);
                    break;
                case CSharpToken.In:
                    scsourceEmitterOutput.Write("in ", Color.DarkBlue);
                    break;
                case CSharpToken.Out:
                    scsourceEmitterOutput.Write("out ", Color.DarkBlue);
                    break;
                case CSharpToken.Ref:
                    scsourceEmitterOutput.Write("ref ", Color.DarkBlue);
                    break;
                #endregion

                #region Primitives
                case CSharpToken.Boolean:
                    scsourceEmitterOutput.Write("boolean ", Color.DarkBlue);
                    break;
                case CSharpToken.Byte:
                    scsourceEmitterOutput.Write("byte ", Color.DarkBlue);
                    break;
                case CSharpToken.Char:
                    scsourceEmitterOutput.Write("char ", Color.DarkBlue);
                    break;
                case CSharpToken.Double:
                    scsourceEmitterOutput.Write("double ", Color.DarkBlue);
                    break;
                case CSharpToken.Short:
                    scsourceEmitterOutput.Write("short ", Color.DarkBlue);
                    break;
                case CSharpToken.Int:
                    scsourceEmitterOutput.Write("int ", Color.DarkBlue);
                    break;
                case CSharpToken.Long:
                    scsourceEmitterOutput.Write("long ", Color.DarkBlue);
                    break;
                case CSharpToken.Object:
                    scsourceEmitterOutput.Write("object ", Color.DarkBlue);
                    break;
                case CSharpToken.String:
                    scsourceEmitterOutput.Write("string ", Color.DarkBlue);
                    break;
                case CSharpToken.UShort:
                    scsourceEmitterOutput.Write("ushort ", Color.DarkBlue);
                    break;
                case CSharpToken.UInt:
                    scsourceEmitterOutput.Write("uint ", Color.DarkBlue);
                    break;
                case CSharpToken.ULong:
                    scsourceEmitterOutput.Write("ulong ", Color.DarkBlue);
                    break;
                #endregion

                #region Statements
                case CSharpToken.Get:
                    scsourceEmitterOutput.Write("get", Color.DarkBlue);
                    break;
                case CSharpToken.Set:
                    scsourceEmitterOutput.Write("set", Color.DarkBlue);
                    break;
                case CSharpToken.Add:
                    scsourceEmitterOutput.Write("add", Color.DarkBlue);
                    break;
                case CSharpToken.Remove:
                    scsourceEmitterOutput.Write("remove", Color.DarkBlue);
                    break;
                case CSharpToken.Return:
                    scsourceEmitterOutput.Write("return", Color.DarkBlue);
                    break;
                case CSharpToken.This:
                    scsourceEmitterOutput.Write("this", Color.DarkBlue);
                    break;
                case CSharpToken.Throw:
                    scsourceEmitterOutput.Write("throw", Color.DarkBlue);
                    break;
                case CSharpToken.Try:
                    scsourceEmitterOutput.Write("try", Color.DarkBlue);
                    break;
                case CSharpToken.YieldReturn:
                    scsourceEmitterOutput.Write("yield return", Color.DarkBlue);
                    break;
                case CSharpToken.YieldBreak:
                    scsourceEmitterOutput.Write("yield break", Color.DarkBlue);
                    break;
                case CSharpToken.TypeOf:
                    scsourceEmitterOutput.Write("typeof", Color.DarkBlue);
                    break;
                default:
                    scsourceEmitterOutput.Write("Unknown-token", Color.DarkBlue);
                    break;
                #endregion

                #region Constants
                case CSharpToken.True:
                    scsourceEmitterOutput.Write("true");
                    break;
                case CSharpToken.False:
                    scsourceEmitterOutput.Write("false");
                    break;
                    #endregion
            }
            return true;
        }

        public override void PrintIdentifier(IName name) => scsourceEmitterOutput.Write(EscapeIdentifier(name.Value), Color.Yellow);

        public override void PrintTypeReferenceName(ITypeReference typeReference)
        {
            Contract.Requires(typeReference != null);
            var typeName = TypeHelper.GetTypeName(typeReference,
              NameFormattingOptions.ContractNullable | NameFormattingOptions.UseTypeKeywords |
              NameFormattingOptions.TypeParameters | NameFormattingOptions.EmptyTypeParameterList |
              NameFormattingOptions.OmitCustomModifiers);
            scsourceEmitterOutput.Write(typeName, Color.Cyan);
        }
        private void PrintScopes(IMethodBody methodBody)
        {
            if (this.pdbReader is not null)
            {
                foreach (ILocalScope scope in this.pdbReader.GetLocalScopes(methodBody))
                    PrintScopes(scope);
            }
        }

        private void PrintScopes(ILocalScope scope)
        {
            if (this.pdbReader is not null)
            {
                scsourceEmitterOutput.Write(string.Format("IL_{0} ... IL_{1} ", scope.Offset.ToString("x4"), (scope.Offset + scope.Length).ToString("x4")), true);
                scsourceEmitterOutput.WriteLine("{");
                scsourceEmitterOutput.IncreaseIndent();
                PrintConstants(this.pdbReader.GetConstantsInScope(scope));
                PrintLocals(this.pdbReader.GetVariablesInScope(scope));
                scsourceEmitterOutput.DecreaseIndent();
                scsourceEmitterOutput.WriteLine("}", true);
            }
        }

        private void PrintConstants(IEnumerable<ILocalDefinition> locals)
        {
            foreach (ILocalDefinition local in locals)
            {
                scsourceEmitterOutput.Write("const ", true);
                PrintTypeReference(local.Type);
                scsourceEmitterOutput.WriteLine(" " + this.GetLocalName(local));
            }
        }

        private void PrintLocals(IEnumerable<ILocalDefinition> locals)
        {
            foreach (ILocalDefinition local in locals)
            {
                scsourceEmitterOutput.Write("", true);
                PrintTypeReference(local.Type);
                scsourceEmitterOutput.WriteLine(" " + this.GetLocalName(local));
            }
        }

        public override void PrintLocalName(ILocalDefinition local)
        {
            this.scsourceEmitterOutput.Write(this.GetLocalName(local));
        }

        private void PrintSourceLocation(IPrimarySourceLocation psloc)
        {
            var source = psloc.Source.Trim() != "{" ? " " + psloc.Source.TrimEnd(';') : "";
            scsourceEmitterOutput.Write(psloc.Document.Name.Value + "(" + psloc.StartLine + ":" + psloc.StartColumn + ")-(" + psloc.EndLine + ":" + psloc.EndColumn + ")", true, Color.Red);
            if (source != string.Empty)
            {
                scsourceEmitterOutput.WriteLine(source + ":");
            }
            else
            {
                scsourceEmitterOutput.WriteLine("");
            }
        }

        private void PrintInstructionCount(IEnumerable<IOperation> operations)
        {
            scsourceEmitterOutput.Write("Total instructions in method: ", true, Color.Red);
            scsourceEmitterOutput.WriteLine(operations.Count().ToString(), Color.Magenta);
        }

        private void PrintTotalGasCost(int gasCount)
        {
            scsourceEmitterOutput.Write($"Total gas cost: ", true, Color.Red);
            scsourceEmitterOutput.WriteLine(gasCount.ToString(), Color.Magenta);
        }
        #endregion

        private string GetLocalName(ILocalDefinition local)
        {
            string localName = local.Name.Value;
            if (this.pdbReader != null)
            {
                foreach (IPrimarySourceLocation psloc in this.pdbReader.GetPrimarySourceLocationsForDefinitionOf(local))
                {
                    if (psloc.Source.Length > 0)
                    {
                        localName = psloc.Source;
                        break;
                    }
                }
            }
            return localName;
        }
        #endregion

        #region Fields
        IMetadataHost host;
        PdbReader? pdbReader;
        bool noIL;
        ///IColorfulSourceEmitterOutput scsourceEmitterOutput;
        SmartContractSourceEmitterOutput scsourceEmitterOutput;
        Regex? classPattern;
        Regex? methodPattern;
        #endregion
    }


    
}
