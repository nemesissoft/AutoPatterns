using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace AutoPatterns.Utils
{
    internal static class AttributeDataReader
    {
        //TODO change to TryGetBoolValue

        public static bool GetBoolValue(IReadOnlyList<TypedConstant> args, int i) => args.Count > i
            ? (bool)args[i].Value!
            : throw new ArgumentOutOfRangeException(nameof(args),
                $"Cannot obtain argument parameter no {i}");

        public static bool IsConstructedWithPrimitives(IReadOnlyList<TypedConstant> args)
        {
            foreach (var arg in args)
                if (arg.Kind != TypedConstantKind.Primitive)
                    return false;
            //TODO iterate over NamedArguments
            return true;
        }
    }

    public abstract class CommonAutoSettings
    {
        public bool GenerateDebuggerHook { get; private set; }

        //TODO
        //public void RenderGeneratorHeaders (StringBuilder)

        private void LoadCommon(GeneratorExecutionContext context) =>
            GenerateDebuggerHook = context.IsOptionEnabled("GenerateDebuggerHook");

        public static bool TryLoad<TSettings>(AttributeData attribute, GeneratorExecutionContext context, out TSettings? result)
            where TSettings : CommonAutoSettings, new()
        {
            result = new();
            if (result.CanLoad(attribute))
            {
                result.Load(attribute.ConstructorArguments);
                result.LoadCommon(context);
                return true;
            }
            result = default;
            return false;
        }

        protected abstract bool CanLoad(AttributeData attribute);
        protected abstract void Load(IReadOnlyList<TypedConstant> args);
    }

    public class AutoWithSettings : CommonAutoSettings
    {
        public bool SupportValidation { get; private set; }

        protected override bool CanLoad(AttributeData attribute) =>
            attribute.ConstructorArguments is {Length: <= 1} args && AttributeDataReader.IsConstructedWithPrimitives(args);

        protected override void Load(IReadOnlyList<TypedConstant> args) =>
            SupportValidation = AttributeDataReader.GetBoolValue(args, 0);
    }
    
    public class AutoDescribeSettings : CommonAutoSettings
    {
        public bool AddToStringMethod { get; private set; }
        public bool AddDebuggerDisplayAttribute { get; private set; }
        
        protected override bool CanLoad(AttributeData attribute) =>
            attribute.ConstructorArguments is { Length: <= 2 } args &&
            AttributeDataReader.IsConstructedWithPrimitives(args);

        protected override void Load(IReadOnlyList<TypedConstant> args)
        {
            AddToStringMethod = AttributeDataReader.GetBoolValue(args, 0);
            AddDebuggerDisplayAttribute = AttributeDataReader.GetBoolValue(args, 1);
        }
    }
}
