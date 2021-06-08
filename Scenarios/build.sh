#!/usr/bin/env bash

# When a new simulation is added, pleas append the name of the C# project to this string
models="SOHFerryTransferBox SOHGreen4BikesBox SOHBicycleRealTimeBox"

for val in $models; do
    echo "Build box for model ${val}" 
    output=$val
    
    cd "$val" || exit 
    rm -rf "$val"
    dotnet publish -c Release -r osx-x64 /p:DebugSymbols=false /p:DebugType=None \
      -p:PublishSingleFile=true --self-contained true -o "${output}"/"${output}"_MACOSX && \
      cp -r resources/ ./"${output}"/"${output}"_MACOSX/resources && \
      cp config.json ./"${output}"/"${output}"_MACOSX/
    
      
    dotnet publish -c Release -r win-x64 /p:DebugSymbols=false /p:DebugType=None \
      -p:PublishSingleFile=true --self-contained true -o "${output}"/"${output}"_WINDOWS && \
      cp -r resources/ ./"${output}"/"${output}"_WINDOWS/resources && \
      cp config.json ./"${output}"/"${output}"_WINDOWS/
      
    dotnet publish -c Release -r linux-x64 /p:DebugSymbols=false /p:DebugType=None \
      -p:PublishSingleFile=true --self-contained true -o "${output}"/"${output}"_LINUX && \
      cp -r resources/ ./"${output}"/"${output}"_LINUX/resources && \
      cp config.json ./"${output}"/"${output}"_LINUX/
    
    zip -r ../"${output}"_WINDOWS.zip ./"${output}"/"${output}"_WINDOWS/
    zip -r ../"${output}"_MACOSX.zip ./"${output}"/"${output}"_MACOSX/
    zip -r ../"${output}"_LINUX.zip ./"${output}"/"${output}"_LINUX/
    
    cd ..
done