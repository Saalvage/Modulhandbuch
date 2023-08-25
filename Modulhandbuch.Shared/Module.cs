using System.Text.Json.Serialization;

namespace Modulhandbuch.Shared;

// Sorted by frequency
public enum Turnus {
    EverySemester,
    EverySummerSemester,
    EveryWinterSemester,
    Yearly,
    OneTime,
    Irregular,
    SeeNotes,
}

[Flags]
public enum Language {
    Unspecified = 0,
    German = 1 << 0,
    English = 1 << 1,
}

public record Module(string Name, string Id, string[]? Responsible, string[] Facilities, string PartOf, int Credits, bool HasGrades,
    Turnus Turnus, int Duration, Language Language, int Level, int Version, string Description) {

    public static bool ParseGradingScale(string gradingScale)
        => gradingScale switch {
            "Zehntelnoten" => true,
            "best./nicht best." => false,
            _ => throw new ArgumentException($"Unknown grading scale: {gradingScale}"),
        };

    public static Turnus ParseTurnus(string turnus)
        => turnus switch {
            "Unregelmäßig" => Turnus.Irregular,
            "Jedes Sommersemester" => Turnus.EverySummerSemester,
            "Jedes Wintersemester" => Turnus.EveryWinterSemester,
            "Jedes Semester" => Turnus.EverySemester,
            "Jährlich" => Turnus.Yearly,
            "Einmalig" => Turnus.OneTime,
            "siehe Anmerkungen" => Turnus.SeeNotes,
            _ => throw new ArgumentException($"Unknown turnus: {turnus}"),
        };

    public static Language ParseLanguage(string language)
        => language == ""
            ? Language.Unspecified
            : language.Split('/').Select(x => x switch {
                "Deutsch" => Language.German,
                "Englisch" => Language.English,
                _ => throw new ArgumentException($"Unknown language: {x}"),
            }).Aggregate(Language.Unspecified, (l, r) => l | r);

    [JsonIgnore]
    public bool IsPractical => Name.Contains("Praktikum") && !Name.Contains("Seminar zum");

    [JsonIgnore]
    public bool IsSeminar => Name.Contains("Seminar");
}
