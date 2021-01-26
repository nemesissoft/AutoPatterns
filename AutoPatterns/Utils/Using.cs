using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoPatterns.Utils
{
    internal sealed class Using : IEquatable<Using>
    {
        public string NamespaceOrType { get; }
        public string? Alias { get; }
        public bool UsingStatic { get; }

        public Using(string namespaceOrType, string? alias = null, bool usingStatic = false)
        {
            NamespaceOrType = namespaceOrType;
            Alias = alias;
            UsingStatic = usingStatic;
        }

        public static implicit operator Using(string s) => new(s);

        public static ISet<Using> FromCompilationUnit(CompilationUnitSyntax compilationUnit)
        {
            var result = new HashSet<Using>();

            foreach (var u in compilationUnit.Usings)
                result.Add(new Using(u.Name.ToString(), u.Alias?.ToString(), !u.StaticKeyword.IsKind(SyntaxKind.None)));

            return result;
        }

        public static void ExtractNamespaces(ITypeSymbol typeSymbol, ICollection<Using> namespaces)
        {
            if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType) //namedType.TypeParameters for unbound generics
            {
                namespaces.Add(namedType.ContainingNamespace.ToDisplayString());

                foreach (var arg in namedType.TypeArguments)
                    ExtractNamespaces(arg, namespaces);
            }
            else if (typeSymbol is IArrayTypeSymbol arraySymbol)
            {
                namespaces.Add("System");

                ITypeSymbol elementSymbol = arraySymbol.ElementType;
                while (elementSymbol is IArrayTypeSymbol innerArray)
                    elementSymbol = innerArray.ElementType;

                ExtractNamespaces(elementSymbol, namespaces);
            }
            /*else if (typeSymbol.TypeKind == TypeKind.Error || typeSymbol.TypeKind == TypeKind.Dynamic)
            {
                //add appropriate reference to your compilation 
            }*/
            else
                namespaces.Add(typeSymbol.ContainingNamespace.ToDisplayString());
        }

        public string ToCSharpCode() => $"using {(UsingStatic ? "static " : "")}{(string.IsNullOrWhiteSpace(Alias) ? "" : $"{Alias} = ")}{NamespaceOrType};";

        public override string ToString() => ToCSharpCode();

        public static IEnumerable<Using> Sort(IEnumerable<Using> collection)
            => collection.OrderBy(x => x.UsingStatic ? 1 : x.Alias == null ? 0 : 2)
                .ThenBy(x => x.Alias?.ToString())
                .ThenByDescending(x => x.NamespaceOrType.StartsWith("System"))
                .ThenBy(x => x.NamespaceOrType);

        #region Equality
        public bool Equals(Using? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return NamespaceOrType == other.NamespaceOrType && Alias == other.Alias && UsingStatic == other.UsingStatic;
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Using other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = NamespaceOrType.GetHashCode();
                hashCode = (hashCode * 397) ^ (Alias != null ? Alias.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ UsingStatic.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Using? left, Using? right) => Equals(left, right);

        public static bool operator !=(Using? left, Using? right) => !Equals(left, right);
        #endregion
    }
}
