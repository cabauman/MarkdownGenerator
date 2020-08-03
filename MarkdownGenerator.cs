using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MarkdownWikiGenerator
{
    public class MarkdownableType
    {
        readonly Type type;
        readonly ILookup<string, XmlDocumentComment> commentLookup;

        public string Namespace => type.Namespace;
        public string Name => type.Name;
        public string BeautifyName => Beautifier.BeautifyType(type);

        public MarkdownableType(Type type, ILookup<string, XmlDocumentComment> commentLookup)
        {
            this.type = type;
            this.commentLookup = commentLookup;
        }

        public IReadOnlyList<MethodInfo> GetMethods()
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any())
                .ToArray();
        }

        IReadOnlyList<PropertyInfo> GetProperties()
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance/* | BindingFlags.NonPublic*/ | BindingFlags.DeclaredOnly | BindingFlags.GetProperty | BindingFlags.SetProperty)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any())
                .Where(y =>
                {
                    var get = y.GetGetMethod(true);
                    var set = y.GetSetMethod(true);
                    if (get != null && set != null)
                    {
                        return !(get.IsPrivate && set.IsPrivate);
                    }
                    else if (get != null)
                    {
                        return !get.IsPrivate;
                    }
                    else if (set != null)
                    {
                        return !set.IsPrivate;
                    }
                    else
                    {
                        return false;
                    }
                })
                .ToArray();
        }

        IReadOnlyList<ConstructorInfo> GetConstructors()
        {
            return type.GetConstructors();
        }

        IReadOnlyList<FieldInfo> GetFields()
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Instance/* | BindingFlags.NonPublic*/ | BindingFlags.DeclaredOnly | BindingFlags.GetField | BindingFlags.SetField)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any() && !x.IsPrivate)
                .ToArray();
        }

        IReadOnlyList<EventInfo> GetEvents()
        {
            return type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any())
                .ToArray();
        }

        IReadOnlyList<FieldInfo> GetStaticFields()
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Static/* | BindingFlags.NonPublic*/ | BindingFlags.DeclaredOnly | BindingFlags.GetField | BindingFlags.SetField)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any() && !x.IsPrivate)
                .ToArray();
        }

        IReadOnlyList<PropertyInfo> GetStaticProperties()
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Static/* | BindingFlags.NonPublic*/ | BindingFlags.DeclaredOnly | BindingFlags.GetProperty | BindingFlags.SetProperty)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any())
                .Where(y =>
                {
                    var get = y.GetGetMethod(true);
                    var set = y.GetSetMethod(true);
                    if (get != null && set != null)
                    {
                        return !(get.IsPrivate && set.IsPrivate);
                    }
                    else if (get != null)
                    {
                        return !get.IsPrivate;
                    }
                    else if (set != null)
                    {
                        return !set.IsPrivate;
                    }
                    else
                    {
                        return false;
                    }
                })
                .ToArray();
        }

        IReadOnlyList<MethodInfo> GetStaticMethods()
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static/* | BindingFlags.NonPublic*/ | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any() && !x.IsPrivate)
                .ToArray();
        }

        IReadOnlyList<EventInfo> GetStaticEvents()
        {
            return type.GetEvents(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any())
                .ToArray();
        }

        public string BuildMethodsPage()
        {
            var mb = new MarkdownBuilder();

            mb.Header(2, Beautifier.BeautifyType(type, false) + " Methods");
            mb.AppendLine();
            mb.AppendLine($"Namespace: {type.Namespace}");
            mb.AppendLine();
            mb.AppendLine($"Assembly: {type.Assembly.ManifestModule.Name}");
            mb.AppendLine();

            var summary = commentLookup[type.FullName].FirstOrDefault(x => x.MemberType == MemberType.Type)?.Summary ?? string.Empty;
            if (summary != string.Empty)
            {
                mb.AppendLine(summary);
            }

            mb.AppendLine();

            BuildTable(mb, "Methods", GetMethods(), commentLookup[type.FullName], x => Beautifier.BeautifyType(x.ReturnType), x => x.Name, x => Beautifier.ToMarkdownMethodInfoWithoutParamNames(x));

            return mb.ToString();
        }

        public string BuildMethodPage(MethodInfo methodInfo)
        {
            var mb = new MarkdownBuilder();

            mb.HeaderWithCode(2, $"{Beautifier.BeautifyType(type)}.{Beautifier.ToMarkdownMethodInfoWithoutParamNames(methodInfo, true)} Method");
            mb.AppendLine();
            mb.AppendLine($"Namespace: {type.Namespace}");
            mb.AppendLine();
            mb.AppendLine($"Assembly: {type.Assembly.ManifestModule.Name}");
            mb.AppendLine();

            var comment = commentLookup[type.FullName].FirstOrDefault(x => x.MemberName == methodInfo.Name || x.MemberName.StartsWith(methodInfo.Name + "`"));

            var summary = comment?.Summary ?? string.Empty;
            if (summary != string.Empty)
            {
                mb.AppendLine(summary);
                mb.AppendLine();
            }

            var sb = new StringBuilder();
            var stat = methodInfo.IsStatic ? "static " : "";
            var abst = methodInfo.IsAbstract ? "abstract " : "";
            var returnType = Beautifier.BeautifyType(methodInfo.ReturnType);
            sb.Append($"public {stat}{abst}{returnType} {Beautifier.ToMarkdownMethodInfo(methodInfo, true)}");
            mb.Code("csharp", sb.ToString());

            mb.AppendLine();

            if (comment.TypeParameters.Count > 0)
            {
                mb.Header(3, "Type Parameters");
                mb.AppendLine();
                foreach (var tp in comment.TypeParameters)
                {
                    mb.CodeQuote(tp.Key);
                    mb.AppendLine();
                    mb.AppendLine();
                    mb.AppendLine(tp.Value);
                }

                mb.AppendLine();
            }

            if (comment.Parameters.Count > 0)
            {
                mb.Header(3, "Parameters");
                mb.AppendLine();
                foreach (var parameter in comment.Parameters)
                {
                    mb.CodeQuote(parameter.Key);
                    mb.AppendLine();
                    mb.AppendLine();
                    mb.AppendLine(parameter.Value);
                }

                mb.AppendLine();
            }

            if (comment.Returns.Length > 0 && !comment.Returns.Equals("void"))
            {
                mb.Header(3, "Returns");
                mb.AppendLine();
                mb.CodeQuote(returnType);
                mb.AppendLine();
                mb.AppendLine();
                mb.AppendLine(comment.Returns);
            }

            return mb.ToString();
        }

        void BuildTable<T>(MarkdownBuilder mb, string label, IReadOnlyList<T> array, IEnumerable<XmlDocumentComment> docs, Func<T, string> type, Func<T, string> name, Func<T, string> finalName)
        {
            if (array.Any())
            {
                mb.AppendLine(label);
                mb.AppendLine();

                string[] head = new[] { "Name", "Description" };

                IEnumerable<T> seq = array;
                if (!this.type.IsEnum)
                {
                    seq = array.OrderBy(x => name(x));
                }

                var data = seq.Select(item2 =>
                {
                    var summary = docs.FirstOrDefault(x => x.MemberName == name(item2) || x.MemberName.StartsWith(name(item2) + "`"))?.Summary ?? "";
                    //if (summary.Contains("`1") && summary.Contains('[') && summary.StartsWith("Removes the specified event handler, causing"))
                    //{
                    //    var match = Regex.Match(summary, @".*\[(.*)\].*");
                    //    if (!match.Success)
                    //    {
                    //        return new[] { finalName(item2), summary };
                    //    }
                    //    var typeString = match.Groups[1].Value;
                    //    var indexOfSuffix = typeString.LastIndexOf("`2.");
                    //    if (indexOfSuffix > -1)
                    //    {
                    //        typeString = typeString.Remove(indexOfSuffix + 2);
                    //    }
                        
                    //    var methodInfo = item2 as MethodInfo;
                    //    var theType = Assembly
                    //        .Load(File.ReadAllBytes("System.Reactive.dll"))
                    //        .GetTypes()
                    //        .FirstOrDefault(x => x.FullName.Equals(typeString));

                    //    Console.WriteLine(summary);
                    //    summary = summary.Replace(typeString, Beautifier.BeautifyType(theType));
                    //}
                    return new[] { finalName(item2), summary };
                });

                mb.Table(head, data);
                mb.AppendLine();
            }
        }

        public override string ToString()
        {
            var mb = new MarkdownBuilder();

            var typeCategory = type.IsClass ? " Class" : type.IsInterface ? " Interface" : string.Empty;
            mb.HeaderWithCode(2, Beautifier.BeautifyType(type, false) + typeCategory);
            mb.AppendLine();
            mb.AppendLine($"Namespace: {type.Namespace}");
            mb.AppendLine();
            mb.AppendLine($"Assembly: {type.Assembly.ManifestModule.Name}");
            mb.AppendLine();

            var summary = commentLookup[type.FullName].FirstOrDefault(x => x.MemberType == MemberType.Type)?.Summary ?? "";
            if (summary != "")
            {
                mb.AppendLine(summary);
                mb.AppendLine();
            }

            var sb = new StringBuilder();

            var isStatic = type.IsAbstract && type.IsSealed;
            var @sealed = !type.IsAbstract && type.IsSealed ? "sealed " : "";
            var stat = isStatic ? "static " : "";
            var abst = (type.IsAbstract && !type.IsInterface && !type.IsSealed) ? "abstract " : "";
            var classOrStructOrEnumOrInterface = type.IsInterface ? "interface" : type.IsEnum ? "enum" : type.IsValueType ? "struct" : "class";

            sb.AppendLine($"public {stat}{@sealed}{abst}{classOrStructOrEnumOrInterface} {Beautifier.BeautifyType(type, true)}");
            var impl = string.Join(", ", new[] { type.BaseType }.Concat(type.GetInterfaces()).Where(x => x != null && x != typeof(object) && x != typeof(ValueType)).Select(x => Beautifier.BeautifyType(x)));
            if (impl != "")
            {
                sb.AppendLine("    : " + impl);
            }

            mb.Code("csharp", sb.ToString());

            var typeParameters = commentLookup[type.FullName].FirstOrDefault(x => x.MemberType == MemberType.Type)?.TypeParameters;
            if (typeParameters.Count > 0)
            {
                mb.Header(3, "Type Parameters");
                mb.AppendLine();
                mb.Table(new[] { "Name", "Summary" }, typeParameters.Select(x => new[] { x.Key, x.Value }));
            }

            mb.AppendLine();

            if (type.IsEnum)
            {
                var underlyingEnumType = Enum.GetUnderlyingType(type);

                var enums = Enum.GetNames(type)
                    .Select(x => new { Name = x, Value = (Convert.ChangeType(Enum.Parse(type, x), underlyingEnumType)) })
                    .OrderBy(x => x.Value)
                    .ToArray();

                BuildTable(mb, "Enum", enums, commentLookup[type.FullName], x => x.Value.ToString(), x => x.Name, x => x.Name);
            }
            else
            {
                BuildTable(mb, "Constructors", GetConstructors(), commentLookup[type.FullName], x => Beautifier.BeautifyType(x.DeclaringType), x => "#ctor", x => Beautifier.ToMarkdownConstructorInfo(x));
                BuildTable(mb, "Fields", GetFields(), commentLookup[type.FullName], x => Beautifier.BeautifyType(x.FieldType), x => x.Name, x => x.Name);
                BuildTable(mb, "Properties", GetProperties(), commentLookup[type.FullName], x => Beautifier.BeautifyType(x.PropertyType), x => x.Name, x => x.Name);
                BuildTable(mb, "Events", GetEvents(), commentLookup[type.FullName], x => Beautifier.BeautifyType(x.EventHandlerType), x => x.Name, x => x.Name);
                BuildTable(mb, "Methods", GetMethods(), commentLookup[type.FullName], x => Beautifier.BeautifyType(x.ReturnType), x => x.Name, x => Beautifier.ToMarkdownMethodInfoWithoutParamNames(x));
                BuildTable(mb, "Static Fields", GetStaticFields(), commentLookup[type.FullName], x => Beautifier.BeautifyType(x.FieldType), x => x.Name, x => x.Name);
                BuildTable(mb, "Static Properties", GetStaticProperties(), commentLookup[type.FullName], x => Beautifier.BeautifyType(x.PropertyType), x => x.Name, x => x.Name);
                BuildTable(mb, "Static Methods", GetStaticMethods(), commentLookup[type.FullName], x => Beautifier.BeautifyType(x.ReturnType), x => x.Name, x => Beautifier.ToMarkdownMethodInfo(x));
                BuildTable(mb, "Static Events", GetStaticEvents(), commentLookup[type.FullName], x => Beautifier.BeautifyType(x.EventHandlerType), x => x.Name, x => x.Name);
            }

            return mb.ToString();
        }
    }


    public static class MarkdownGenerator
    {
        public static MarkdownableType[] Load(string dllPath, string namespaceMatch)
        {
            var xmlPath = Path.Combine(Directory.GetParent(dllPath).FullName, Path.GetFileNameWithoutExtension(dllPath) + ".xml");

            var assembly = Assembly.LoadFrom(dllPath);

            //var aType = assembly.GetType("System.Reactive.Subjects.Subject`1");
            //var methods = aType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod);
            //var methods = aType.GetRuntimeMethods();

            XmlDocumentComment[] comments = new XmlDocumentComment[0];
            if (File.Exists(xmlPath))
            {
                comments = VSDocParser.ParseXmlComment(XDocument.Parse(File.ReadAllText(xmlPath)), namespaceMatch, assembly);
            }
            var commentsLookup = comments.ToLookup(x => x.ClassName);

            var namespaceRegex = 
                !string.IsNullOrEmpty(namespaceMatch) ? new Regex(namespaceMatch) : null;

            var markdownableTypes = new[] { assembly }
                .SelectMany(x =>
                {
                    try
                    {
                        return x.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(t => t != null);
                    }
                    catch
                    {
                        return Type.EmptyTypes;
                    }
                })
                .Where(x => x != null)
                .Where(x => x.IsPublic && !typeof(Delegate).IsAssignableFrom(x)/* && !x.GetCustomAttributes<ObsoleteAttribute>().Any()*/)
                .Where(x => IsRequiredNamespace(x, namespaceRegex))
                .Select(x => new MarkdownableType(x, commentsLookup))
                .ToArray();


            return markdownableTypes;
        }

        static bool IsRequiredNamespace(Type type, Regex regex) {
            if ( regex == null ) {
                return true;
            }
            return regex.IsMatch(type.Namespace != null ? type.Namespace : string.Empty);
        }
    }
}
