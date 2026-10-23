# Technical details

## Supported setup

- Game directory: `C:\Games\FarCry6\bin`. This path is hardcoded in the original tested launcher.
- The seven files listed in [supported-files.sha256](../supported-files.sha256) must match. The launcher checks their hashes before starting the game. An edited `uplay_r2.ini` also causes rejection.
- Tested CPU: Intel Core Ultra 9 275HX. Tested discrete GPU: NVIDIA GeForce RTX 5070 Ti Laptop GPU.
- Windows registry reported version 26H2, build 26300.9711. Local `ntdll.dll` file version: `10.0.26100.9549`; SHA-256: `651AC9041AF8E6B5EBD331D53163244C0A27D36D1433F0F15CAC1F28C5F2E82C`.

The executable checks selected instruction bytes in the Windows loader and game startup code. **It does not check the entire Windows DLL hash or Windows version.** These guards are not proof that another Windows build is compatible. Other builds need review before use; do not remove the guards to force it to run.

The tested installation was a DODI repack. Its official trial launched on the same laptop, and all 312 shared files in an older installation backup matched the installed copy by SHA-256. This repository contains no game binaries, repack archives, cracked DLLs, saves, Windows DLLs, or download links for them. It requires the user's existing matching installation and does not install or supply a game or entitlement.

## Verify the download

Back up your saves separately before testing experimental software. The tested executable's SHA-256 is:

   ```text
   67D69D83566C494538AD9106BAB7D924AFBE0521A2E42D86EF8D90CF09A70390
   ```

   ```powershell
   Get-FileHash -LiteralPath .\FarCry6-Compatibility.exe -Algorithm SHA256
   ```

The launcher exits after completing the startup changes, leaving the game running. If it reports an unsupported file or loader, stop and retain the exact error text for a report. Do not bypass antivirus warnings or disable system protections to run it.

It needs no administrator privileges for a normally installed game, no driver, no service, and no Defender exclusion. The release is unsigned. A checksum verifies that a file matches this release; it is not a security audit.

The original tested executable is unchanged. To support another folder, a developer can change `GameDirectory` in `src/Launcher.cs` and rebuild; that creates a different, untested executable. Do not move or reinstall a working game just to try this release.

## What changes

The launcher creates its own game process suspended and applies changes only to that process's private memory:

1. A temporary loader thunk preserves RBX across a game TLS callback. The original loader call is restored after the startup callback finishes.
2. A repack exception-handler edit prevents RBX from being overwritten before it is saved.
3. Two branch edits avoid an empty CPU-brand block being treated as a match for ordinary zero register values.
4. A replacement initializer stores CRC32 `0xEC95C51C` for `CNetMsgCharacterStatus_Unicast`, sets the initialization flag, and returns its storage address.

The final initializer was reconstructed from neighboring getters and startup behavior. Reaching gameplay is encouraging, but does not establish complete semantic equivalence or long-term stability. These edits are specific to the hashes and offsets in the source.

Game and Windows files on disk are not patched. The tool does not install persistence, access the network, upload anything, or change security settings. Normal game execution can still write game settings and saves. If startup repair fails, the launcher attempts to terminate only the game process it created.

## Build from source

From the repository root, run `build.ps1` with Windows PowerShell and the Windows .NET Framework 4.x compiler installed. It produces `build\FarCry6-Compatibility.exe`.

The bundled release is the previously tested executable. A fresh compilation can have a different checksum because compiler versions and PE timestamps can differ. Compilation verifies source buildability; it does not repeat the gameplay test.

## Undo

Launch the original `bin\FarCry6.exe` to stop using the workaround. If you changed a shortcut, restore its target to the original executable. Delete this tool's files when no longer needed. No game-file replacement or system rollback is required for the memory changes.

## Reporting results

Include your Windows build, `ntdll.dll` version/hash, CPU, game-file hash match results, exact launcher error, and whether menu access, gameplay, save/load, and a second launch work. Do not upload saves, account details, full memory dumps, game files, or cracked DLLs. A report from a different setup is not yet a supported configuration.

This project is provided as-is under the MIT license. It is independent of Ubisoft and Microsoft.

