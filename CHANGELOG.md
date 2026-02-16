## v1.1.0 (minor)

Changes since v1.0.0:

- Add unit tests and implement image processing features ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove legacy build scripts ([@matt-edmondson](https://github.com/matt-edmondson))
- Add RoundTripStringJsonConverter to Import and Export verbs for improved JSON handling ([@matt-edmondson](https://github.com/matt-edmondson))
- Add Import verb for importing image descriptions from JSON and CSV files ([@matt-edmondson](https://github.com/matt-edmondson))
- Increase HttpClient timeout to 10 minutes for improved request handling ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance image description prompt by including known file paths for context ([@matt-edmondson](https://github.com/matt-edmondson))
- Add MaxConcurrentRequests property and update Scan verb for configurable concurrency ([@matt-edmondson](https://github.com/matt-edmondson))
- Update DescriptionPrompt for clarity and specificity; enhance image description guidelines ([@matt-edmondson](https://github.com/matt-edmondson))
- Reset PathString to default after scan completion; improve state management ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.8-pre.1 (prerelease)

Changes since v1.0.7:

- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))

## v1.0.7 (patch)

Changes since v1.0.6:

- Remove legacy build scripts ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.7-pre.1 (prerelease)

No significant changes detected since v1.0.7.

## v1.0.6 (patch)

Changes since v1.0.5:

- Add RoundTripStringJsonConverter to Import and Export verbs for improved JSON handling ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.5 (patch)

Changes since v1.0.4:

- Add Import verb for importing image descriptions from JSON and CSV files ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.4 (patch)

Changes since v1.0.3:

- Increase HttpClient timeout to 10 minutes for improved request handling ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.3 (patch)

Changes since v1.0.2:

- Enhance image description prompt by including known file paths for context ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.2 (patch)

Changes since v1.0.1:

- Add MaxConcurrentRequests property and update Scan verb for configurable concurrency ([@matt-edmondson](https://github.com/matt-edmondson))
- Update DescriptionPrompt for clarity and specificity; enhance image description guidelines ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.1 (patch)

Changes since v1.0.0:

- Reset PathString to default after scan completion; improve state management ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.0 (major)

- Refactor Scan verb to deduplicate image paths and enhance description handling; improve output for discovered images ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor image file scanning logic to improve extension checking; streamline file path handling ([@matt-edmondson](https://github.com/matt-edmondson))
- Implement user input for path validation in Scan verb; enhance error handling for empty paths ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance ImageDescription to track multiple known paths; update export and search functionalities to reflect changes ([@matt-edmondson](https://github.com/matt-edmondson))
- Add suggested filename handling and prompts for image descriptions; update related classes and export functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor code to use specific types for file paths and model settings; improve type safety and clarity across multiple classes. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor ImageDescription to use specific types for FilePath and FileName; add OllamaEndpoint and OllamaModelName records ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor file handling to use AbsoluteFilePath for improved type safety and clarity ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor menu item creation to include help text in the display ([@matt-edmondson](https://github.com/matt-edmondson))
- Add Configure verb for Ollama endpoint and model settings ([@matt-edmondson](https://github.com/matt-edmondson))
- Initial Commit ([@matt-edmondson](https://github.com/matt-edmondson))
- Initial Commit ([@matt-edmondson](https://github.com/matt-edmondson))

