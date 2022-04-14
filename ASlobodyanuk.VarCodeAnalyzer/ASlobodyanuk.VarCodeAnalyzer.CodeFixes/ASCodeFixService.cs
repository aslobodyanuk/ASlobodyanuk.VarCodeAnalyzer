using ASlobodyanuk.Core;
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

        private static async Task<Document> ChangeDeclaration<TNode>(Document document, TNode node, CancellationToken cancellationToken) where TNode : CSharpSyntaxNode
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var newNode = GetReplacementNode(node, semanticModel);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(node, newNode);

            return document.WithSyntaxRoot(newRoot);
        }

        private static VariableDeclarationSyntax GetReplacementNode(VariableDeclarationSyntax variable, SemanticModel semanticModel)
        {
            var typeDeclaration = variable.GetTypeString(semanticModel);
            var identifierToken = variable.GetIdentifierToken();

            return CreateNewNode(variable, typeDeclaration, identifierToken);
        }

        private static ForEachStatementSyntax GetReplacementNode(ForEachStatementSyntax statement, SemanticModel semanticModel)
        {
            var typeDeclaration = statement.GetTypeString(semanticModel);
            var identifierToken = statement.GetIdentifierToken();

            return CreateNewNode(statement, typeDeclaration, identifierToken);
        }

        private static T CreateNewNode<T>(T node, string typeDeclaration, SyntaxToken? identifierToken) where T : CSharpSyntaxNode
        {
            if (identifierToken == default)
                return node;

            var newToken = SyntaxFactory.Identifier(identifierToken.Value.LeadingTrivia, SyntaxKind.IdentifierToken, typeDeclaration, typeDeclaration, identifierToken.Value.TrailingTrivia);
            var newVariable = node.ReplaceToken(identifierToken.Value, newToken);

            return newVariable;
        }

        private static TNode GetReplacementNode<TNode>(TNode node, SemanticModel semanticModel) where TNode : CSharpSyntaxNode
        {
            if (node is VariableDeclarationSyntax)
                return GetReplacementNode(node as VariableDeclarationSyntax, semanticModel) as TNode;

            if (node is ForEachStatementSyntax)
                return GetReplacementNode(node as ForEachStatementSyntax, semanticModel) as TNode;

            return node;
        }

        private static CSharpSyntaxNode GetDeclaration(Diagnostic diagnostic, SyntaxNode documentRoot)
        {
            var variable = documentRoot.FindToken(diagnostic.Location.SourceSpan.End)
                                .Parent
                                .AncestorsAndSelf()
                                .OfType<VariableDeclarationSyntax>()
                                .FirstOrDefault();

            if (variable != default)
                return variable;

            var foreachNode = documentRoot.FindToken(diagnostic.Location.SourceSpan.End)
                                .Parent
                                .AncestorsAndSelf()
                                .OfType<ForEachStatementSyntax>()
                                .FirstOrDefault();

            return foreachNode;
        }
    }
}
