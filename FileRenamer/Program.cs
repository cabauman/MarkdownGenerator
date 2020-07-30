using System;
using System.IO;
using System.Linq;

namespace FileRenamer
{
    class Program
    {
        static void Main(string[] args)
        {
            var targetDirectory = args[0];

            IterateDirectories(targetDirectory);

            Console.ReadKey();
        }

        private static void IterateDirectories(string targetDirectory)
        {
            Console.WriteLine($"****** {targetDirectory}");
            RenameAll(targetDirectory);
            foreach (string d in Directory.GetDirectories(targetDirectory))
            {
                IterateDirectories(d);
            }
        }

        private static void RenameAll(string targetDirectory)
        {
            var files = Directory
                .EnumerateFiles(targetDirectory)
                .Where(x => Path.GetExtension(x).Equals(".md") && !x.EndsWith("index.md"));

            foreach (var file in files)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                var parts = fileNameWithoutExtension.Split(".");
                if (parts.Length != 2)
                {
                    Console.WriteLine($"Skipping {fileNameWithoutExtension} because it doesn't have exactly one [.] char.");
                    continue;
                }

                var firstPart = parts[0];
                var lastPart = parts[^1];

                var parenthesisIndex = lastPart.IndexOf('(');
                if (parenthesisIndex == -1)
                {
                    continue;
                }

                var newFileName = firstPart + "." + lastPart.Remove(parenthesisIndex) + ".md";
                var newFile = Path.Combine(targetDirectory, newFileName);
                Console.WriteLine($"Old: {fileNameWithoutExtension}, New: {newFileName}");
                File.Move(file, newFile);
            }
        }
    }
}
