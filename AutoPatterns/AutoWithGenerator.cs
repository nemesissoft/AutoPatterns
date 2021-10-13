using System.Collections.Generic;
using System.Linq;
using System.Text;

using AutoPatterns.Utils;

using Microsoft.CodeAnalysis;


namespace AutoPatterns
{
    public record AutoWithGeneratorState(IList<MemberMeta> Properties, AutoWithSettings? Settings);

    [Generator]
    public sealed class AutoWithGenerator : AutoAttributeGenerator<AutoWithGeneratorState>
    {
        internal readonly DiagnosticDescriptor InvalidSettingsAttributeRule;
        internal readonly DiagnosticDescriptor BaseTypeNotDecorated;
        internal readonly DiagnosticDescriptor NoContractMembersRule;

        public AutoWithGenerator() : base("With", "AutoWithAttribute", @"using System;
namespace Auto
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    sealed class AutoWithAttribute : Attribute 
    {
        public bool SupportValidation { get; }

        public AutoWithAttribute(bool supportValidation = true) => SupportValidation = supportValidation;
    }
}")
        {
            InvalidSettingsAttributeRule = GetDiagnosticDescriptor(3, AutoPatternName, "Attribute {3} must be constructed with 1 boolean value, or with default values");
            BaseTypeNotDecorated = GetDiagnosticDescriptor(4, AutoPatternName, "Base '{0}' type must also be decorated with {3} attribute");
            NoContractMembersRule = GetDiagnosticDescriptor(50, AutoPatternName, $"No properties for {AutoPatternName} pattern defined at '{{0}}'", DiagnosticSeverity.Warning);
        }

        protected override bool ShouldProcessType(ISymbol typeSymbol, ISymbol autoAttributeSymbol,
            in GeneratorExecutionContext context, out AutoWithGeneratorState? state)
        {
            if (GeneratorUtils.GetAttribute(typeSymbol, autoAttributeSymbol) is { } autoAttributeData)
            {
                if (CommonAutoSettings.TryLoad<AutoWithSettings>(autoAttributeData, context, out var settings))
                {
                    state = new(new List<MemberMeta>(), settings);
                    return true;
                }
                else
                    ReportDiagnostics(context, InvalidSettingsAttributeRule, typeSymbol);
            }

            state = default;
            return false;
        }


        protected override bool ShouldRender(INamedTypeSymbol typeSymbol, INamedTypeSymbol autoAttributeSymbol,
            in GeneratorExecutionContext context, ICollection<Using> namespaces, AutoWithGeneratorState? state)
        {
            if (state is null)
            {
                ReportDiagnostics(context, NullStateRule, typeSymbol);
                return false;
            }

            var propertyList = new List<MemberMeta>();

            void FetchProperties(INamespaceOrTypeSymbol symbol, bool declaredInBase)
            {
                foreach (var ps in symbol.GetMembers().Where(s => s.Kind == SymbolKind.Property).OfType<IPropertySymbol>())
                {
                    propertyList.Add(new(ps.Name, SymbolUtils.GetTypeMinimalName(ps.Type), declaredInBase, ps.IsAbstract));
                    Using.ExtractNamespaces(ps.Type, namespaces);
                }
            }

            var basesDecoratedProperly = true;
            foreach (var baseType in SymbolUtils.GetSymbolHierarchy(typeSymbol))
            {
                if (!ShouldProcessType(baseType, autoAttributeSymbol, context, out _))
                {
                    ReportDiagnostics(context, BaseTypeNotDecorated, baseType);
                    basesDecoratedProperly = false;
                }

                FetchProperties(baseType, true);
            }

            FetchProperties(typeSymbol, false);

            if (propertyList.Count == 0)
            {
                ReportDiagnostics(context, NoContractMembersRule, typeSymbol);
                return false;
            }

            foreach (var prop in propertyList)
                state.Properties.Add(prop);
            return basesDecoratedProperly;
        }


        protected override void Render(StringBuilder source, TypeMeta typeMeta, AutoWithGeneratorState? state)
        {
            var properties = state?.Properties ?? new List<MemberMeta>();
            properties = properties.Where(p => !p.IsAbstract).ToList();

            var settings = state?.Settings ?? new AutoWithSettings(true);

            source.AppendLine($@"
namespace {typeMeta.Namespace}
{{");

            source.Append($@"{INDENT_1}{typeMeta.TypeDefinition} {typeMeta.Name} 
    {{");

            RenderDebuggerHook(source, settings);

            source.Append(@$"
        {(typeMeta.IsAbstract ? "protected" : "public")} {typeMeta.Name}(");

            for (var i = 0; i < properties.Count; i++)
            {
                source.Append(properties[i].Type).Append(" ").Append(properties[i].ParameterName);
                if (i < properties.Count - 1)
                    source.Append(", ");
            }

            source.Append(")");

            if (properties.Where(p => p.DeclaredInBase).ToList() is { Count: > 0 } declaredInBase)
            {
                source.Append(" : base(");

                for (var i = 0; i < declaredInBase.Count; i++)
                {
                    source.Append(declaredInBase[i].ParameterName);
                    if (i < declaredInBase.Count - 1)
                        source.Append(", ");
                }

                source.Append(")");
            }

            source.AppendLine(@"
        {");

            foreach (var p in properties.Where(p => !p.DeclaredInBase))
                source.Append(INDENT_3).Append(p.Name).Append(" = ").Append(p.ParameterName).AppendLine(";");

            if (settings.SupportValidation)
                source.Append(@"
            OnConstructed();").AppendLine();

            source.AppendLine(
@"        }");
            if (settings.SupportValidation)
                source.AppendLine(@"
        partial void OnConstructed();");

            if (!typeMeta.IsAbstract)
                for (var i = 0; i < properties.Count; i++)
                {
                    var p = properties[i];
                    source.Append(@"
        [System.Diagnostics.Contracts.Pure]
        public ").Append(p.DeclaredInBase ? "new " : "").Append(typeMeta.Name)
                        .Append(" With").Append(p.Name)
                        .Append("(").Append(p.Type).Append(" value) => new ")
                        .Append(typeMeta.Name).Append("(");

                    for (var j = 0; j < properties.Count; j++)
                    {
                        source.Append(i == j ? "value" : properties[j].Name);

                        if (j < properties.Count - 1)
                            source.Append(", ");
                    }

                    source.AppendLine(");");
                }


            source.AppendLine(@"    }
}");
        }
    }
}
