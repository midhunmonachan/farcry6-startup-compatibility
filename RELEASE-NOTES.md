A compatibility launcher for the DODI repack of Far Cry 6 v1.5.0 when it closes or hangs at startup.

v0.1.2 finds the Windows loader repair call at launch instead of using fixed addresses. It also ignores game processes that have already exited. If the loader's instructions change in a future Windows version, the launcher stops without applying the repair.

Tested on Windows build 26300 with `ntdll.dll` 10.0.26100.9974: the game launched and played the opening video. Full gameplay on this build was not tested.

Download the ZIP, extract it, and run `FarCry6-Compatibility.exe`.

Experimental. See the [tested setups](https://github.com/midhunmonachan/farcry6-startup-compatibility/blob/main/docs/technical.md).
