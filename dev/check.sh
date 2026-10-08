#!/usr/bin/env bash
# Comprobacion completa sin el juego: tests del Core + el mod entero compilado en C# 5 contra
# stubs con SOLO la API verificada de la build 719. Requiere .NET SDK 8.
set -euo pipefail
cd "$(dirname "$0")"
dotnet run --project Core.Tests --nologo
dotnet build Check --nologo -v q
dotnet build CheckPecera --nologo -v q
