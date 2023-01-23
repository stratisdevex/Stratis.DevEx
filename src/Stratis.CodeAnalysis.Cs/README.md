# About
This tool is a Roslyn analyzer that provides static analysis and validation of Stratis smart contract C# code.

# Installation
You need to add the Stratis.DevEx [package dev feed](https://www.myget.org/F/stratisdevex/api/v3/index.json) to your NuGet package sources e.g. in Visual Studio goto Tools->Options->NuGet Package Manager->Package Sources and add the URL https://www.myget.org/F/stratisdevex/api/v3/index.json.
![img](https://phx02pap002files.storage.live.com/y4mFcBqajfZXbydpDpjiAiulclR9coMXZSydLbTxLKGfz9tyH2m4w86rPrkZ-413id1Wx5nhdOiS6CnnLu7EHEs10pv7J80zhwTaA8WPv3-ZQ3mGB_eHI7Fke3K4rCv501KDPyf7I3PGS1vLfoQhZtzfECq2tUXp6xEWr9sVZxp1ONLZVDSDweix3scfSCO8TZ7?width=1918&height=963&cropmode=none)
Then install the latest version of the package.

# Usage
The analyzer can be configured using `%AppData%\StratisDev\stratisdev.cfg`. Logfiles are written to the same folder.


# Troubleshooting
To help diagnose issues set `Debug=True` in the global configuration file and reload the solution or project.