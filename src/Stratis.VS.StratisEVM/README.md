## About
The StratisEVM extension provides support for developing Stratis Solidity smart contracts inside Visual Studio.

## Features
* Uses [vscode-solidity](https://github.com/juanfranblanco/vscode-solidity) language server
* Integrates with Visual Studio "Open Folder"
* Syntax highlighting and hover information
* Intellisense
* Linting
* Compile a Solidity file from inside Visual Studio

## Requirements
* Visual Studio 2022
* A recent version of [Node.js](https://nodejs.org/)

## Usage
Use the Visual Studio Open Folder... feature to open a folder with Solidity contracts. Your folder should contain one of the following files:
* remappings.txt
* foundry.toml
* brownie-config.yaml
* truffle-config.js
* hardhat.config.js
* hardhat.config.ts

which indicates it is a Solidity project folder. (This is needed for things like error-checking as-you-type by the Solidity compiler.)

Edit a Solidity file as normal. To compile a Solidity file, right-click on the file in Solution Explorer and select 'Compile Solidity File'.

The first time you open a folder with Solidity contracts the extension will install the necessary Node.js modules in the extension's private `node_modules` directory. 
This will take a few seconds to complete so opening the folder will be slower than normal but after the modules are installed the first time, opening folders containing
Solidity contracts will be as usual.
