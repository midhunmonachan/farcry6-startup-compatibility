using System;
using System.IO;

internal static class LocationTests
{
    static int failures;
    static int checks;

    static string Find(string launcherFolder, string[] installations)
    {
        return GameLocation.Find(launcherFolder, installations);
    }

    static void Check(string name, string expected, string actual)
    {
        checks++;
        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase)) {
            failures++;
            Console.WriteLine("FAIL " + name + ": expected " + (expected ?? "no match") + ", got " + (actual ?? "no match"));
        } else Console.WriteLine("PASS " + name);
    }

    static int Main()
    {
        string baseFolder = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
        string fixture = Path.Combine(baseFolder, "location-fixtures-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fixture);
        try {
            string game = Path.Combine(fixture, "Different Library", "Far Cry 6");
            string bin = Path.Combine(game, "bin");
            string compatibility = Path.Combine(game, "Compatibility");
            string downloads = Path.Combine(fixture, "Downloads");
            Directory.CreateDirectory(bin);
            Directory.CreateDirectory(compatibility);
            Directory.CreateDirectory(downloads);
            File.WriteAllText(Path.Combine(bin, "FarCry6.exe"), "test fixture only");

            Check("launcher in another game root with spaces", bin, Find(game, new string[0]));
            Check("launcher beside the game executable", bin, Find(bin, new string[0]));
            Check("existing Compatibility-folder layout", bin, Find(compatibility, new string[0]));
            Check("registered install found from Downloads", bin, Find(downloads, new string[] { game }));
            Check("registered bin path accepted", bin, Find(downloads, new string[] { bin }));
            Check("missing installation leaves selection to user", null, Find(downloads, new string[0]));
            Check("invalid registry paths do not hide a valid install", bin, Find(downloads, new string[] { null, "", "bad\0path", game }));
            Console.WriteLine(checks + " checks, " + failures + " failures");
            return failures == 0 ? 0 : 1;
        }
        finally {
            string resolved = Path.GetFullPath(fixture);
            if (!resolved.StartsWith(baseFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar + "location-fixtures-", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unexpected fixture cleanup path.");
            Directory.Delete(resolved, true);
        }
    }
}
