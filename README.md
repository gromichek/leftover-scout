# LeftoverScout

Find and safely clean up leftover data from uninstalled programs in your Windows user profile.

> **Status:** early development — the app cannot scan or clean anything yet.
> See [docs/ROADMAP.md](docs/ROADMAP.md).

## What it will do

- Scan `%USERPROFILE%` (AppData and other folders) **read-only** and show what takes space.
- Explain which items probably belong to programs that are no longer installed, and why.
- Remove only what you select — to the **Recycle Bin** by default.
- Work fully **offline**, without installation and without administrator rights.

## Requirements

- Windows 10 22H2 or Windows 11, x64.

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet build LeftoverScout.slnx --configuration Release
dotnet test LeftoverScout.slnx --configuration Release
```

## Documentation

- [User guide](docs/user-guide/en/index.md) (English)
- [Руководство пользователя](docs/user-guide/ru/index.md) (русский)
- [Architecture](docs/ARCHITECTURE.md)
- [Roadmap](docs/ROADMAP.md)
- [Contributor and session rules](CLAUDE.md)

## License

[MIT](LICENSE)
