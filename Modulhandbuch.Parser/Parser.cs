using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Modulhandbuch.Shared;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig;

namespace Modulhandbuch.Parser;

public partial class Parser {
    public static void Parse(string file, string outputFile) {
        using var stream = File.OpenRead(file);
        Parse(stream, outputFile);
    }

    public static void Parse(Stream stream, string outputFile) {
        using var doc = PdfDocument.Open(stream);
        Parse(doc, outputFile);
    }

    public static void Parse(byte[] data, string outputFile) {
        using var doc = PdfDocument.Open(data);
        Parse(doc, outputFile);
    }

    private static void Parse(PdfDocument doc, string outputFile) {
        var start = DateTimeOffset.UtcNow;

        var input = string.Join("", InsertNewlines(doc.GetPages().SelectMany(x => x.Letters)).ToArray());

        static IEnumerable<string> InsertNewlines(IEnumerable<Letter> str) {
            using var enumerator = str.GetEnumerator();
            enumerator.MoveNext();
            var prev = enumerator.Current;
            yield return prev.Value;
            while (enumerator.MoveNext()) {
                var curr = enumerator.Current;
                if (prev.EndBaseLine.Y != curr.EndBaseLine.Y) {
                    yield return prev.Value == " " ? "" : "\n";
                }

                yield return curr.Value;
                prev = curr;
            }
        }

        var modules = new ConcurrentBag<Module>();

        var offset = input.IndexOf("4 Module");

        var regex = ModuleRegex();

        foreach (var match in regex.Matches(input, offset).AsEnumerable()) {
            try {
                modules.Add(new(match.Groups["name"].Value.Replace("\n", ""),
                    match.Groups["id"].Value.Replace("\n", ""),
                    match.Groups["responsible"]?.Value.Split('\n'),
                    match.Groups["facility"].Value.Split('\n'), match.Groups["partOf"].Value,
                    int.Parse(match.Groups["credits"].ValueSpan),
                    Module.ParseGradingScale(match.Groups["gradingscale"].Value),
                    Module.ParseTurnus(match.Groups["turnus"].Value),
                    int.Parse(match.Groups["duration"].ValueSpan), Module.ParseLanguage(match.Groups["language"].Value.Replace("\n", "")),
                    int.Parse(match.Groups["level"].ValueSpan), int.Parse(match.Groups["version"].ValueSpan),
                    match.Groups["description"].Value));
            } catch (Exception) {
                Console.WriteLine(match.Value);
                throw;
            }
        }

        var str = JsonSerializer.Serialize(modules, SerializerOptions.Default);
        File.WriteAllText(outputFile, str);

        var indexRegex = IndexModuleRegex();
        var indexMatches = indexRegex.Matches(input);
        var missing = indexMatches.Select(x => x.Groups["id"].Value.Replace("\n", "")).Except(modules.Select(x => x.Id)).ToArray();
        if (indexMatches.Count != modules.Count) {
            Console.WriteLine("Missing: \n" + string.Join('\n', missing));
        }

        Console.WriteLine($"Converted in {(DateTimeOffset.UtcNow - start).TotalSeconds} seconds");
    }

    [GeneratedRegex(@"\d+ Modul: (?<name>[\S\s]*?) \[(?<id>[\S\s]*?)\]\n(Verantwortung:(?<responsible>[\S\s]*?)\n)?Einrichtung:(?<facility>[\s\S]*?)\nBestandteil von:(?<partOf>[\s\S]*?)( |\n)Leistungspunkte\n(?<credits>\d+)\nNotenskala\n(?<gradingscale>.*?)\nTurnus\n(?<turnus>.*?)\nDauer\n(?<duration>.*?) Sem(ester|\.)\n(Sprache\n(?<language>[\S\s]*?)\n)?Level\n(?<level>\d+)\nVersion\n(?<version>\d+)\n(?<description>[\S\s]*?)(4 MODULE|5 TEILLEISTUNGEN)")]
    private static partial Regex ModuleRegex();

    [GeneratedRegex(@"(?<=4\.\d+\.\s)(?<name>.*?)\s-\s(?<id>M[\s\S]*?)(?=\.)")]
    private static partial Regex IndexModuleRegex();
}
