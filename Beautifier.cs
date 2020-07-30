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

        public static string ToMarkdownMethodInfo(MethodInfo methodInfo)
        {
            var isExtension = methodInfo.GetCustomAttributes<ExtensionAttribute>(false).Any();

            //if (methodInfo.IsGenericMethod)
            //{
            //    var innerFormat = string.Join(", ", methodInfo.GetGenericArguments().Select(x => BeautifyType(x)));
            //    return Regex.Replace(methodInfo.GetGenericMethodDefinition().Name, @"`.+$", "") + "<" + innerFormat + ">";
            //}

            var parameters = methodInfo.GetParameters();
            foreach (var p in parameters)
            {

            }

            var seq = parameters.Select(x =>
            {
                var suffix = x.HasDefaultValue ? (" = " + (x.DefaultValue ?? $"null")) : "";
                return "`" + BeautifyType(x.ParameterType) + "` " + x.Name + suffix;
            });

            return methodInfo.Name + "(" + (isExtension ? "this " : "") + string.Join(", ", seq) + ")";
        }

        public static string ToMarkdownMethodInfo2(MethodInfo methodInfo)
        {
            var isExtension = methodInfo.GetCustomAttributes<ExtensionAttribute>(false).Any();

            if (methodInfo.IsGenericMethod)
            {
                var innerFormat = string.Join(", ", methodInfo.GetGenericArguments().Select(x => BeautifyType(x)));
                return Regex.Replace(methodInfo.GetGenericMethodDefinition().Name, @"`.+$", "") + "<" + innerFormat + ">";
            }

            var seq = methodInfo.GetParameters().Select(x =>
            {
                return "`" + BeautifyType(x.ParameterType) + "`";
            });

            return methodInfo.Name + "(" + (isExtension ? "this " : "") + string.Join(", ", seq) + ")";
        }
    }
}
