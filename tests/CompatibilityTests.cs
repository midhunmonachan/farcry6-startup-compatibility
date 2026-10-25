using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

internal static class CompatibilityTests
{
    static int checks, failures;

    static void Check(string name, bool expected, bool actual)
    {
        checks++;
        if (expected != actual) { failures++; Console.WriteLine("FAIL " + name); }
        else Console.WriteLine("PASS " + name);
    }

    static string Hash(string content)
    {
        using (var hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(content))).Replace("-", "");
    }

    static int Main(string[] args)
    {
        // Catch the original false rejection without needing game binaries or starting the game.
        string[] personalConfigurations = {
            "[Settings]\nEmail=player@example.invalid\nUsername=Player\n",
            "[Settings]\r\nUsername=Another Player\r\nEmail=\r\n",
            "[Settings]\nUserId=custom-profile\nLanguage=fr-FR\nSaveType=2\nSavePath=D:\\My Saves\\Far Cry 6\n",
            "\uFEFF; edited in Notepad\r\n[Settings]\r\nUsername = Player\r\n",
            "[Settings]\nUsername=玩家\nLanguage=zh-CN\n"
        };
        for (int i = 0; i < personalConfigurations.Length; i++)
            Check("personal INI variation " + (i + 1) + " is not a build mismatch", true,
                GameCompatibility.AcceptsHash("uplay_r2.ini", Hash(personalConfigurations[i])));

        Check("graphics preferences are not build identity", true,
            GameCompatibility.AcceptsHash("GamerProfile.xml", Hash("<Profile><Graphics Quality='High'/></Profile>")));
        foreach (string name in new[] { "amd_ags_x64.dll", "upc_r2_loader64.dll", "dbdata.dll" })
            Check("unpatched dependency " + name + " is not pinned by the repair", true,
                GameCompatibility.AcceptsHash(name, Hash("different dependency")));

        string[,] patched = {
            { "FarCry6.exe", "95C71228D719E8FD68BEE4139B0C5ED253B34B7434698A77AC8E4D2ADAABCCD9" },
            { "FC_m64d3d12.dll", "64A58E8A068ACE1CF10DEC0698C2CEEC0741E3E060F4498CCBCA2AADEAC3B3EF" },
            { "EMP.dll", "D6041735C6769FEC381C85DF1117EF58240AA2DBC4802B99963DDCA739B4203A" }
        };
        for (int i = 0; i < patched.GetLength(0); i++) {
            string name = patched[i, 0], supported = patched[i, 1];
            Check(name + " supported hash accepted", true, GameCompatibility.AcceptsHash(name, supported));
            Check(name + " changed build rejected", false, GameCompatibility.AcceptsHash(name, Hash("modified binary")));
            Check(name + " missing hash rejected", false, GameCompatibility.AcceptsHash(name, null));
        }
        Check("Windows filename casing retains binary checks", false, GameCompatibility.AcceptsHash("emp.DLL", Hash("modified binary")));
        Check("checksum casing does not reject the same build", true, GameCompatibility.AcceptsHash("FarCry6.exe", patched[0, 1].ToLowerInvariant()));

        if (args.Length == 1) {
            GameCompatibility.Verify(args[0]);
            Console.WriteLine("PASS installed build validates without launching or editing the game");
            checks++;
        }
        Console.WriteLine(checks + " compatibility checks, " + failures + " failures");
        return failures == 0 ? 0 : 1;
    }
}
