# ImageDescriber

A .NET 10 CLI that uses a local Ollama vision model to generate descriptions and suggested filenames for images in bulk.

## What it does

Recursively scans a directory for images (`.jpg`, `.png`, `.gif`, `.bmp`, `.webp`, `.tiff`), computes a content hash for each file, and asks a local Ollama instance to caption each unique image. Descriptions, suggested filenames, and metadata are persisted in a JSON database keyed by the content hash, so identical images at different paths share one record and re-scanning skips already-described content.

No cloud APIs are called — all inference runs locally against Ollama.

## Prerequisites

- [Ollama](https://ollama.com) running locally or on the network (defaults to `http://localhost:11434`).
- A vision-capable model installed in Ollama (default `llama3.2-vision`):
  ```bash
  ollama pull llama3.2-vision
  ollama serve
  ```
- .NET 10 SDK.

## Installation

```bash
git clone <repo>
cd ImageDescriber
dotnet build
```

## Usage

Without arguments the tool opens an interactive menu. All verbs can also be invoked directly.

```bash
# Interactive menu
ImageDescriber

# Scan a directory
ImageDescriber Scan -p "C:\photos"

# Scan with a custom model and remote endpoint
ImageDescriber Scan -p "C:\photos" -m llava -e http://192.168.1.100:11434

# Search stored descriptions
ImageDescriber Search -q "dog"

# Export / import the database
ImageDescriber Export -o descriptions.csv      # or .json
ImageDescriber Import -i backup.json            # or .csv
```

### Verbs

| Verb | Purpose |
|---|---|
| `Menu` *(default)* | Interactive console menu. |
| `Scan` | Hash images in a directory and describe each unique one. |
| `Search` | Keyword search across stored descriptions and paths. |
| `Configure` | Edit endpoint, model, concurrency, and prompt templates. |
| `Export` | Dump the database to JSON or CSV. |
| `Import` | Merge a JSON or CSV export back into the database. |

### Common options

| Option | Long form | Effect |
|---|---|---|
| `-p` | `--path` | Directory to scan (`Scan`) or default path. |
| `-e` | `--endpoint` | Ollama URL. Defaults to `http://localhost:11434`. |
| `-m` | `--model` | Vision model name. Defaults to `llama3.2-vision`. |
| `-q` | `--query` | Search query (`Search`). |
| `-o` | `--output` | Export file path. The extension picks the format. |
| `-i` | `--input` | Import file path. |

## Storage

Settings and the description database are stored via `ktsu.AppDataStorage` (typically `%APPDATA%\ktsu\ImageDescriber` on Windows).

## License

MIT — see `LICENSE.md`.
