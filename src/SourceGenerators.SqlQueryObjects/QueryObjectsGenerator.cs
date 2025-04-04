using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Scriban;

namespace IOKode.OpinionatedFramework.SourceGenerators.SqlQueryObjects;

[Generator]
public partial class QueryObjectsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var rootNamespaceProvider = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider)
            .Select((pair, _) =>
            {
                var (compilation, options) = pair;

                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                options.GlobalOptions.TryGetValue("build_property.projectdir", out var rootPath);

                var configOptions = new ConfigOptions
                {
                    RootNamespace = rootNamespace ?? compilation.AssemblyName ?? compilation.GlobalNamespace.Name,
                    RootPath = rootPath ?? string.Empty,
                };
                return configOptions;
            });

        var provider = context.AdditionalTextsProvider
            .Where(file => Path.GetFileName(file.Path).EndsWith(".sql"))
            .Where(file => Regex.IsMatch(file.GetText()?.ToString() ?? string.Empty, @"--\s*@generate"))
            .Collect()
            .Combine(rootNamespaceProvider);

        context.RegisterSourceOutput(provider, GenerateCode);
    }

    private void GenerateCode(SourceProductionContext context, (ImmutableArray<AdditionalText>, ConfigOptions) pair)
    {
        var (files, configOptions) = pair;
        var sqlFiles = files.Select(file => new SqlFile
        {
            FilePath = file.Path,
            Content = file.GetText()!.ToString()
        });
        foreach (var file in sqlFiles)
        {
            var queryObjectClass = new QueryObjectClass(file, configOptions);
            var queryObjectClassRender = Template.Parse(_QueryObjectClassTemplate).Render(queryObjectClass, member => member.Name);
            context.AddSource(queryObjectClass.FileName, SourceText.From(queryObjectClassRender, Encoding.UTF8));
        }
    }
}