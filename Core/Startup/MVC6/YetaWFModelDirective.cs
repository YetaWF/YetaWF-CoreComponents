// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Portions /* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
// Copied MVC6 ModelDirective.cs and modified to create a custom YetaWFModelDirective
// original formatting kept to simplify diffs

#if MVC6

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using System.Text.RegularExpressions;

namespace YetaWF2.Support
{
    public static class YetaWFModelDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            "model",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddTypeToken();
                builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            });

        public static IRazorEngineBuilder Register(IRazorEngineBuilder builder)
        {
            builder.AddDirective(Directive);
            builder.Features.Add(new Pass(builder.DesignTime));
            return builder;
        }

        public static string GetModelType(DocumentIntermediateNode document, out bool asis)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var visitor = new Visitor();
            return GetModelType(document, visitor, out asis);
        }

        private static string GetModelType(DocumentIntermediateNode document, Visitor visitor, out bool asis)
        {
            asis = false;
            visitor.Visit(document);

            for (var i = visitor.ModelDirectives.Count - 1 ; i >= 0 ; i--)
            {
                var directive = visitor.ModelDirectives[i];

                var tokens = directive.Tokens.ToArray();
                if (tokens.Length >= 1)
                {
                    return tokens[0].Content;
                }
            }

            if (document.DocumentKind == RazorPageDocumentClassifierPass.RazorPageDocumentKind)
            {
                return visitor.Class.ClassName;
            }
            else
            {
                //$$$MOD
                Match match = reInherits1.Match(visitor.Class.BaseType);
                if (match.Success) {
                    asis = true;
                    return match.Groups["model"].Value;
                } else
                //$$$END-MOD

                return "dynamic";
            }
        }
        //$$$MOD
        private static Regex reInherits1 = new Regex(@"^\s*[a-zA-Z0-9_\.]+\<(\s*[a-zA-Z0-9_\.]+\s*\,){0,1}\s*(?'model'[a-zA-Z0-9_\.\<\> \,]+\?{0,1})\s*\>\s*$", RegexOptions.Multiline);
        //$$$END-MOD

        internal class Pass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
        {
            private readonly bool _designTime;

            public Pass(bool designTime)
            {
                _designTime = designTime;
            }

            // Runs after the @inherits directive
            public override int Order => 5;

            protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
            {
                var visitor = new Visitor();
                bool asis;
                var modelType = GetModelType(documentNode, visitor, out asis);

                if (_designTime)
                {
                    // Alias the TModel token to a known type.
                    // This allows design time compilation to succeed for Razor files where the token isn't replaced.
                    var typeName = $"global::{typeof(object).FullName}";
                    var usingNode = new UsingDirectiveIntermediateNode()
                    {
                        Content = $"TModel = {typeName}"
                    };

                    visitor.Namespace?.Children.Insert(0, usingNode);
                }
                if (asis) return;

                var baseType = visitor.Class?.BaseType?.Replace("<TModel>", "<" + modelType + ">");
                visitor.Class.BaseType = baseType;
            }
        }

        private class Visitor : IntermediateNodeWalker
        {
            public NamespaceDeclarationIntermediateNode Namespace { get; private set; }

            public ClassDeclarationIntermediateNode Class { get; private set; }

            public IList<DirectiveIntermediateNode> ModelDirectives { get; } = new List<DirectiveIntermediateNode>();

            public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
            {
                if (Namespace == null)
                {
                    Namespace = node;
                }

                base.VisitNamespaceDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                if (Class == null)
                {
                    Class = node;
                }

                base.VisitClassDeclaration(node);
            }

            public override void VisitDirective(DirectiveIntermediateNode node)
            {
                if (node.Directive == Directive)
                {
                    ModelDirectives.Add(node);
                }
            }
        }
    }
}

#else
#endif