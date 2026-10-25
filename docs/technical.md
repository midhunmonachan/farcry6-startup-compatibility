# Technical details

## Tested setups

- DODI repack, Far Cry 6 v1.5.0, Intel Core Ultra 9 275HX, RTX 5070 Ti Laptop GPU, Windows 11 26H2 build 26300.9711: reached gameplay with the original launcher.
- v0.1.1: launch confirmed on Far Cry 6 v1.5.0, Intel Core Ultra 9 and Windows 11 26H2 build 26300.9711 in [issue #1](https://github.com/midhunmonachan/farcry6-startup-compatibility/issues/1#issuecomment-6399041304). The exact CPU model and GPU were not provided.

Other game and Windows builds, save/load, and long sessions have not been verified. The original test used `ntdll.dll` version `10.0.26100.9549`.

## Compatibility checks

The launcher checks the hashes of `FarCry6.exe`, `FC_m64d3d12.dll` and `EMP.dll` against [supported-files.sha256](../supported-files.sha256). It also checks the Windows and game instructions before changing them. A mismatch stops the repair.

It does not check or edit `uplay_r2.ini`, graphics settings or saves. Custom names, email addresses, languages and save paths can stay as they are. Invalid settings can still prevent the game from working.

It does not patch or require specific hashes for `amd_ags_x64.dll`, `upc_r2_loader64.dll` or `dbdata.dll`. Replacement DLLs still need to work with the game.

## Finding the game

The launcher checks nearby folders and the DODI installation record. If it cannot find the game, select the main game folder or its `bin` folder. No fixed installation path is required.

## Startup changes

The launcher starts the game suspended and changes only that process's memory:

1. Preserves RBX across a TLS callback, then restores the original loader call.
2. Preserves RBX in an exception handler.
3. Fixes two CPU-brand comparisons that incorrectly match zero values.
4. Replaces a stalled initializer for `CNetMsgCharacterStatus_Unicast` with CRC32 `0xEC95C51C`, its initialization flag and storage address.

The initializer was reconstructed from nearby code. Reaching gameplay does not confirm stability in every part of the game. The offsets apply to the supported binaries only.

Game and Windows files stay unchanged. The launcher installs no driver or service and changes no security settings. The game can still update its own settings and saves. If the repair fails, the launcher attempts to stop the game process it started.

## Download checksums

The release includes checksums for the ZIP and its contents. Compare a download using `Get-FileHash`. The EXE is unsigned.

## Build and test

Run `build.ps1` on Windows to compile `build/FarCry6-Compatibility.exe` with the .NET Framework 4.x x64 compiler. Run `test.ps1` for folder detection and compatibility tests.

For filesystem tests, run `./test.ps1 -GameBin 'C:\Your Library\Far Cry 6\bin'`. This tests temporary copies of the three supported binaries, then removes them. It does not launch the game or edit the installation.

## Remove the launcher

Use the original `bin/FarCry6.exe`, restore any shortcuts you changed, and delete the launcher files.

## Report a problem

Include your game version, CPU, GPU, Windows build and the exact error. Include the filename if a compatibility check fails.

Released under the [MIT license](../LICENSE).
