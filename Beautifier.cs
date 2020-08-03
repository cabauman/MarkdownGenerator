using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MarkdownWikiGenerator
{
    public static class Beautifier
    {
        public static string BeautifyType(Type t, bool isFull = false)
        {
            if (t == null) return string.Empty;
            if (t == typeof(void)) return "void";
            if (t.IsArray) return BeautifyType(t.GetElementType()) + "[]";
            if (!t.IsGenericType) return (isFull) ? t.FullName : t.Name;

            var innerFormat = string.Join(", ", t.GetGenericArguments().Select(x => BeautifyType(x)));
            return Regex.Replace(isFull ? t.GetGenericTypeDefinition().FullName : t.GetGenericTypeDefinition().Name, @"`.+$", "") + "<" + innerFormat + ">";
        }

        public static string ToMarkdownMethodInfo(MethodInfo methodInfo, bool excludeCodeQuote = false, bool excludeEmptyParentheses = false)
        {
            var isExtension = methodInfo.GetCustomAttributes<ExtensionAttribute>(false).Any();
            var parameters = methodInfo.GetParameters();

            if (excludeEmptyParentheses && parameters.Length == 0)
            {
                return methodInfo.Name;
            }

            var seq = parameters.Select(x =>
            {
                var suffix = x.HasDefaultValue ? (" = " + (x.DefaultValue ?? $"null")) : "";
                if (excludeCodeQuote)
                {
                    return BeautifyType(x.ParameterType) + " " + x.Name + suffix;
                }
                else
                {
                    return "`" + BeautifyType(x.ParameterType) + "` " + x.Name + suffix;
                }
            });

            return methodInfo.Name + "(" + (isExtension ? "this " : "") + string.Join(", ", seq) + ")";
        }

        public static string ToMarkdownMethodInfoWithoutParamNames(MethodInfo methodInfo, bool excludeCodeQuote = false)
        {
            var parameters = methodInfo.GetParameters();

            //if (methodInfo.IsGenericMethod)
            //{
            //    var innerFormat = string.Join(", ", methodInfo.GetGenericArguments().Select(x => BeautifyType(x)));
            //    return Regex.Replace(methodInfo.GetGenericMethodDefinition().Name, @"`.+$", "") + "<" + innerFormat + ">";
            //}

            if (parameters.Length == 0)
            {
                return methodInfo.Name;
            }

            var seq = parameters.Select(x =>
            {
                if (excludeCodeQuote)
                {
                    return BeautifyType(x.ParameterType);
                }
                else
                {
                    return $"`{BeautifyType(x.ParameterType)}`";
                }
            });

            return methodInfo.Name + "(" + string.Join(", ", seq) + ")";
        }

        public static string ToMarkdownConstructorInfo(ConstructorInfo constructorInfo)
        {
            string name;
            if (constructorInfo.DeclaringType.IsGenericType)
            {
                var innerFormat = string.Join(", ", constructorInfo.DeclaringType.GetGenericArguments().Select(x => BeautifyType(x)));
                return Regex.Replace(constructorInfo.DeclaringType.Name, @"`.+$", "") + "<" + innerFormat + ">";
            }
            else
            {
                name = constructorInfo.DeclaringType.Name;
            }

            var seq = constructorInfo.GetParameters().Select(x =>
            {
                return "`" + BeautifyType(x.ParameterType) + "`";
            });

            return name + "(" + string.Join(", ", seq) + ")";
        }
    }
}
