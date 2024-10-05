using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MarkdownWikiGenerator
{
    public class GenericClass<T1>
    {
        public void GenericMethod<U>(string s, T1 t, Func<Action<int>, U> u)
        {
        }
    }

    public enum MemberType
    {
        Field = 'F',
        Property = 'P',
        Type = 'T',
        Event = 'E',
        Method = 'M',
        None = 0
    }

    public class XmlDocumentSeeGenericType
    {
        public string ClassName { get; set; }
    }

    public class XmlDocumentSeeGenericMethod
    {
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public int MethodTypeParamCount { get; set; }
        public int MethodArgCount { get; set; }
    }

    public class ArgNode
    {
        public string value;
        public IReadOnlyList<ArgNode> argNodes;

        public ArgNode(string typeName, Assembly assembly, IDictionary<string, string> typeParamMarkerToNameMap)
        {
            if (typeName.StartsWith('`'))
            {
                value = typeParamMarkerToNameMap[typeName];
                return;
            }

            Type type;
            typeName = typeName.Replace("@", "");
            var firstBracketIndex = typeName.IndexOf('{');
            var isGeneric = firstBracketIndex > -1;
            if (!isGeneric)
            {
                type = assembly.GetType(typeName);
                if (type == null)
                {
                    type = Type.GetType(typeName);
                }

                if (type == null) return;
                value = type.Name;
                return;
            }

            var lastBracketIndex = typeName.LastIndexOf('}');
            var argsString = typeName.Substring(firstBracketIndex + 1, lastBracketIndex - 1 - firstBracketIndex);
            argNodes = VSDocParser.SplitArgs(argsString)
                .Select(x => new ArgNode(x, assembly, typeParamMarkerToNameMap))
                .ToList();

            typeName = typeName.Substring(0, firstBracketIndex) + '`' + argNodes.Count;
            type = assembly.GetType(typeName);
            if (type == null)
            {
                type = Type.GetType(typeName);
            }

            value = type.Name[..^2];
        }

        public override string ToString()
        {
            if (argNodes == null)
            {
                return value;
            }

            return $"{value}<{string.Join(',', argNodes)}>";
        }
    }

    public class XmlDocumentComment
    {
        public MemberType MemberType { get; set; }
        public string ClassName { get; set; }
        public string MemberName { get; set; }
        public string Summary { get; set; }
        public string Remarks { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public Dictionary<string, string> TypeParameters { get; set; }
        public Dictionary<string, string> Exceptions { get; set; }
        public string Returns { get; set; }
        public IReadOnlyList<XmlDocumentSeeGenericType> SeeGenericTypes { get; set; }
        public IReadOnlyList<XmlDocumentSeeGenericMethod> SeeGenericMethods { get; set; }

        public override string ToString()
        {
            return MemberType + ":" + ClassName + "." + MemberName;
        }
    }

    public static class VSDocParser
    {
        /// <summary>
        /// Testing <see cref="GenericClass{T}.GenericMethod{Z}(string, T, Func{Action{int}, Z})"/>
        /// </summary>
        public static void DummyMethod<Y>()
        {

        }

        //public static XmlDocumentComment[] ParseXmlComment(XDocument xDocument) {
        //    return ParseXmlComment(xDocument, null);
        //}

        // cheap, quick hack parser:)
        internal static XmlDocumentComment[] ParseXmlComment(XDocument xDocument, string namespaceMatch, Assembly assembly) {

            var assemblyName = xDocument.Descendants("assembly").First().Elements("name").First().Value;

            return xDocument.Descendants("member")
                .Select(x => {
                    var match = Regex.Match(x.Attribute("name").Value, @"(.):(.+)\.([^.()]+)?(\(.+\)|$)");
                    if (!match.Groups[1].Success) return null;

                    var memberType = (MemberType)match.Groups[1].Value[0];
                    if (memberType == MemberType.None) return null;

                    var summaryXml = x.Elements("summary").FirstOrDefault()?.ToString()
                        ?? x.Element("summary")?.ToString()
                        ?? string.Empty;

                    summaryXml = Regex.Replace(summaryXml, @"<\/?summary>", string.Empty);
                    summaryXml = Regex.Replace(summaryXml, @"<para\s*/>", Environment.NewLine);
                    summaryXml = Regex.Replace(summaryXml, @"<see(?: also)? cref=""(\w):([^\""]*)""\s*\/?>", m => ResolveSeeElement(m, assemblyName, assembly));

                    var parsed = Regex.Replace(summaryXml, @"<(?: type)*paramref name=""([^\""]*)""\s*\/>", e => $"`{e.Groups[1].Value}`");

                    var summary = parsed;

                    if (summary != "") {
                        summary = string.Join("  ", summary.Split(new[] { "\r", "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(y => y.Trim()));
                    }
                    
                    var returns = ((string)x.Element("returns")) ?? "";
                    var remarks = ((string)x.Element("remarks")) ?? "";
                    var parameters = x.Elements("param")
                        .Select(e => Tuple.Create(e.Attribute("name").Value, e))
                        .Distinct(new Item1EqualityCompaerer<string, XElement>())
                        .ToDictionary(e => e.Item1, e => e.Item2.Value);
                    var typeParameters = x.Elements("typeparam")
                        .Select(e => Tuple.Create(e.Attribute("name").Value, e))
                        .Distinct(new Item1EqualityCompaerer<string, XElement>())
                        .ToDictionary(e => e.Item1, e => e.Item2.Value);
                    var exceptions = x.Elements("exception")
                        .Select(e => Tuple.Create(e.Attribute("cref").Value, e))
                        .Distinct(new Item1EqualityCompaerer<string, XElement>())
                        .ToDictionary(e => e.Item1, e => e.Item2.Value);

                    var className = (memberType == MemberType.Type)
                        ? match.Groups[2].Value + "." + match.Groups[3].Value
                        : match.Groups[2].Value;

                    return new XmlDocumentComment {
                        MemberType = memberType,
                        ClassName = className,
                        MemberName = match.Groups[3].Value,
                        Summary = summary.Trim(),
                        Remarks = remarks.Trim(),
                        Parameters = parameters,
                        TypeParameters = typeParameters,
                        Exceptions = exceptions,
                        Returns = returns.Trim()
                    };
                })
                .Where(x => x != null)
                .ToArray();
        }

        public static IReadOnlyList<string> SplitArgs(string s)
        {
            var parts = new List<string>();
            var parenLevel = 0;
            var lastPos = 0;
            for (var i = 0; i != s.Length; i++)
            {
                switch (s[i])
                {
                    case '{':
                        parenLevel++;
                        break;
                    case '}':
                        parenLevel--;
                        if (parenLevel < 0)
                        {
                            throw new ArgumentException();
                        }
                        break;
                    case ',':
                        if (parenLevel == 0)
                        {
                            parts.Add(s.Substring(lastPos, i - lastPos));
                            lastPos = i + 1;
                        }
                        break;
                }
            }
            if (lastPos != s.Length)
            {
                parts.Add(s.Substring(lastPos, s.Length - lastPos));
            }

            return parts;
        }

        private static string ResolveSeeElement(Match m, string ns, Assembly assembly)
        {
            //<see cref="M:System.Reactive.Linq.Observable.Scan``2(System.IObservable{``0},``1,System.Func{``1,``0,``1})"/>

            var isMethod = m.Groups[1].Value[0].Equals('M');
            var isType = m.Groups[1].Value[0].Equals('T');
            var typeName = m.Groups[2].Value;

            if (isType)
            {
                var isGeneric = typeName[^2].Equals('`');

                var type = assembly.GetType(typeName);
                if (type == null)
                {
                    type = Type.GetType(typeName);
                }
                var beautifulTypeName = Beautifier.BeautifyType(type);
                return $"`{beautifulTypeName}`";
            }

            var methods = new List<XmlDocumentSeeGenericMethod>();
            if (isMethod)
            {
                var parts = typeName.Split('(', ')');
                var namespaceQualifiedMethodName = parts[0];
                var methodNameSeparatorIndex = namespaceQualifiedMethodName.LastIndexOf('.');
                var namespaceQualifiedTypeName = namespaceQualifiedMethodName.Substring(0, methodNameSeparatorIndex);
                var methodName = namespaceQualifiedMethodName.Substring(methodNameSeparatorIndex + 1);
                var isGenericMethod = methodName[^2].Equals('`');
                var isArray = methodName[^2..].Equals("[]");

                var typeParamMarkerToNameMap = new Dictionary<string, string>();

                var type = assembly.GetType(namespaceQualifiedTypeName);
                if (type == null)
                {
                    type = Type.GetType(namespaceQualifiedTypeName);
                }

                var isGenericType = namespaceQualifiedTypeName[^2].Equals('`');

                if (parts.Length > 1)
                {
                    var genericTypeArgs = type.GetGenericArguments();
                    for (int i = 0; i < genericTypeArgs.Length; ++i)
                    {
                        typeParamMarkerToNameMap[$"`{i}"] = genericTypeArgs[i].Name;
                    }

                    var argumentsString = parts[1];
                    var argsUseGenericMethodParams = argumentsString.Contains("{``");
                    var argStrings = SplitArgs(argumentsString);

                    var genericParameterCount = isGenericMethod ? int.Parse(methodName[^1].ToString()) : 0;
                    if (isGenericMethod)
                    {
                        methodName = methodName.Remove(methodName.Length - 3);
                    }
                    var theMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod)
                        .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any() && !x.IsPrivate);
                    var matchingMethod = theMethods
                        .Single(x => x.Name.Equals(methodName) && x.GetGenericArguments().Count().Equals(genericParameterCount) && x.GetParameters().Count().Equals(argStrings.Count));

                    var genericMethodTypeArgs = matchingMethod.GetGenericArguments();
                    for (int i = 0; i < genericMethodTypeArgs.Length; ++i)
                    {
                        typeParamMarkerToNameMap[$"``{i}"] = genericMethodTypeArgs[i].Name;
                    }

                    var arguments = argStrings
                        .Select(x => new ArgNode(x, assembly, typeParamMarkerToNameMap))
                        .ToList();

                    return $"`{methodName}({string.Join(',', arguments)})`";
                }

                return methodName;
            }

            return typeName.Remove(0, typeName.LastIndexOf('.') + 1);
        }

        class Item1EqualityCompaerer<T1, T2> : EqualityComparer<Tuple<T1, T2>>
        {
            public override bool Equals(Tuple<T1, T2> x, Tuple<T1, T2> y)
            {
                return x.Item1.Equals(y.Item1);
            }

            public override int GetHashCode(Tuple<T1, T2> obj)
            {
                return obj.Item1.GetHashCode();
            }
        }
    }
}
