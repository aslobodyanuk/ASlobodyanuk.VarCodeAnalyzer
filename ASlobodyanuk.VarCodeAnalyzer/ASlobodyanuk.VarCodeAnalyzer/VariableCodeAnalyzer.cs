using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ASlobodyanuk.VarCodeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VariableCodeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ASVAR";
        private const string Category = "Naming";

        private static readonly LocalizableString Title = GetResource(nameof(Resources.AnalyzerTitle));
        private static readonly LocalizableString MessageFormat = GetResource(nameof(Resources.AnalyzerMessageFormat));
        private static readonly LocalizableString Description = GetResource(nameof(Resources.AnalyzerDescription));

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
                                                                                    Title,
                                                                                    MessageFormat,
                                                                                    Category,
                                                                                    DiagnosticSeverity.Info,
                                                                                    isEnabledByDefault: true,
                                                                                    description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get => ImmutableArray.Create(Rule);
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSemanticModelAction(AnalyzeDocumentSemanticModel);
            //context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.VariableDeclaration);
        }

        private static void AnalyzeDocumentSemanticModel(SemanticModelAnalysisContext context)
        {
            Task.Run(async () =>
            {
                var root = await context.SemanticModel.SyntaxTree.GetRootAsync();
                var variables = root.DescendantNodes().OfType<VariableDeclarationSyntax>();

                Trace.TraceInformation($"{variables.Count()}");

                var tasks = variables.Select(x => AnalyzeVariableNode(x, context.SemanticModel));
                var results = (await Task.WhenAll(tasks)).Where(x => x != null);

                foreach (var diagnostic in results)
                    context.ReportDiagnostic(diagnostic);

            }).Wait();
        }

        private static bool ShouldCreateDiagnostic(VariableDeclarationSyntax variable, SemanticModel semanticModel)
        {
            var typeDeclarationSymbol = semanticModel?.GetSymbolInfo(variable.Type);

            var typeDeclaration = typeDeclarationSymbol?.Symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var variableDeclaration = variable.ToString();

            var result = ShouldBeExplicit(variableDeclaration, typeDeclaration);

            return result;
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var variable = context.Node as VariableDeclarationSyntax;
            
            if (variable != default && ShouldCreateDiagnostic(variable, context.SemanticModel))
            {
                var type = variable.Type;
                var variableName = variable?.Variables.FirstOrDefault()?.Identifier.Text;

                var diagnostic = Diagnostic.Create(Rule, type.GetLocation(), variableName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static async Task<Diagnostic> AnalyzeVariableNode(VariableDeclarationSyntax variable, SemanticModel model)
        {
            return await Task.Run(() =>
            {
                if (variable != default && ShouldCreateDiagnostic(variable, model))
                {
                    var type = variable.Type;
                    var variableName = variable?.Variables.FirstOrDefault()?.Identifier.Text;

                    return Diagnostic.Create(Rule, type.GetLocation(), variableName);
                }

                return null;
            });
        }

        private static bool ShouldBeExplicit(string variableDeclaration, string typeDeclaration)
        {
            //Correct declaration
            if (variableDeclaration.StartsWith(typeDeclaration))
                return false;

            //Generics
            if (variableDeclaration.Contains($"<{typeDeclaration}>"))
                return false;

            //Constructor
            if (variableDeclaration.Contains($"new {typeDeclaration}"))
                return false;

            //Cast
            if (variableDeclaration.Contains($" ({typeDeclaration})"))
                return false;

            //Anonymous types
            if (typeDeclaration.Contains("<anonymous type:"))
                return false;

            //Casting with 'as'
            if (variableDeclaration.Contains($"as {typeDeclaration}"))
                return false;

            return true;
        }

        private static LocalizableResourceString GetResource(string name)
        {
            return new LocalizableResourceString(name, Resources.ResourceManager, typeof(Resources));
        }
    }
}
