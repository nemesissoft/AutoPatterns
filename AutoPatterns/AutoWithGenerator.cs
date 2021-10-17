using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using AutoPatterns.Utils;

using Microsoft.CodeAnalysis;


namespace AutoPatterns
{
    public class Meta
    {
        public string TypeName { get; }
        public IReadOnlyList<PropertyMeta> Properties { get; }
        public Meta? Base { get; }
        public bool HasBase => Base is not null;

        public Meta(string typeName, IReadOnlyList<PropertyMeta> properties, Meta? @base)
        {
            TypeName = typeName;
            Properties = properties;
            Base = @base;
        }

        public override string ToString() => $"{TypeName} ({Properties?.Count ?? 0}) => {Base?.ToString() ?? "∅"}";
    }

    public record AutoWithGeneratorState(Meta Meta);

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
            state = null;
            var hierarchy = SymbolUtils.GetSymbolHierarchy(typeSymbol).Reverse().ToList();
            var basesDecoratedProperly = true;
            foreach (var baseType in hierarchy)
            {
                if (!ShouldProcessType(baseType, autoAttributeSymbol, context, out _))
                {
                    ReportDiagnostics(context, BaseTypeNotDecorated, baseType);
                    basesDecoratedProperly = false;
                }
            }
            if (!basesDecoratedProperly)
                return false;

            hierarchy.Add(typeSymbol);
            Meta? meta = null;
            foreach (var symbol in hierarchy)
                meta = new(symbol.Name,
                    PropertyMeta.GetProperties(symbol, namespaces).ToList(), meta);

            if (meta is null)
                return false;

            state = new(meta);

            static int GetNonAbstractPropertiesCount(Meta meta)
            {
                var nonAbstractPropertiesCount = 0;
                var current = meta;
                do
                {
                    nonAbstractPropertiesCount += current.Properties.Count(p => !p.IsAbstract);
                    current = current.Base;
                } while (current is not null);

                return nonAbstractPropertiesCount;
            }

            if (typeSymbol.IsAbstract || GetNonAbstractPropertiesCount(meta) > 0) return true;


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

            Meta meta = state.Meta;
            var allProperties = GetAllProperties(meta);

            for (var i = 0; i < allProperties.Count; i++)
                source.Append(allProperties[i].Type).Append(" ")
                      .Append(allProperties[i].ParameterName)
                      .Append(i < allProperties.Count - 1 ? ", " : "");
            source.Append(")");


            var declaredOutside = meta.Base is null ? null : GetAllProperties(meta.Base);

            if (declaredOutside is not null)
            {
                source.Append(" : base(");
                for (var i = 0; i < declaredOutside.Count; i++)
                    source.Append(declaredOutside[i].ParameterName)
                          .Append(i < declaredOutside.Count - 1 ? ", " : "");
                source.Append(")");
            }

            source.AppendLine(@"
        {");

            var declaredInType = meta.Properties.Where(p => !p.IsAbstract).ToList();

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

            //TODO add withers:
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

        private static IReadOnlyList<PropertyMeta> GetAllProperties(Meta meta)
        {
            static bool IsOverrideOfVirtual(string propName, Meta current)
            {
                var m = current;
                while ((m = m.Base) != null)
                {
                    if (m.Properties.Any(pm => string.Equals(pm.Name, propName) && pm.IsVirtual))
                        return true;
                }

                return false;
            }

            static bool IsOverrideOfAbstract(string propName, Meta current)
            {
                var m = current;
                while ((m = m.Base) != null)
                {
                    if (m.Properties.Any(pm => string.Equals(pm.Name, propName) && pm.IsAbstract))
                        return true;
                }

                return false;
            }

            static bool IsOverrideOfPreviouslyImplementedAbstract(string propName, Meta current)
            {
                var m = current;
                while ((m = m.Base) != null)
                {
                    if (m.Properties.Any(pm => string.Equals(pm.Name, propName) && pm.IsOverride && IsOverrideOfAbstract(propName, m)))
                        return true;
                }

                return false;
            }

            var result = new List<PropertyMeta>();

            var m = meta;
            do
            {
                if (m.HasBase)
                    foreach (var prop in m.Properties)
                    {
                        if (prop.IsAbstract) continue;

                        var shouldAdd =
                            !prop.IsOverride ||
                            (!IsOverrideOfVirtual(prop.Name, m) && !IsOverrideOfPreviouslyImplementedAbstract(prop.Name, m));

                        if (shouldAdd) result.Add(prop);
                    }

                else
                    result.AddRange(m.Properties.Where(p => !p.IsAbstract));

            } while ((m = m.Base) != null);
            return result;
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
