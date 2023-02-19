# About
This tool is a Roslyn analyzer that provides static analysis and validation of Stratis smart contract C# code.

# Installation
You need to add the Stratis.DevEx [package dev feed](https://myget.org/gallery/stratisdevex) to your NuGet package sources e.g. in Visual Studio goto Tools->Options->NuGet Package Manager->Package Sources and add the URL https://www.myget.org/F/stratisdevex/api/v3/index.json.
![img](https://phx02pap002files.storage.live.com/y4mFcBqajfZXbydpDpjiAiulclR9coMXZSydLbTxLKGfz9tyH2m4w86rPrkZ-413id1Wx5nhdOiS6CnnLu7EHEs10pv7J80zhwTaA8WPv3-ZQ3mGB_eHI7Fke3K4rCv501KDPyf7I3PGS1vLfoQhZtzfECq2tUXp6xEWr9sVZxp1ONLZVDSDweix3scfSCO8TZ7?width=1918&height=963&cropmode=none)
Make sure you select the right package source and also to include prerelease packages in the NuGet browser window. Then install the latest version of the package into your smart contract project.

# Usage
The analyzer can be configured using `%AppData%\StratisDev\stratisdev.cfg`. Logfiles are written to the same folder.

# Troubleshooting
To help diagnose issues set `Debug=True` in the global configuration file and reload the solution or project.

# Code Analysis Rules

## Validation rules
These are the rules implemented by the [validator module](https://github.com/stratisdevex/Stratis.DevEx/blob/master/src/Stratis.CodeAnalysis.Cs/Stratis.CodeAnalysis.Cs/Validator.cs) of the smart contract Roslyn analyzer based on the [CLR execution and validation](https://github.com/stratisproject/StratisFullNode/blob/master/Documentation/Features/SmartContracts/Clr-execution-and-validation.md) Stratis doc:
### Format validation
| Id | Description | Severity |
| --- | ----------- | ------  |
| SC0001 | Smart contract classes cannot be declared inside a namespace. | Error |
| SC0002 | Types from this namespace cannot be used in smart contract code. | Error |
| SC0003 | Classes in smart contract code must inherit from Stratis.SmartContract. | Error |
| SC0004 | The first parameter in a smart constructor must be of type ISmartContractState. | Error |
| SC0005 | New object creation of reference types is not allowed in smart contract code. | Error | 
| SC0006 | Non-const field declarations are not allowed in smart contract classes. | Error |
| SC0007 | Only certain variable types can be used in smart contract code. | Error |
| SC0008 | Only certain types and members can be used in smart contract code. | Error |
| SC0009 | Cannot use this method here. | Error |
| SC0010 | An assert condition should be derived from input or state. | Warning |
| SC0011 | Custom assert message should be used, as this can be parsed to identify reason for failure. | Info |
| SC0012 | Assert message should not be empty, as this can be parsed to identify reason for failure. | Info |
| SC0013 | This type cannot be used in smart contract code. | Error | 
| SC0014 | This type cannot be used as a smart contract method return type or parameter type. | Error | 
| SC0017 | Smart contract assemblies cannot directly reference any other .NET assembly except Stratis.SmartContracts. | Error |
| SC0018 | Only one class in a smart contract assembly can be marked with the Deploy attribute. | Error |
| SC0019 | A smart contract type cannot declare a static constructor or property or field. | Error |
| SC0020 | A smart contract  type cannot contain generic parameters. | Error |


### Determinism validation
| Id | Description | Severity |
| --- | ----------- | ------  |
| SC0015 | A smart contract class cannnot declare a destructor or finalizer. | Error |
| SC0016 | Exception handling with try/catch blocks not allowed in smart contract code. | Error | 




