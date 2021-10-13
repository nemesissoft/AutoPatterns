using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoPatterns.Utils
{
    public record TypeMeta(string Name, string Namespace, string TypeDefinition, bool IsAbstract)
    {
        public static TypeMeta FromDeclaration(TypeDeclarationSyntax type, INamedTypeSymbol typeSymbol)
        {
            var typeDefinition = type.Modifiers + " " + type switch
            {
                ClassDeclarationSyntax => "class",
                StructDeclarationSyntax => "struct",
                RecordDeclarationSyntax => "record",
                _ => throw new NotSupportedException("Only class, struct or record types are allowed")
            };
            var isAbstract = type.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword));

            return new(typeSymbol.Name, typeSymbol.ContainingNamespace.ToDisplayString(), typeDefinition, isAbstract);
        }
    }

    public record MemberMeta(string Name, string Type, bool DeclaredInBase, bool IsAbstract)
    {
        private string? _parameterName;
        public string ParameterName => _parameterName ??= GetParameterName(Name);

        private static string GetParameterName(string name)
        {
            var parameterName = char.ToLower(name[0]) + name.Substring(1);
            if (CSharpKeyword.Is(parameterName))
                parameterName = "@" + parameterName;
            return parameterName;
        }
    }
}
