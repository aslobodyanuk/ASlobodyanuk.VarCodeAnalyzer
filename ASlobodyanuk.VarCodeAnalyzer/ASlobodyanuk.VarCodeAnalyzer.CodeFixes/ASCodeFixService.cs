using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ASlobodyanuk.VarCodeAnalyzer
{
    public static class ASCodeFixService
    {
        public static async Task<Document> ApplyDiagnosticFixesAsync(IEnumerable<Diagnostic> diagnostics, Document document, CancellationToken cancellationToken)
        {
            var documentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync();

            var nodesToReplace = diagnostics.Select(x => GetDeclaration(x, documentRoot));

            documentRoot = documentRoot.ReplaceNodes(nodesToReplace, (variable, b) => GetReplacementNode(variable, semanticModel));

            return document.WithSyntaxRoot(documentRoot);
        }

        public static async Task<Document> ApplyDiagnosticFixAsync(Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
        {
            var documentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var declaration = GetDeclaration(diagnostic, documentRoot);

            if (declaration == default)
                return document;

            return await ChangeDeclaration(document, declaration, cancellationToken);
        }

        private static async Task<Document> ChangeDeclaration(Document document, VariableDeclarationSyntax variable, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var newVariable = GetReplacementNode(variable, semanticModel);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(variable, newVariable);

            return document.WithSyntaxRoot(newRoot);
        }

        private static VariableDeclarationSyntax GetReplacementNode(VariableDeclarationSyntax variable, SemanticModel semanticModel)
        {
            var typeDeclarationSymbol = semanticModel?.GetSymbolInfo(variable.Type);

            var typeDeclaration = typeDeclarationSymbol?.Symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            var identifierName = variable.Type as SimpleNameSyntax;

            if (identifierName == default)
                return variable;

            var identifierToken = identifierName.Identifier;

            var newToken = SyntaxFactory.Identifier(identifierToken.LeadingTrivia, SyntaxKind.IdentifierToken, typeDeclaration, typeDeclaration, identifierToken.TrailingTrivia);
            var newVariable = variable.ReplaceToken(identifierToken, newToken);

            return newVariable;
        }

        private static VariableDeclarationSyntax GetDeclaration(Diagnostic diagnostic, SyntaxNode documentRoot)
        {
            return documentRoot.FindToken(diagnostic.Location.SourceSpan.End)
                                .Parent
                                .AncestorsAndSelf()
                                .OfType<VariableDeclarationSyntax>()
                                .FirstOrDefault();
        }
    }
}
