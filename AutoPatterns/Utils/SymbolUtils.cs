using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace AutoPatterns.Utils
{

    internal static class SymbolUtils
    {
        public static IEnumerable<INamedTypeSymbol> GetSymbolHierarchy(INamedTypeSymbol symbol)
        {
            while (symbol.BaseType != null)
            {
                var @base = symbol.BaseType;

                if (@base.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
                    break;

                yield return @base;
                symbol = @base;
            } }

        /*public static IEnumerable<INamedTypeSymbol> GetSymbolHierarchyWithSelf(INamedTypeSymbol symbol)
        {
            yield return symbol;
            foreach (var s in GetSymbolHierarchy(symbol))
                yield return s;
        }*/

        public static string GetTypeMinimalName(ISymbol ts) =>
            ts.ContainingType is { } containingType
                ? $"{containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}.{ts.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}"
                : ts.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }
}
