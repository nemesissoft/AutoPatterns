﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nemesis.CodeAnalysis;
using NUnit.Framework;

namespace AutoPatterns.Tests
{
    internal static class Utils
    {
        public static Compilation CreateCompilation(string source, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        {
            var (compilation, _, _) = CompilationUtils.CreateTestCompilation(source, new[]
                {
                    typeof(BigInteger).GetTypeInfo().Assembly,
                }, outputKind);

            return compilation;
        }


        private const RegexOptions REGEX_OPTIONS = RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled;
        private static readonly Regex _headerPattern = new(@"/\*\s*<auto-generated>   .+?   </auto-generated>\s*\*/  \s*", REGEX_OPTIONS);


        public static string ScrubGeneratorComments(string text)
        {
            text = _headerPattern.Replace(text, "");
            return text;
        }

        public static IReadOnlyList<string> GetGeneratedTreesOnly<TSourceGenerator>(Compilation compilation, int requiredCardinality = 1)
            where TSourceGenerator: AutoAttributeGenerator, new()
        {
            var generator = new TSourceGenerator();
            var newComp = CompilationUtils.RunGenerators(compilation, out var diagnostics, generator);
            Assert.That(diagnostics, Is.Empty);

            SyntaxTree attributeTree = null;
            foreach (var tree in newComp.SyntaxTrees)
            {
                var attributeDeclaration = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(cds => string.Equals(cds.Identifier.ValueText, generator.AutoAttributeName, StringComparison.Ordinal));
                if (attributeDeclaration != null)
                {
                    attributeTree = tree;
                    break;
                }
            }
            Assert.That(attributeTree, Is.Not.Null, $"Auto attribute for '{generator.AutoPatternName}' pattern not found among generated trees");

            var toRemove = compilation.SyntaxTrees.Append(attributeTree);

            var generatedTrees = newComp.RemoveSyntaxTrees(toRemove).SyntaxTrees.ToList();
            Assert.That(generatedTrees, Has.Count.EqualTo(requiredCardinality), "Generated trees");

            return generatedTrees.Select(tree =>
                ((CompilationUnitSyntax)tree.GetRoot())
                .ToFullString()).ToList();
        }
    }


    internal class IgnoreNewLinesComparer : IComparer<string>, IEqualityComparer<string>
    {
        public static readonly IComparer<string> Comparer = new IgnoreNewLinesComparer();

        public static readonly IEqualityComparer<string> EqualityComparer = new IgnoreNewLinesComparer();

        public int Compare(string x, string y) => string.CompareOrdinal(NormalizeNewLines(x), NormalizeNewLines(y));

        public bool Equals(string x, string y) => NormalizeNewLines(x) == NormalizeNewLines(y);

        public int GetHashCode(string s) => NormalizeNewLines(s)?.GetHashCode() ?? 0;

        public static string NormalizeNewLines(string s) => s?
            .Replace(Environment.NewLine, "")
            .Replace("\n", "")
            .Replace("\r", "");
    }
}
