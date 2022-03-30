using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace ASlobodyanuk.VarCodeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VariableCodeFixProvider)), Shared]
    public class VariableCodeFixProvider : CodeFixProvider
    {
        private static readonly ASVariableFixAllProvider _fixAllProvider = new ASVariableFixAllProvider();

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get => ImmutableArray.Create(VariableCodeAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider() => _fixAllProvider;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.FirstOrDefault();
            var document = context.Document;

            if (diagnostic == default)
                return Task.CompletedTask;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => ASCodeFixService.ApplyDiagnosticFixAsync(diagnostic, document, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);

            return Task.CompletedTask;
        }
    }
}
