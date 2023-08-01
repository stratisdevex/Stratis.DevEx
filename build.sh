#!/bin/bash

set -e 

dotnet restore src/Stratis.DevEx.sln && \
dotnet build src/Stratis.CodeAnalysis.Cs/Stratis.CodeAnalysis.Cs/Stratis.CodeAnalysis.Package.csproj /p:Configuration=Release && \
dotnet build src/Stratis.DevEx.Gui/Stratis.DevEx.Gui.csproj /p:Configuration=Release $*

