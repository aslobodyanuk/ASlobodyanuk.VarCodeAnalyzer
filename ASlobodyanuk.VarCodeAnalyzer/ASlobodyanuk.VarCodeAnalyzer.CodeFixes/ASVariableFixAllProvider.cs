using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ASlobodyanuk.VarCodeAnalyzer
{
    public class ASVariableFixAllProvider : FixAllProvider
    {
        public override IEnumerable<string> GetSupportedFixAllDiagnosticIds(CodeFixProvider originalCodeFixProvider) => new List<string>()
            {
                VariableCodeAnalyzer.DiagnosticId
            };

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() => new List<FixAllScope>()
            {
                FixAllScope.Document,
                FixAllScope.Solution
            };

        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            if (fixAllContext.Scope == FixAllScope.Document)
            {
                return Task.FromResult(
                            CodeAction.Create(
                                title: CodeFixResources.CodeFixTitle,
                                createChangedDocument: c => ProcessDocumentAsync(fixAllContext, fixAllContext.Document, c),
                                equivalenceKey: nameof(CodeFixResources.CodeFixTitle)));
            }

            return Task.FromResult(
                        CodeAction.Create(
                            title: CodeFixResources.CodeFixTitle,
                            createChangedSolution: c => ChangeSolution(fixAllContext.Solution, fixAllContext, c),
                            equivalenceKey: nameof(CodeFixResources.CodeFixTitle)));
        }

        private async Task<Document> ProcessDocumentAsync(FixAllContext context, Document document, CancellationToken cancellationToken)
        {
            var diagnostics = (await context.WithCancellationToken(cancellationToken).GetDocumentDiagnosticsAsync(document))
                                        .Where(x => x.Id == VariableCodeAnalyzer.DiagnosticId);

            return await ASCodeFixService.ApplyDiagnosticFixesAsync(diagnostics, document, cancellationToken);
        }

        private async Task<Solution> ChangeSolution(Solution solution, FixAllContext context, CancellationToken cancellationToken)
        {
            var documents = solution.Projects.SelectMany(x => x.Documents)
                                            .Where(x => Path.GetExtension(x.FilePath) == ".cs" && IsProjectOutputDocument(x) == false)
                                            .ToList();

            var updatedSolution = solution;

            var tasks = documents.Select(async (document) =>
            {
                var updatedDocument = await ProcessDocumentAsync(context, document, cancellationToken);
                var syntaxRoot = await updatedDocument.GetSyntaxRootAsync(cancellationToken);

                return (Document: updatedDocument, SyntaxRoot: syntaxRoot);
            });
            var updatedDocuments = await Task.WhenAll(tasks);

            foreach (var document in updatedDocuments)
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(document.Document.Id, document.SyntaxRoot);

            return updatedSolution;
        }

        private bool IsProjectOutputDocument(Document document)
        {
            var projectFolder = Directory.GetParent(document.Project.FilePath);
            var objPath = projectFolder + "\\obj";
            var binPath = projectFolder + "\\bin";

            return document.FilePath.Contains(objPath) || document.FilePath.Contains(binPath);
        }
    }
}
