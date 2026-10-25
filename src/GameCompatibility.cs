using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

internal static class GameCompatibility
{
    // These files determine the executable layout and the two game modules patched below.
    // User settings and modules that the repair does not patch are not build fingerprints.
    static readonly Dictionary<string,string> Supported = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {
        {"FarCry6.exe","95C71228D719E8FD68BEE4139B0C5ED253B34B7434698A77AC8E4D2ADAABCCD9"},
        {"FC_m64d3d12.dll","64A58E8A068ACE1CF10DEC0698C2CEEC0741E3E060F4498CCBCA2AADEAC3B3EF"},
        {"EMP.dll","D6041735C6769FEC381C85DF1117EF58240AA2DBC4802B99963DDCA739B4203A"}
    };

    internal static bool AcceptsHash(string fileName, string actualHash)
    {
        string expected;
        return !Supported.TryGetValue(fileName, out expected) || string.Equals(expected, actualHash, StringComparison.OrdinalIgnoreCase);
    }

    internal static void Verify(string gameDirectory)
    {
        foreach (string fileName in Supported.Keys) {
            string actual;
            using (var file = File.OpenRead(Path.Combine(gameDirectory, fileName)))
            using (var hash = SHA256.Create()) actual = BitConverter.ToString(hash.ComputeHash(file)).Replace("-", "");
            if (!AcceptsHash(fileName, actual))
                throw new InvalidOperationException("Unsupported game build: " + fileName + " does not match the supported build. No startup repair was applied.");
        }
    }
}
