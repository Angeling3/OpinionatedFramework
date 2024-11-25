using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace IOKode.OpinionatedFramework.SourceGenerators.AspNetCoreIntegrations.Controllers;

[Generator]
internal partial class ControllersGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if SHOULD_ATTACH_DEBUGGER
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
#endif

        var s = context. AnalyzerConfigOptionsProvider.Select((options, _) =>
        {
            options.GlobalOptions.TryGetValue("build_property.FilteredProjectReferences", out var value);
            var references = new HashSet<string>();
            references.Add(value);
            return references;
        });
        var manifestDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider
            (
                static (syntaxNode, _) => _IsSyntaxTargetForGeneration(syntaxNode),
                static (context, _) => _GetSemanticTargetForGeneration(context)
            )
            .Where(static declarationSyntax => declarationSyntax is not null);

        IncrementalValueProvider<((Compilation Compilation, ImmutableArray<ClassDeclarationSyntax> Declarations), HashSet<string> Options)>
            compilationAndClasses =
                context.CompilationProvider.Combine(manifestDeclarations.Collect()).Combine(s);

        context.RegisterSourceOutput(compilationAndClasses, static (sourceProductionContext, source) =>
            Execute(source.Item1.Compilation, source.Item1.Declarations, source.Options, sourceProductionContext));
    }
    
    /// <summary>
    /// Generate the files with the generated code and adds them to the context.
    /// </summary>
    private static void Execute(Compilation compilation, IEnumerable<ClassDeclarationSyntax> manifestClassDeclarationSyntax, HashSet<string> options, SourceProductionContext context)
    {
        var commands = _GetControllers(compilation, manifestClassDeclarationSyntax, options, context.CancellationToken)
            .ToList();

        if (commands.Count <= 0)
        {
            return;
        }

        foreach (var command in commands)
        {
            string controllerClass = _GenerateControllerClass(command);
            context.AddSource($"{command.ControllerClassName}.g.cs", SourceText.From(controllerClass, Encoding.UTF8));
        }
    }

    /// <summary>
    /// Filter classes at the syntactic level for code generation.
    /// </summary>
    private static bool _IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclarationSyntax; // todo should check interfaces?
    }

    /// <summary>
    /// Filter classes at the semantic level for code generation.
    /// </summary>
    private static ClassDeclarationSyntax _GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var controllerClassDeclarationSyntax = (ClassDeclarationSyntax) context.Node;

        var controllerClassSymbol = (INamedTypeSymbol) context.SemanticModel.GetDeclaredSymbol(controllerClassDeclarationSyntax)!;

        var hasInterface = controllerClassSymbol.Interfaces
            .Any(interfaceSymbol => interfaceSymbol.Name.StartsWith("ICommandController")); // todo ensure correct name
        if (!hasInterface)
        {
            return null;
        }

        return controllerClassDeclarationSyntax;
    }
}