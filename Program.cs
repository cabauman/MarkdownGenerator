﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MarkdownGenerator;

namespace MarkdownWikiGenerator
{
    class Program
    {
        // 0 = dll src path, 1 = dest root
        static void Main(string[] args)
        {
            //var iobserverType = Type.GetType("System.IObserver`1[]");
            //var t = iobserverType.GetGenericArguments()[0];

            //var parameters = methodInfo.GetParameters();
            //foreach (var p in parameters)
            //{
            //    BeautifyType(x.ParameterType);
            //}

            // put dll & xml on same diretory.
            var target = "UniRx.dll"; // :)
            string dest = "md";
            string namespaceMatch = string.Empty;
            if (args.Length == 1)
            {
                target = args[0];
            }
            else if (args.Length == 2)
            {
                target = args[0];
                dest = args[1];
            }
            else if (args.Length == 3)
            {
                target = args[0];
                dest = args[1];
                namespaceMatch = args[2];
            }

            var types = MarkdownGenerator.Load(target, namespaceMatch);

            NewMdWriter(dest, types);
        }

        private static void NewMdWriter(string dest, MarkdownableType[] types)
        {
            foreach (var g in types.GroupBy(x => x.Namespace).OrderBy(x => x.Key))
            {
                foreach (var item in g.OrderBy(x => x.Name))
                {
                    var typeDirectory = Path.Combine(dest, g.Key, item.Name[..Math.Min(30, item.Name.Length)]);
                    Directory.CreateDirectory(typeDirectory);
                    var x = new ClassSectionCreator();
                    x.Create(item, typeDirectory);
                }
            }
        }

        private static void OrginalMdWriter(string dest, MarkdownableType[] types)
        {
            // Home Markdown Builder
            var homeBuilder = new MarkdownBuilder();
            homeBuilder.Header(1, "References");
            homeBuilder.AppendLine();

            foreach (var g in types.GroupBy(x => x.Namespace).OrderBy(x => x.Key))
            {
                if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);

                homeBuilder.HeaderWithLink(2, g.Key, g.Key);
                homeBuilder.AppendLine();

                var sb = new StringBuilder();
                foreach (var item in g.OrderBy(x => x.Name))
                {
                    homeBuilder.ListLink(MarkdownBuilder.MarkdownCodeQuote(item.BeautifyName), g.Key + "#" + item.BeautifyName.Replace("<", "").Replace(">", "").Replace(",", "").Replace(" ", "-").ToLower());

                    sb.Append(item.ToString());
                }

                File.WriteAllText(Path.Combine(dest, g.Key + ".md"), sb.ToString());
                homeBuilder.AppendLine();
            }

            // Gen Home
            File.WriteAllText(Path.Combine(dest, "Home.md"), homeBuilder.ToString());
        }
    }
}
