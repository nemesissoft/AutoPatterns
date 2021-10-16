using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using AutoPatterns.Utils;

using Microsoft.CodeAnalysis;


namespace AutoPatterns
{
    public record AutoWithGeneratorState(
        IReadOnlyList<PropertyMeta> DeclaredInTypeProperties,
        IReadOnlyList<PropertyMeta> DeclaredOutsideProperties,
        IReadOnlyList<PropertyMeta> AllNonAbstractProperties);

    [Generator]
    public sealed class AutoWithGenerator : AutoAttributeGenerator<AutoWithGeneratorState, AutoWithSettings>
    {
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
}", "Attribute {3} must be constructed with 1 boolean value, or with default values")
        {
            BaseTypeNotDecorated = GetDiagnosticDescriptor(4, AutoPatternName, "Base '{0}' type must also be decorated with {3} attribute");
            NoContractMembersRule = GetDiagnosticDescriptor(50, AutoPatternName, $"No non-abstract properties for {AutoPatternName} pattern defined at '{{0}}'", DiagnosticSeverity.Warning);
        }

        protected override bool ShouldRender(INamedTypeSymbol typeSymbol, INamedTypeSymbol autoAttributeSymbol,
            in GeneratorExecutionContext context, ISet<Using> namespaces, AutoWithSettings settings,
            [NotNullWhen(true)] out AutoWithGeneratorState? state)
        {
            var declaredInTypeProperties = PropertyMeta.GetProperties(typeSymbol, namespaces)
                .Where(p => !p.IsAbstract).ToList();

            var allNonAbstractProperties = declaredInTypeProperties.ToList();

            var declaredOutsideProperties = new List<PropertyMeta>();

            var basesDecoratedProperly = true;
            foreach (var baseType in SymbolUtils.GetSymbolHierarchy(typeSymbol))
            {
                if (!ShouldProcessType(baseType, autoAttributeSymbol, context, out _))
                {
                    ReportDiagnostics(context, BaseTypeNotDecorated, baseType);
                    basesDecoratedProperly = false;
                }
                foreach (var pm in PropertyMeta.GetProperties(baseType, namespaces))
                    if (!pm.IsAbstract)
                    {
                        declaredOutsideProperties.Add(pm);
                        allNonAbstractProperties.Add(pm);
                    }
            }

            state = new(declaredInTypeProperties, declaredOutsideProperties, allNonAbstractProperties);

            var nonAbstractPropertiesCount = declaredInTypeProperties.Count + declaredOutsideProperties.Count;

            if (nonAbstractPropertiesCount > 0) return basesDecoratedProperly;

            ReportDiagnostics(context, NoContractMembersRule, typeSymbol);
            return false;
        }

        protected override void Render(StringBuilder source, TypeMeta typeMeta, AutoWithSettings settings, AutoWithGeneratorState state)
        {
            source.AppendLine($@"
namespace {typeMeta.Namespace}
{{");

            source.Append($@"{INDENT_1}{typeMeta.TypeDefinition} {typeMeta.Name} 
    {{");

            RenderDebuggerHook(source, settings);

            source.Append(@$"
        {(typeMeta.IsAbstract ? "protected" : "public")} {typeMeta.Name}(");

            var allProperties = state.AllNonAbstractProperties;
            //TODO //pomiń overridy wirtualnych propertów + overridy abstraktów nadpisanych wyżej 

            for (var i = 0; i < allProperties.Count; i++)
            {
                source.Append(allProperties[i].Type).Append(" ").Append(allProperties[i].ParameterName);
                if (i < allProperties.Count - 1)
                    source.Append(", ");
            }

            source.Append(")");

            var declaredOutside = state.DeclaredOutsideProperties;

            if (declaredOutside.Count > 0)
            {
                source.Append(" : base(");

                for (var i = 0; i < declaredOutside.Count; i++)
                {
                    source.Append(declaredOutside[i].ParameterName);
                    if (i < declaredOutside.Count - 1)
                        source.Append(", ");
                }

                source.Append(")");
            }

            source.AppendLine(@"
        {");

            var declaredInType = state.DeclaredInTypeProperties;

            foreach (var p in declaredInType)
                source.Append(INDENT_3).Append("this.").Append(p.Name).Append(" = ").Append(p.ParameterName).AppendLine(";");

            if (settings.SupportValidation)
                source.Append(@"
            OnConstructed();").AppendLine();

            source.AppendLine(
@"        }");
            if (settings.SupportValidation)
                source.AppendLine(@"
        partial void OnConstructed();");

            /*if (!typeMeta.IsAbstract)
            {
                RenderWithers(source, typeMeta, declaredInType, allProperties, true);
                RenderWithers(source, typeMeta, declaredOutside, allProperties, false);
            }
            else
            {
                RenderAbstractWithersDeclaration(source, typeMeta, declaredInType);
            }*/


            source.AppendLine(@"    }
}");
        }

        private static void RenderAbstractWithersDeclaration(StringBuilder source, TypeMeta typeMeta, IEnumerable<PropertyMeta> declaredInType)
        {
            foreach (var prop in declaredInType)
            {
                source.Append(@"        
        public abstract ").Append(typeMeta.Name)
                    .Append(" With").Append(prop.Name)
                    .Append("(").Append(prop.Type).AppendLine(" value);");
            }
        }

        private static void RenderWithers(StringBuilder source, TypeMeta typeMeta,
            IEnumerable<PropertyMeta> properties,
            IReadOnlyList<PropertyMeta> allProperties,
            bool declaredInType)
        {
            foreach (var prop in properties)
            {
                source.Append(@"
        [System.Diagnostics.Contracts.Pure]
        public ")
                    .Append(declaredInType
                        ? typeMeta.IsSealed ? "" : "virtual "
                        : prop.IsAbstract || prop.IsVirtual ? "override " : "new "
                    )
                    .Append(typeMeta.Name)
                    .Append(" With").Append(prop.Name)
                    .Append("(").Append(prop.Type).Append(" value) => new ")
                    .Append(typeMeta.Name).Append("(");

                for (var j = 0; j < allProperties.Count; j++)
                {
                    var allName = allProperties[j].Name;
                    source.Append(prop.Name == allName ? "value" : allName);

                    if (j < allProperties.Count - 1)
                        source.Append(", ");
                }

                source.AppendLine(");");
            }
        }
    }
}
