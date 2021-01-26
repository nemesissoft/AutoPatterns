using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using AutoPatterns.Utils;

namespace AutoPatterns
{
    public partial class AutoWithGenerator
    {
        private static string RenderRecord(TypeMeta meta, IReadOnlyList<MemberMeta> properties, IEnumerable<Using> namespaces, AutoWithSettings settings)
        {
            var source = new StringBuilder(512);
            source.AppendLine(GeneratorUtils.HEADER);

            var sortedNamespaces = Using.Sort(namespaces);


            foreach (var ns in sortedNamespaces)
                if (!string.Equals(meta.Namespace, ns.NamespaceOrType, StringComparison.Ordinal))
                    source.AppendLine(ns.ToCSharpCode());

            source.Append($@"
namespace {meta.Namespace}
{{
    [System.CodeDom.Compiler.GeneratedCode(""{settings.Generator}"", ""{Assembly.GetExecutingAssembly().GetName().Version}"")]
    [System.Runtime.CompilerServices.CompilerGenerated]
    {meta.TypeDefinition} {meta.Name} 
    {{");

            if (settings.GenerateDebuggerHook)
                source.AppendLine(@"
#if DEBUG
        internal void DebuggerHook() {{ System.Diagnostics.Debugger.Launch(); }}
#endif");

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
            return source.ToString();
        }
    }
}
