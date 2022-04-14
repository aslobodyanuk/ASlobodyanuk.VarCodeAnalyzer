using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ASlobodyanuk.Core
{
    public static class NodeExtensions
    {
        public static SyntaxToken? GetIdentifierToken(this ForEachStatementSyntax node)
        {
            var identifierName = node.Type as SimpleNameSyntax;
            return identifierName?.Identifier;
        }

        public static SyntaxToken? GetIdentifierToken(this VariableDeclarationSyntax node)
        {
            var identifierName = node.Type as SimpleNameSyntax;
            return identifierName?.Identifier;
        }

        public static string GetTypeString(this ForEachStatementSyntax node, SemanticModel semanticModel)
        {
            return GetTypeString(node.Type, semanticModel);
        }

        public static string GetTypeString(this VariableDeclarationSyntax node, SemanticModel semanticModel)
        {
            return GetTypeString(node.Type, semanticModel);
        }

        private static string GetTypeString(TypeSyntax typeSyntax, SemanticModel semanticModel)
        {
            if (typeSyntax == default)
                return null;

            var typeDeclarationSymbol = semanticModel?.GetSymbolInfo(typeSyntax);
            var typeDeclaration = typeDeclarationSymbol?.Symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            return typeDeclaration;
        }

        public static string GetStringRepresentation(this ForEachStatementSyntax node)
        {
            return node.ToString().Replace(node.Statement.ToString(), string.Empty);
        }

        public static string GetStringRepresentation(this VariableDeclarationSyntax node)
        {
            return node.ToString();
        }
    }
}
