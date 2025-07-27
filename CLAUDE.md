# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Path to Tarkov is a comprehensive mod for SPT (Single Player Tarkov) that transforms the game into an open-world experience by connecting all maps through extraction points. It consists of both server-side TypeScript components and client-side C# Unity modifications.

## Development Commands

### Build Commands
- `npm run build` - Compile TypeScript to JavaScript
- `npm run build:client` - Build C# projects (requires dotnet SDK and InteractableExfilsAPI)
- `npm run build:all` - Full build including generated files, docs, and client
- `npm run build:release` - Complete release build with zip packaging

### C# Project Build Setup
The C# projects use relative paths to find SPT assemblies. The `PathToSPT` property in the .csproj files must point to your SPT installation root. By default it's set to `../..` (2 directories up), expecting this structure:
```
SPT Installation/
├── Development/
│   └── PathToTarkov-master/
│       ├── PTT-Plugin/
│       └── PTT-Packets/
├── BepInEx/
├── EscapeFromTarkov_Data/
└── ...
```
If your directory structure differs, update the `PathToSPT` property in both .csproj files.

### Development Workflow
- `npm run dev:install` - Build and install for local SPT development
- `npm test` - Run Jest unit tests
- `npm run lint` - Run ESLint
- `npm run prettier` - Format code

### Testing
- Run a single test: `npm test -- path/to/test.spec.ts`
- Watch mode: `npm run test:watch`
- Full test suite with linting: `npm run test:all`

## Architecture

### Server-Side (TypeScript)
The main server mod structure:
- `src/mod.ts` - Entry point implementing `IPreSptLoadMod` and `IPostSptLoadMod` interfaces
- `src/path-to-tarkov-controller.ts` - Core controller managing all mod functionality
- `src/config.ts` - Configuration loading and validation
- `src/routes/` - HTTP API endpoints for client communication
- `src/services/` - Business logic services (stashes, traders, spawns, etc.)
- `src/helpers.ts` - Utility functions

Key patterns:
- Uses `tsyringe` for dependency injection
- Winston for logging
- JSON5 for configuration files
- Strict TypeScript with null checks enabled

### Client-Side (C#)
BepInEx plugin structure:
- `PTT-Plugin/` - Main Unity mod
  - `Patches/` - Harmony patches for game modifications
  - `Services/` - Client services mirroring server functionality
  - `UI/` - Custom UI components
- `PTT-Packets/` - Shared network packet definitions

Dependencies:
- BepInEx framework
- InteractableExfilsAPI (required dependency)
- Fika.Core for multiplayer support
- SPT assemblies for game integration

### Configuration System
- User configuration: `configs/UserConfig.json5`
- Pre-configured setups in `configs/` directory
- JSON5 format for comments and trailing commas
- Highly customizable with offraid positions, spawns, and trader restrictions

## Key Implementation Details

1. **Offraid Position System**: Players have a persistent location that determines available maps and traders
2. **Dynamic Spawns**: Spawn points adjusted based on offraid position and configured spawn points
3. **Multi-Stash**: Optional multiple hideout stashes at different locations
4. **Extract System**: Custom extraction UI allowing choice between transit and extraction
5. **Trader Restrictions**: Location-based trader availability with modded trader support
6. **Multiplayer**: Full Fika compatibility with individual offraid positions

## Development Notes

- Always check `InteractableExfilsAPI` is available before building client
- Use `npm run gather:external-resources` to update external map data
- Generated files in `src/_generated/` should not be edited manually
- Test configurations thoroughly as they affect core gameplay
- WebSocket communication between client and server for real-time updates