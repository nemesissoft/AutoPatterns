using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace AutoPatterns.Utils
{

    internal static class SymbolUtils
    {
        public static IEnumerable<INamedTypeSymbol> GetSymbolHierarchy(ITypeSymbol symbol)
        {
            var result = new List<INamedTypeSymbol>();

            while (symbol.BaseType != null)
            {
                var @base = symbol.BaseType;

                if (@base.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
                    break;

                result.Insert(0, @base);
                symbol = @base;
            }

            return result;
        }

        public static string GetTypeMinimalName(ISymbol ts) =>
            ts.ContainingType is { } containingType
                ? $"{containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}.{ts.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}"
                : ts.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }
}
