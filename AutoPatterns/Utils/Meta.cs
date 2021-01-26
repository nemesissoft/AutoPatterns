using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#nullable enable

namespace AutoPatterns.Utils
{
    internal class TypeMeta
    {
        public string Name { get; }
        public string Namespace { get; }
        public string TypeDefinition { get; }
        public bool IsAbstract { get; }

        public TypeMeta(string name, string ns, string typeDefinition, bool isAbstract)
        {
            Name = name;
            Namespace = ns;
            TypeDefinition = typeDefinition;
            IsAbstract = isAbstract;
        }

        public void Deconstruct(out string name, out string ns, out string typeDefinition, out bool isAbstract)
        {
            name = Name;
            ns = Namespace;
            typeDefinition = TypeDefinition;
            isAbstract = IsAbstract;
        }

        public static TypeMeta FromDeclaration(TypeDeclarationSyntax type, INamedTypeSymbol typeSymbol)
        {
            var typeDefinition = type.Modifiers + " " + type switch
            {
                ClassDeclarationSyntax => "class",
                StructDeclarationSyntax => "struct",
                RecordDeclarationSyntax => "record",
                _ => throw new NotSupportedException("Only class, struct or record types are allowed")
            };
            bool isAbstract = type.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword));

            return new(typeSymbol.Name, typeSymbol.ContainingNamespace.ToDisplayString(), typeDefinition, isAbstract);
        }
    }

    internal class MemberMeta
    {
        public string Name { get; }
        public string Type { get; }
        public bool DeclaredInBase { get; }
        public string ParameterName { get; }

        public MemberMeta(string name, string type, bool declaredInBase)
        {
            Name = name;
            Type = type;
            DeclaredInBase = declaredInBase;

            var parameterName = char.ToLower(name[0]) + name.Substring(1);
            if (CSharpKeyword.Is(parameterName))
                parameterName = "@" + parameterName;
            ParameterName = parameterName;
        }
    }
}
