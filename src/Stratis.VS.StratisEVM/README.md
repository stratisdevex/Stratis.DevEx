## About
The StratisEVM extension provides support for developing Stratis Solidity smart contracts inside Visual Studio.
![img](https://stratisdevex.gallerycdn.vsassets.io/extensions/stratisdevex/stratisevm/0.1.3/1725544756658/stratis.vs.stratisevm.gif)
## Features
* Solidity project system featuring support for managing smart contract dependencies and Solidity compiler integration with MSBuild
* Integrates with Visual Studio solutions and "New Project..." dialog
* Integrates with Visual Studio "Open Folder..." feature
* Uses [vscode-solidity](https://github.com/juanfranblanco/vscode-solidity) language server
* Syntax highlighting and hover information
* Intellisense
* Linting
* Compile Solidity projects and files from inside Visual Studio or on the command-line using MSBuild with error reporting
* Generate .NET bindings to Solidity smart contracts from inside Visual Studio

## Requirements
* Visual Studio 2022
* A recent version of [Node.js](https://nodejs.org/)

## Usage
Add a Solidity project to your solution using the "New Project..." dialog or use the Visual Studio "Open Folder..." feature to open a folder with Solidity contracts. 
If using the Open Folder... feature, your folder should contain one of the following files:
* remappings.txt
* foundry.toml
* brownie-config.yaml
* truffle-config.js
* hardhat.config.js
* hardhat.config.ts

Edit Solidity files as normal. Use Visual Studio's package.json editing support to add your smart contract dependencies and right-click on an existing package.json
and select "Install NPM dependencies". To build a Solidity project use the Build menu commands and shortcuts as usual or run `dotnet build MyProj.solproj` from the command-line.
To compile an individual Solidity file or when using the Open Folder... feature, right-click on the file in Solution Explorer and select 'Compile Solidity File'.

The very first time you open a Solidity file the extension will install the necessary Node.js modules in the extension's private `node_modules` directory. 
This will take a few seconds to complete so Intellisense won't be available during installation but after the modules are installed the first time, 
editing Solidity contracts will be as usual.