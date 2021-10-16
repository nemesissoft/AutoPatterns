using System.Collections.Generic;
using System.Linq;
using System.Text;

using AutoPatterns.Utils;

using Microsoft.CodeAnalysis;

//TODO pass StringBuilder to Descriptor.Describe + do not materialize large Enumerables + provide non-pattern match Describe 
namespace AutoPatterns
{
    public record AutoDescribeGeneratorState(IList<MemberMeta> Properties, AutoDescribeSettings? Settings)
    {
        public bool IsDerivedClass { get; set; }
    }

    [Generator]
    public sealed class AutoDescribeGenerator : AutoAttributeGenerator<AutoDescribeGeneratorState>
    {
        private readonly DiagnosticDescriptor _invalidSettingsAttributeRule;
        private readonly DiagnosticDescriptor _baseTypeNotDecorated;

        public AutoDescribeGenerator() : base("Describe", "AutoDescribeAttribute", @"using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace Auto
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    internal sealed class AutoDescribeAttribute : Attribute
    {
        public bool AddToStringMethod { get; }
        public bool AddDebuggerDisplayAttribute { get; }

        public AutoDescribeAttribute(bool addToStringMethod = true, bool addDebuggerDisplayAttribute = false)
        {
            AddToStringMethod = addToStringMethod;
            AddDebuggerDisplayAttribute = addDebuggerDisplayAttribute;
        }
    }

    internal static class Descriptor
    {
        public static string Describe(object value) =>
            value switch
            {
                null => ""∅"",
                bool b => b ? ""true"" : ""false"",
                string s => $""\""{s}\"""",
                char c => $""\'{c}\'"",
                DateTime dt => dt.ToString(""o"", CultureInfo.InvariantCulture),
                IEnumerable ie => ""["" + string.Join("", "", ie.Cast<object>().Select(Describe)) + ""]"",
                IFormattable @if => @if.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
    }
}")
        {
            _invalidSettingsAttributeRule = GetDiagnosticDescriptor(3, AutoPatternName, "Attribute {3} must be constructed with 2 boolean values, or with default values");
            _baseTypeNotDecorated = GetDiagnosticDescriptor(4, AutoPatternName, "Base '{0}' type must also be decorated with {3} attribute");
        }

        protected override bool ShouldProcessType(ISymbol typeSymbol, ISymbol autoAttributeSymbol,
            in GeneratorExecutionContext context, out AutoDescribeGeneratorState? state)
        {
            if (GeneratorUtils.GetAttribute(typeSymbol, autoAttributeSymbol) is { } autoAttributeData)
            {
                if (CommonAutoSettings.TryLoad<AutoDescribeSettings>(autoAttributeData, context, out var settings))
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
            in GeneratorExecutionContext context, ISet<Using> namespaces, AutoDescribeGeneratorState? state)
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
                    //TODO check behaviour for abstract properties 
                    propertyList.Add(new(ps.Name, SymbolUtils.GetTypeMinimalName(ps.Type), declaredInBase, ps.IsAbstract));
                    Using.ExtractNamespaces(ps.Type, namespaces);
                }
            }


            var basesDecoratedProperly = true;
            foreach (var baseType in SymbolUtils.GetSymbolHierarchy(typeSymbol))
            {
                state.IsDerivedClass = true;
                if (!ShouldProcessType(baseType, autoAttributeSymbol, context, out _))
                {
                    ReportDiagnostics(context, _baseTypeNotDecorated, baseType);
                    basesDecoratedProperly = false;
                }
            }

            FetchProperties(typeSymbol, false);


            foreach (var prop in propertyList)
                state.Properties.Add(prop);
            return basesDecoratedProperly;
        }


        protected override void Render(StringBuilder source, TypeMeta typeMeta, AutoDescribeGeneratorState? state)
        {
            const string DISPLAY_METHOD = "GetDisplayText";
            const string PRINT_MEMBERS = "PrintMembers";

            //TODO improve nullable checks 
            var properties = state?.Properties ?? new List<MemberMeta>();
            var settings = state?.Settings ?? new AutoDescribeSettings(true, false);
            var isDerivedClass = state?.IsDerivedClass ?? false;

            source.AppendLine($@"
namespace {typeMeta.Namespace}
{{");

            if (settings.AddDebuggerDisplayAttribute)
                source.AppendLine($@"{INDENT_1}[System.Diagnostics.DebuggerDisplay(""{{{DISPLAY_METHOD}(),nq}}"")]");

            source.Append($@"{INDENT_1}{typeMeta.TypeDefinition} {typeMeta.Name} 
    {{");

            RenderDebuggerHook(source, settings);

            source.AppendLine($@"
        private string {DISPLAY_METHOD}()
        {{
            var sb = new System.Text.StringBuilder();
        	sb.Append(""{typeMeta.Name}"");
        	sb.Append("" {{ "");
        	if (this.{PRINT_MEMBERS}(sb))
        		sb.Append("" "");
        	sb.Append(""}}"");
        	return sb.ToString();
        }}");


            if (settings.AddToStringMethod)
                source.AppendLine($@"{INDENT_2}public override string ToString() => {DISPLAY_METHOD}();");

            //TODO add no modifier for base class with sealed keyword 
            source.Append($@"
        protected {(isDerivedClass ? "override" : "virtual")} bool {PRINT_MEMBERS}(System.Text.StringBuilder builder)
        {{");

            if (properties.Count == 0)
            {
                source.AppendLine(isDerivedClass
                    ? @$"
            return base.{PRINT_MEMBERS}(builder);"
                    : @"
            return false;");
            }
            else
            {
                if (isDerivedClass)
                    source.AppendLine(@$"
            if (base.{PRINT_MEMBERS}(builder)) builder.Append("", "");");

                //TODO add SpecificDescribe for KeyValuePair<,> and generics 
                for (var i = 0; i < properties.Count; i++)
                {
                    var p = properties[i];

                    //TODO add option for customizing Descriptor.Describe method 
                    source.AppendLine($@"
            builder.Append(""{p.Name}"")
                   .Append("" = "")
                   .Append(Auto.Descriptor.Describe({p.Name}));");

                    if (i < properties.Count - 1)
                        source.AppendLine($@"{INDENT_3}builder.Append("", "");");
                }

                source.AppendLine(@"
            return true;");

            }

            source.AppendLine(@"        }
    }
}");
        }
    }
}