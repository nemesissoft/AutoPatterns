using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace AutoPatterns.Utils
{
    internal readonly struct AttributeDataReader
    {
        public IReadOnlyList<TypedConstant> Args { get; }
        //TODO add reader for:   TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

        public AttributeDataReader(IReadOnlyList<TypedConstant> args) => Args = args;

        //public static AttributeDataReader FromAttributeData(AttributeData data) => new(data.ConstructorArguments);

        //TODO change to TryGetBoolValue

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
                //TODO iterate over NamedArguments
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
                    context.IsOptionEnabled("GenerateDebuggerHook")
                    );

                return true;
            }

            result = default;
            return false;
        }
    }
}
