# Technical details

## Compatibility and testing

- Tested repair: DODI installation with the executable and two patched game-module hashes in [supported-files.sha256](../supported-files.sha256).
- Laptop: Intel Core Ultra 9 275HX, NVIDIA GeForce RTX 5070 Ti Laptop GPU.
- Windows registry: 26H2, build 26300.9711. Local ntdll.dll version: `10.0.26100.9549`; SHA-256: `651AC9041AF8E6B5EBD331D53163244C0A27D36D1433F0F15CAC1F28C5F2E82C`.
- The owner reported actual gameplay on October 23, 2026 with the original fixed-path launcher. Save/load and longer sessions remain unverified.
- The portable launcher adds folder detection and a folder picker while retaining the startup repair code. Version 0.1.1 removes unrelated configuration/dependency hash restrictions. Automated checks cover folder detection and compatibility decisions. The updated portable executable and folder-picker interaction have not had a separate gameplay/UI test. Other Windows/game builds and CPUs are unverified.

The launcher verifies `FarCry6.exe`, `FC_m64d3d12.dll` and `EMP.dll`, plus the selected Windows/game instruction bytes before patching. These checks protect the executable layout and the modules that the repair changes. It does not verify the full Windows DLL hash or the Windows version. These guards are not proof of compatibility with other Windows builds; those need review before use. Do not remove the guards to force an incompatible setup to run.

The launcher does not read, hash or rewrite `uplay_r2.ini`, graphics preferences or saves. Email, username, profile ID, language, save location, comments, encoding and line endings therefore do not cause a launcher build mismatch. Invalid settings can still prevent the game itself from working; the launcher does not validate them.

`amd_ags_x64.dll`, `upc_r2_loader64.dll` and `dbdata.dll` are not patched by the launcher and are no longer pinned to a particular hash. This removes unrelated rejections; it does not establish that every replacement DLL works. Modifications to the executable or either patched module still fail the build check.

## Finding the game

The launcher looks beside itself, in a nearby bin folder, then in the DODI uninstall registration. If it cannot locate the game, it opens a folder picker. Select the main Far Cry 6 folder or its bin folder. Cancel exits without launching the game. No fixed drive or folder name is required and no location setting is saved.

Only a folder containing FarCry6.exe is selected. The three-file hash verification runs before creating the game process.

## Startup changes

The launcher starts its own game process suspended and changes only that process's memory:

1. A temporary loader thunk preserves RBX across a game TLS callback. The original loader call is restored after the startup callback finishes.
2. An exception-handler edit prevents RBX from being overwritten before it is saved.
3. Two branch edits avoid an empty CPU-brand block being treated as a match for ordinary zero register values.
4. A replacement initializer stores CRC32 `0xEC95C51C` for `CNetMsgCharacterStatus_Unicast`, sets the initialization flag and returns its storage address.

The final initializer was reconstructed from neighboring getters and startup behavior. Reaching gameplay does not prove complete semantic equivalence or long-term stability. All edits remain specific to the hashes and offsets in the source.

Original game and Windows files stay unchanged. The launcher reads the game's installation registration; it does not write registry settings, install a driver/service, access the network, upload files or change security settings. Normal game execution can write its settings and saves. A failed repair attempts to terminate only the game process created by this launch.

## Verify a download

The release ZIP has SHA256SUMS.txt for its contents, plus a separate ZIP checksum asset. Run Get-FileHash on the download to compare it with that checksum. A checksum identifies the published file; it is not a security audit. The executable is unsigned. Do not disable security protections to run it.

The original fixed-path executable used for the gameplay test had SHA-256 `67D69D83566C494538AD9106BAB7D924AFBE0521A2E42D86EF8D90CF09A70390`. The portable release is a different build; use its published checksum.

## Build and test

On Windows, run build.ps1 from the repository root. It uses the Windows .NET Framework 4.x x64 compiler and writes build/FarCry6-Compatibility.exe. Run test.ps1 for the folder-discovery and compatibility checks. Build outputs are ignored by Git. Compiler versions and PE timestamps can produce different executable hashes.

For optional filesystem integration checks, run `./test.ps1 -GameBin 'C:\Your Library\Far Cry 6\bin'`. This copies the three supported binaries into a temporary test folder, verifies that locked custom settings/dependencies are not accessed, and checks rejection of modified binaries. It does not launch the game or edit the supplied installation, and removes its temporary copies afterward.

## Undo

Use the original bin/FarCry6.exe or restore your old shortcut target, then remove the launcher files. No game-file replacement or system rollback is required for the memory changes.

## Reporting results

Include Windows build, ntdll.dll version/hash, CPU, game-file hash results, exact launcher error and whether gameplay/save/load/relaunch work. Keep account details, saves, full memory dumps and game/Windows binaries out of reports.

The project includes only launcher code and its compiled executable. It is independent of Ubisoft and Microsoft, provided as-is under the MIT license.
