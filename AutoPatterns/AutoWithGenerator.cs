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
        private readonly DiagnosticDescriptor _invalidSettingsAttributeRule;
        private readonly DiagnosticDescriptor _baseTypeNotDecorated;
        private readonly DiagnosticDescriptor _noContractMembersRule;

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
            _invalidSettingsAttributeRule = GetDiagnosticDescriptor(3, AutoPatternName, "Attribute {3} must be constructed with 1 boolean value, or with default values");
            _baseTypeNotDecorated = GetDiagnosticDescriptor(4, AutoPatternName, "Base '{0}' type must also be decorated with {3} attribute");
            _noContractMembersRule = GetDiagnosticDescriptor(50, AutoPatternName, $"No properties for {AutoPatternName} pattern defined at '{{0}}'", DiagnosticSeverity.Warning);
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
                    ReportDiagnostics(context, _invalidSettingsAttributeRule, typeSymbol);
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
                    propertyList.Add(new(ps.Name, SymbolUtils.GetTypeMinimalName(ps.Type), declaredInBase));
                    Using.ExtractNamespaces(ps.Type, namespaces);
                }
            }

            var basesDecoratedProperly = true;
            foreach (var baseType in SymbolUtils.GetSymbolHierarchy(typeSymbol))
            {
                if (!ShouldProcessType(baseType, autoAttributeSymbol, context, out _))
                {
                    ReportDiagnostics(context, _baseTypeNotDecorated, baseType);
                    basesDecoratedProperly = false;
                }

                FetchProperties(baseType, true);
            }

            FetchProperties(typeSymbol, false);

            if (propertyList.Count == 0)
            {
                ReportDiagnostics(context, _noContractMembersRule, typeSymbol);
                return false;
            }

            foreach (var prop in propertyList)
                state.Properties.Add(prop);
            return basesDecoratedProperly;
        }


        protected override void Render(StringBuilder source, TypeMeta meta, AutoWithGeneratorState? state)
        {
            var properties = state?.Properties ?? new List<MemberMeta>();
            var settings = state?.Settings ?? new AutoWithSettings(true);

            source.Append($@"
namespace {meta.Namespace}
{{");
            
            source.Append($@"{INDENT_1}{meta.TypeDefinition} {meta.Name} 
    {{");

            RenderDebuggerHook(source, settings);

            source.Append(@$"
        {(meta.IsAbstract ? "protected" : "public")} {meta.Name}(");


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
                source.Append("            ").Append(p.Name).Append(" = ").Append(p.ParameterName).AppendLine(";");

            if (settings.SupportValidation)
                source.Append(@"
            OnConstructed();").AppendLine();

            source.AppendLine(
@"        }");
            if (settings.SupportValidation)
                source.AppendLine(@"
        partial void OnConstructed();");


            for (var i = 0; i < properties.Count; i++)
            {
                var p = properties[i];
                source.Append(@"
        [System.Diagnostics.Contracts.Pure]
        public ").Append(p.DeclaredInBase ? "new " : "").Append(meta.Name)
                    .Append(" With").Append(p.Name)
                    .Append("(").Append(p.Type).Append(" value) => new ")
                    .Append(meta.Name).Append("(");

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
