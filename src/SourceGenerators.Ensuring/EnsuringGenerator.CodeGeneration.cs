﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using IOKode.OpinionatedFramework.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;

namespace IOKode.OpinionatedFramework.SourceGenerators.Ensuring;

internal partial class EnsuringGenerator
{
    private class _Ensurer
    {
        public string EnsurerClassName { get; set; }
        public string EnsurerClassNamespace { get; set; }
        public _EnsurerMethod[] Methods { get; set; }

        public string Name
        {
            get
            {
                var ensurerClassNameBuilder = new StringBuilder(EnsurerClassName);
                if (EnsurerClassName.EndsWith("Ensurer"))
                {
                    ensurerClassNameBuilder.Length -= "Ensurer".Length;
                }

                return ensurerClassNameBuilder.ToString();
            }
        }

        public string EnsurerThrowerClassName => $"{Name}EnsurerThrower";
    }

    private class _EnsurerMethod
    {
        public string Name { get; set; }
        public IEnumerable<_EnsurerMethodParameter> Parameters { get; set; }
        public string DocComment { get; set; }
    }

    private class _EnsurerMethodParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    private static readonly string _EnsurerAttribute = "IOKode.OpinionatedFramework.Ensuring.EnsurerAttribute";
    
    /// <summary>
    /// Get the relevant information of each class for code generation.
    /// </summary>
    private static IEnumerable<_Ensurer> _GetEnsurers(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, CancellationToken cancellationToken)
    {
        foreach (var classDeclarationSyntax in classes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ensurerClassName = classDeclarationSyntax.Identifier.Text;
            var ensurerClassNamespace = SourceGenerationHelper.GetNamespace(classDeclarationSyntax);

            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            var methods = _GetMethodsFromPartialClass(classDeclarationSyntax, semanticModel)
                .Where(method => method is not null)
                .ToArray();

            var ensurer = new _Ensurer
            {
                EnsurerClassName = ensurerClassName,
                EnsurerClassNamespace = ensurerClassNamespace,
                Methods = methods
            };
            yield return ensurer;
        }
    }

    private static _EnsurerMethod? _GetEnsurerMethod(MethodDeclarationSyntax methodDeclarationSyntax, SemanticModel semanticModel)
    {
        var methodSymbol = (IMethodSymbol) semanticModel.GetDeclaredSymbol(methodDeclarationSyntax)!;
        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        var boolTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName(typeof(bool).FullName!);
        if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, boolTypeSymbol))
        {
            return null;
        }

        var methodName = methodDeclarationSyntax.Identifier.Text;
        var docComment = SourceGenerationHelper.GetMethodDocComment(methodSymbol);
        var methodParameters = methodDeclarationSyntax.ParameterList.Parameters
            .Select(parameterSyntax => (IParameterSymbol) semanticModel.GetDeclaredSymbol(parameterSyntax))
            .Where(parameterSymbol => parameterSymbol is not null)
            .Select(parameterSymbol => new _EnsurerMethodParameter
            {
                Name = parameterSymbol.Name,
                Type = parameterSymbol.Type.ToString()
            });

        var method = new _EnsurerMethod
        {
            Name = methodName,
            Parameters = methodParameters,
            DocComment = docComment
        };
        return method;
    }

    private static IEnumerable<_EnsurerMethod> _GetMethodsFromPartialClass(ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
    {
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (classSymbol is null)
        {
            yield break;
        }

        var partialParts = classSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>();

        foreach (var part in partialParts)
        {
            foreach (var method in part.Members.OfType<MethodDeclarationSyntax>())
            {
                var partSemanticModel = semanticModel.Compilation.GetSemanticModel(part.SyntaxTree);
                var ensurerMethod = _GetEnsurerMethod(method, partSemanticModel);
                if (ensurerMethod != null)
                {
                    yield return ensurerMethod;
                }
            }
        }
    }

    private static string _GenerateEnsurerThrowerClass(_Ensurer ensurer)
    {
        return Template.Parse(_EnsurerThrowerClassTemplate).Render(ensurer, member => member.Name);
    }

    private static string _GenerateEnsureClass(IEnumerable<_Ensurer> ensurers)
    {
        var script = new ScriptObject {{ "Ensurers", ensurers }};
        var templateContext = new TemplateContext(script)
        {
            MemberRenamer = member => member.Name
        };
        return Template.Parse(_EnsureClassTemplate).Render(templateContext);
    }

    private static readonly string _EnsurerThrowerClassTemplate = 
        """
        // This file was auto-generated by a source generator

        #nullable enable
        using System;
        using {{ EnsurerClassNamespace }};

        namespace IOKode.OpinionatedFramework.Ensuring;

        public class {{ EnsurerThrowerClassName }}
        {
            {{~ for method in Methods ~}}
            {{~ if method.DocComment ~}}
            {{ method.DocComment }}
            {{~ end ~}}
            public Thrower {{ method.Name }}({{ for parameter in method.Parameters }}{{ parameter.Type }} {{ parameter.Name }}{{ if !for.last }}, {{ end }}{{ end }})
            {
                bool isValid = {{ EnsurerClassName }}.{{ method.Name }}({{ for parameter in method.Parameters }}{{ parameter.Name }}{{ if !for.last }}, {{ end }}{{ end }});
                return new(isValid);
            }
            {{~ if !for.last ~}}

            {{~ end ~}}
            {{~ end ~}}
        }
        """;

    private static readonly string _EnsureClassTemplate =
        """
        // This file was auto-generated by a source generator

        namespace IOKode.OpinionatedFramework.Ensuring;

        public static partial class Ensure
        {
            {{~ for ensurer in Ensurers ~}}
            public static {{ ensurer.EnsurerThrowerClassName }} {{ ensurer.Name }} => new {{ ensurer.EnsurerThrowerClassName }}();
            {{~ end ~}}
        }
        """;
}