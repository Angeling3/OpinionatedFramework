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
// #if SHOULD_ATTACH_DEBUGGER
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
// #endif

        var manifestDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider
            (
                static (syntaxNode, _) => _IsSyntaxTargetForGeneration(syntaxNode),
                static (context, _) => _GetSemanticTargetForGeneration(context)
            )
            .Where(static declarationSyntax => declarationSyntax is not null);

        IncrementalValueProvider<(Compilation Compilation, ImmutableArray<ClassDeclarationSyntax> Declarations)>
            compilationAndClasses =
                context.CompilationProvider.Combine(manifestDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses, static (sourceProductionContext, source) =>
            Execute(source.Compilation, source.Declarations.FirstOrDefault(), sourceProductionContext));
    }
    
    /// <summary>
    /// Generate the files with the generated code and adds them to the context.
    /// </summary>
    private static void Execute(Compilation compilation, ClassDeclarationSyntax manifestClassDeclarationSyntax, SourceProductionContext context)
    {
        var commands = _GetCommands(compilation, manifestClassDeclarationSyntax, context.CancellationToken)
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
        var manifestClassDeclarationSyntax = (ClassDeclarationSyntax) context.Node;

        var manifestClassSymbol = (INamedTypeSymbol) context.SemanticModel.GetDeclaredSymbol(manifestClassDeclarationSyntax)!;

        var hasInterface = manifestClassSymbol.Interfaces
            .Any(interfaceSymbol => interfaceSymbol.Name is "ICommandControllersManifest" or "ICommandController"); // todo ensure correct name
        if (!hasInterface)
        {
            return null;
        }

        return manifestClassDeclarationSyntax;
    }
}