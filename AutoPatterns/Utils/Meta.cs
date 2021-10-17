using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoPatterns.Utils
{
    public record TypeMeta(string Name, string Namespace, string TypeDefinition, bool IsAbstract, bool IsSealed)
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
            var isSealed = type.Modifiers.Any(m => m.IsKind(SyntaxKind.SealedKeyword));

            return new(typeSymbol.Name, typeSymbol.ContainingNamespace.ToDisplayString(), typeDefinition, isAbstract, isSealed);
        }
    }

    public abstract record MemberMeta(string Name, string Type)
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

    [Flags]
    public enum PropertyModifiers : byte
    {
        None = 0,
        Abstract = 1,
        Virtual = 2,
        Override = 4
    }

    public sealed record PropertyMeta(string Name, string Type, PropertyModifiers Modifiers) : MemberMeta(Name, Type)
    {
        public bool IsAbstract => (Modifiers & PropertyModifiers.Abstract) > 0;
        public bool IsVirtual => (Modifiers & PropertyModifiers.Virtual) > 0;
        public bool IsOverride => (Modifiers & PropertyModifiers.Override) > 0;


        public static IEnumerable<PropertyMeta> GetProperties(INamespaceOrTypeSymbol symbol, ISet<Using> namespacesCollector)
        {
            foreach (var ps in symbol.GetMembers().Where(s => s.Kind == SymbolKind.Property).OfType<IPropertySymbol>())
            {
                yield return new(ps.Name, SymbolUtils.GetTypeMinimalName(ps.Type),
                    (ps.IsAbstract ? PropertyModifiers.Abstract : 0) |
                    (ps.IsVirtual ? PropertyModifiers.Virtual : 0) |
                    (ps.IsOverride ? PropertyModifiers.Override : 0)
                );
                Using.ExtractNamespaces(ps.Type, namespacesCollector);
            }
        }
    }

    public sealed record FieldMeta(string Name, string Type, bool IsReadonly) : MemberMeta(Name, Type)
    {
        public static IEnumerable<FieldMeta> GetFields(INamespaceOrTypeSymbol symbol, ISet<Using> namespacesCollector)
        {
            foreach (var fs in symbol.GetMembers().Where(s => s.Kind == SymbolKind.Field).OfType<IFieldSymbol>())
            {
                yield return new(fs.Name, SymbolUtils.GetTypeMinimalName(fs.Type), fs.IsReadOnly);
                Using.ExtractNamespaces(fs.Type, namespacesCollector);
            }
        }
    }
}
