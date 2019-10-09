using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Castle.DynamicProxy;

namespace Amg.Build
{
    internal class OnceHook : IProxyGenerationHook
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        public void MethodsInspected()
        {
            foreach (var type in types)
            {
                AssertNoMutableFields(type);
                AssertNoMutableProperties(type);
            }
        }

        readonly HashSet<Type> types = new HashSet<Type>();

        static void AssertNoMutableFields(Type type)
        {
            var fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public | 
                BindingFlags.NonPublic);

            var mutableFields = fields.Where(
                f => !f.IsInitOnly && 
                !f.GetCustomAttributes<CompilerGeneratedAttribute>().Any());

            if (mutableFields.Any())
            {
                throw new OnceException($@"All fields of {type} must be readonly. 
Following fields are not readonly:
{mutableFields.Select(_ => _.Name).Join()}");
            }
        }

        static bool IsCommandLineProperty(PropertyInfo p)
        {
            return p.GetCustomAttributes<System.ComponentModel.DescriptionAttribute>().Any();
        }

        static void AssertNoMutableProperties(Type type)
        {
            var properties = type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public | 
                BindingFlags.NonPublic);

            var writableProperties = properties.Where(
                f => f.CanWrite && !Once.Has(f));

            if (writableProperties.Any())
            {
                throw new OnceException($@"All properties of {type} must be readonly OR have the [Once] attribute.
Following properties do not fulfill the condition:
{writableProperties.Select(_ => _.Name).Join()}");
            }
        }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
        {
            if (Once.Has(memberInfo))
            {
                throw new OnceException($"{memberInfo} must be virtual because it has the [Once] attribute.");
            }
        }

        public bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            if (Once.Has(methodInfo))
            {
                types.Add(methodInfo.DeclaringType);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
