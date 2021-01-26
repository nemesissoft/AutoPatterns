using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

#nullable enable
namespace AutoPatterns.Utils
{
    readonly struct AttributeDataReader
    {
        public IReadOnlyList<TypedConstant> Args { get; }

        public AttributeDataReader(IReadOnlyList<TypedConstant> args) => Args = args;

        //public static AttributeDataReader FromAttributeData(AttributeData data) => new(data.ConstructorArguments);

        public bool GetBoolValue(int i) => Args.Count > i
            ? (bool)Args[i].Value!
            : throw new ArgumentOutOfRangeException(nameof(Args),
                $"Cannot obtain argument parameter no {i}");

        public bool IsConstructedWithPrimitives
        {
            get
            {
                foreach (var arg in Args)
                    if (arg.Kind != TypedConstantKind.Primitive)
                        return false;

                return true;
            }
        }
    }

    internal readonly struct AutoWithSettings
    {
        public bool SupportValidation { get; }

        public bool GenerateDebuggerHook { get; }

        //TODO
        //public void RenderGeneratorHeaders (StringBuilder)
        //TODO render content on members, not classes ? 

        //TODO remove
        public string Generator => nameof(AutoWithGenerator);

        public AutoWithSettings(bool supportValidation, bool generateDebuggerHook)
        {
            SupportValidation = supportValidation;
            GenerateDebuggerHook = generateDebuggerHook;
        }

        public static bool TryFromCurrent(AttributeData attribute, GeneratorExecutionContext context, out AutoWithSettings result)
        {
            if (attribute.ConstructorArguments is { Length: 1 } args && new AttributeDataReader(args) is { IsConstructedWithPrimitives: true } reader)
            {
                result = new AutoWithSettings(
                    reader.GetBoolValue(0),
                    context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GenerateDebuggerHook",
                        out var hookText) && bool.TryParse(hookText, out var hook) && hook
                );

                return true;
            }

            result = default;
            return false;
        }
    }
}
