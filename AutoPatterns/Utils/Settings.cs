using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace AutoPatterns.Utils
{
    internal static class AttributeDataReader
    {
        public static readonly MethodInfo GetValueMethod =
            typeof(AttributeDataReader).GetMethod(nameof(GetValue))
            ?? throw new MissingMethodException(nameof(AttributeDataReader), nameof(GetValue));

        public static TValue GetValue<TValue>(IReadOnlyList<TypedConstant> args, int i) => args.Count > i
            ? (TValue)args[i].Value!
            : throw new ArgumentOutOfRangeException(nameof(args),
                $"Cannot obtain argument parameter no {i}");

        public static bool IsConstructedWithPrimitives(IReadOnlyList<TypedConstant> args)
        {
            foreach (var arg in args)
                if (arg.Kind != TypedConstantKind.Primitive)
                    return false;
            return true;
        }
    }

    public abstract record CommonAutoSettings
    {
        private class AttributeParameterReader
        {
            public int MaxAttrParams { get; }
            public Func<IReadOnlyList<TypedConstant>, CommonAutoSettings> Reader { get; }

            public AttributeParameterReader(int maxAttrParams, Type settingsType)
            {
                MaxAttrParams = maxAttrParams;
                Reader = GenerateReader(settingsType);
            }

            private static Func<IReadOnlyList<TypedConstant>, CommonAutoSettings> GenerateReader(Type type)
            {
                var ctor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault()
                           ?? throw new MissingMemberException($"{type.Name} should have at least 1 constructor. The one with largest number of parameters will be used");

                var args = Expression.Parameter(typeof(IReadOnlyList<TypedConstant>), "args");

                Expression[] ctorArguments = ctor.GetParameters().Select((param, i) => (Expression)
                    Expression.Call(AttributeDataReader.GetValueMethod.MakeGenericMethod(param.ParameterType),
                        args, Expression.Constant(i))
                ).ToArray();

                var ctorExpr = Expression.New(ctor, ctorArguments);

                var lambda = Expression.Lambda<Func<IReadOnlyList<TypedConstant>, CommonAutoSettings>>(ctorExpr, args);
                return lambda.Compile();
            }
        }

        private static readonly Dictionary<Type, AttributeParameterReader> _attributeParameterReaders = new()
        {
            [typeof(AutoWithSettings)] = new(1, typeof(AutoWithSettings)),
            [typeof(AutoDescribeSettings)] = new(2, typeof(AutoDescribeSettings))
        };

        public bool GenerateDebuggerHook { get; private set; }

        private void LoadCommon(GeneratorExecutionContext context) =>
            GenerateDebuggerHook = context.IsOptionEnabled("GenerateDebuggerHook");

        public static bool TryLoad<TSettings>(AttributeData attribute, GeneratorExecutionContext context, out TSettings? result)
            where TSettings : CommonAutoSettings
        {
            var settingsType = typeof(TSettings);
            var attrReader = _attributeParameterReaders.TryGetValue(settingsType, out var reader)
                ? reader
                : throw new NotSupportedException(
                    $"{settingsType.Name} is not properly configured in settings reader");

            CommonAutoSettings? newSettings = null;

            if (attribute.ConstructorArguments is { } args && args.Length <= reader.MaxAttrParams &&
                AttributeDataReader.IsConstructedWithPrimitives(args))
            {
                newSettings = attrReader.Reader(args);
                newSettings.LoadCommon(context);
            }

            result = (TSettings?)newSettings;
            return result is not null;
        }
    }

    public record AutoWithSettings(bool SupportValidation) : CommonAutoSettings;

    public record AutoDescribeSettings(bool AddToStringMethod, bool AddDebuggerDisplayAttribute) : CommonAutoSettings;
}
