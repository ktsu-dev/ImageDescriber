## v1.2.12 (patch)

Changes since v1.2.11:

- Reject a file passed to Scan -p instead of crashing [patch] (Claude)
- Report an oversized image as a failure instead of crashing the scan [patch] (matt-edmondson)
- Check the part before the first dot for a Windows device name [patch] (Claude)

## v1.2.12-pre.1 (prerelease)

Changes since v1.2.11:

- Bump MSTest.Sdk from 4.5.1 to 4.5.2 (dependabot[bot])

## v1.2.11 (patch)

Changes since v1.2.10:

- Merge main into fix/181-explicit-dot-path (Claude)
- Scan an explicit -p . instead of prompting for a path [patch] (Claude)

## v1.2.10 (patch)

Changes since v1.2.9:

- Cap a suggested file name at 150 UTF-8 bytes, cut back to a whole word [patch] (Claude)
- Skip a null entry in a JSON import instead of crashing [patch] (Claude)

## v1.2.10-pre.1 (prerelease)

Changes since v1.2.9:

- Bump ktsu.RoundTripStringJsonConverter and ktsu.Semantics.Paths (dependabot[bot])
- Bump Polyfill from 11.4.4 to 11.4.5 (dependabot[bot])

## v1.2.9 (patch)

Changes since v1.2.8:

- Cover the semantic-validation failure on JSON import (Claude)
- Report a malformed JSON import instead of crashing [patch] (Claude)

## v1.2.9-pre.1 (prerelease)

Changes since v1.2.8:

- Bump Polyfill from 11.4.3 to 11.4.4 (dependabot[bot])
- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.2.8 (patch)

Changes since v1.2.7:

- Use a test input that is a legal extension on Windows too (Claude)
- Drop a trailing period from a suggested file name [patch] (Claude)

## v1.2.7 (patch)

Changes since v1.2.6:

- Suggest file names Windows accepts on every platform [patch] (Claude)

## v1.2.6 (patch)

Changes since v1.2.5:

- Join the lock path instead of combining it (Claude)
- Lock the description store while a run writes to it [patch] (Claude)

## v1.2.5 (patch)

Changes since v1.2.4:

- Cover the remaining failure exits in ProgramTests (Claude)
- Exit non-zero when arguments do not parse or a verb fails [patch] (Claude)

## v1.2.4 (patch)

Changes since v1.2.3:

- Normalize imported hashes to lower case and default their null fields [patch] (Claude)

## v1.2.4-pre.5 (prerelease)

Changes since v1.2.4-pre.4:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.2.4-pre.4 (prerelease)

Changes since v1.2.4-pre.3:

- Bump Polyfill from 11.4.2 to 11.4.3 (dependabot[bot])
- Bump the ktsu group with 1 update (dependabot[bot])

## v1.2.4-pre.3 (prerelease)

Changes since v1.2.4-pre.2:

- Bump ktsu.Semantics.Paths from 5.10.0 to 5.11.0 (dependabot[bot])
- Bump Polyfill from 11.4.1 to 11.4.2 (dependabot[bot])

## v1.2.4-pre.2 (prerelease)

Changes since v1.2.4-pre.1:

- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.2.4-pre.1 (prerelease)

Changes since v1.2.3:

- Bump the ktsu group with 5 updates (dependabot[bot])

## v1.2.3 (patch)

Changes since v1.2.2:

- Move CI onto the shared ci-shared.yml pipeline (Claude)

## v1.2.2 (patch)

Changes since v1.2.1:

- Use Assert.Contains in the new Configure test (MSTEST0046) (Claude)
- Use Assert.Contains in the new menu tests (MSTEST0046) (Claude)
- Cover Configure's endpoint choice and drop unreachable catches (Claude)
- Prompt for Export's path and Search's query when run from the menu [patch] (Claude)
- Accept "localhost:11434" and trailing slashes as the Ollama endpoint [patch] (Claude)
- Keep CSV-exported paths that contain "; " intact [patch] (Claude)

## v1.2.1 (patch)

Changes since v1.2.0:

- Show a numbered menu off Windows instead of crashing [patch] (Claude)
- Keep fenced and dotted filename suggestions intact [patch] (Claude)
- Skip unreadable folders when scanning for images [patch] (Claude)

## v1.2.0 (minor)

Changes since v1.1.0:

- Move Scan's failure summary into PrintFailureSummary (Claude)
- Split quoted-field reading out of ParseCsvRecords (Claude)
- Cover Scan's failure summary with an end-to-end test (Claude)
- Use a Windows-legal suggested name in the CSV round-trip test (Claude)
- Keep scanning when one file or one model response fails (Claude)
- Round-trip CSV exports that contain line breaks, commas or quotes (Claude)
- Stop a short imported hash crashing every Search (Claude)
- Scan image files whose extensions are upper-case (Claude)
- fix: add AUTHORS.md so every identity keeps the ktsu. prefix [patch] (matt-edmondson)
- Gate Dependabot auto-merge on CI actually being green (Claude)
- ci: adopt the consolidated .NET workflow [patch] (Claude)
- ci: make the SonarQube quality gate opt in [patch] (matt-edmondson)
- ci: adopt the unified dotnet workflow [patch] (matt-edmondson)
- chore: store icon.png in LFS as .gitattributes declares (matt-edmondson)
- docs: scope build badge to the default branch (matt-edmondson)
- docs: correct README, DESCRIPTION and TAGS metadata (matt-edmondson)
- Stop Update SDKs failing when there is nothing to update (matt-edmondson)
- Fix ktsu.Sdk 2.26+ analyzer errors: new one-line copyright header (IDE0073), normalize line endings to LF (IDE0055), PrivateAssets="all" on Polyfill (KTSU0007) [patch] (matt-edmondson)
- Sync .github\workflows\dotnet.yml (KtsuTools)
- Sync .runsettings (KtsuTools)
- Sync .editorconfig (KtsuTools)
- Sync .gitattributes (KtsuTools)
- Sync global.json (KtsuTools)
- Sync .github\workflows\dotnet.yml (KtsuTools)
- chore: remove Microsoft.SourceLink.GitHub package version (matt-edmondson)
- build: add console tools and JSON converter packages (matt-edmondson)
- Add TAGS.md with NuGet package tags (matt-edmondson)
- docs: add Stats verb to README (matt-edmondson)
- docs: replace placeholder README with usage and command reference (matt-edmondson)
- Sync .github\workflows\dotnet.yml (KtsuTools)

## v1.1.105 (patch)

Changes since v1.1.104:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.104 (patch)

Changes since v1.1.103:

- Bump the ktsu group with 14 updates (dependabot[bot])

## v1.1.103 (patch)

Changes since v1.1.102:

- Bump the ktsu group with 12 updates (dependabot[bot])

## v1.1.102 (patch)

Changes since v1.1.101:

- Bump the ktsu group with 13 updates (dependabot[bot])

## v1.1.101 (patch)

Changes since v1.1.100:

- fix: add AUTHORS.md so every identity keeps the ktsu. prefix [patch] (matt-edmondson)

## v1.1.100 (patch)

Changes since v1.1.99:

- Bump the ktsu group with 11 updates (dependabot[bot])

## v1.1.99 (patch)

Changes since v1.1.98:

- Bump MSTest.Sdk from 4.4.0 to 4.4.1 (dependabot[bot])
- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.98 (patch)

Changes since v1.1.97:

- Bump the ktsu group with 14 updates (dependabot[bot])

## v1.1.97 (patch)

Changes since v1.1.96:

- Gate Dependabot auto-merge on CI actually being green (Claude)

## v1.1.96 (patch)

Changes since v1.1.95:

- Bump the ktsu group with 5 updates (dependabot[bot])

## v1.1.95 (patch)

Changes since v1.1.94:

- ci: adopt the consolidated .NET workflow [patch] (Claude)

## v1.1.94 (patch)

Changes since v1.1.93:

- Bump the ktsu group with 5 updates (dependabot[bot])

## v1.1.93 (patch)

Changes since v1.1.92:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.92 (patch)

Changes since v1.1.91:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.91 (patch)

Changes since v1.1.90:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.90 (patch)

Changes since v1.1.89:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.89 (patch)

Changes since v1.1.88:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.88 (patch)

Changes since v1.1.87:

- Bump MSTest.Sdk from 4.3.3 to 4.4.0 (dependabot[bot])
- Bump the ktsu group with 5 updates (dependabot[bot])

## v1.1.87 (patch)

Changes since v1.1.86:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.86 (patch)

Changes since v1.1.85:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.85 (patch)

No significant changes detected since v1.1.84.

## v1.1.84 (patch)

Changes since v1.1.83:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.83 (patch)

Changes since v1.1.82:

- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.1.82 (patch)

Changes since v1.1.81:

- ci: make the SonarQube quality gate opt in [patch] (matt-edmondson)
- ci: adopt the unified dotnet workflow [patch] (matt-edmondson)

## v1.1.81 (patch)

Changes since v1.1.80:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.80 (patch)

No significant changes detected since v1.1.79.

## v1.1.79 (patch)

Changes since v1.1.78:

- Bump the ktsu group with 12 updates (dependabot[bot])

## v1.1.78 (patch)

Changes since v1.1.77:

- Bump the ktsu group with 14 updates (dependabot[bot])

## v1.1.77 (patch)

Changes since v1.1.76:

- chore: store icon.png in LFS as .gitattributes declares (matt-edmondson)
- docs: scope build badge to the default branch (matt-edmondson)
- docs: correct README, DESCRIPTION and TAGS metadata (matt-edmondson)

## v1.1.76 (patch)

Changes since v1.1.75:

- Stop Update SDKs failing when there is nothing to update (matt-edmondson)

## v1.1.75 (patch)

Changes since v1.1.74:

- Fix ktsu.Sdk 2.26+ analyzer errors: new one-line copyright header (IDE0073), normalize line endings to LF (IDE0055), PrivateAssets="all" on Polyfill (KTSU0007) [patch] (matt-edmondson)
- Sync .github\workflows\dotnet.yml (KtsuTools)
- Sync .runsettings (KtsuTools)
- Sync .editorconfig (KtsuTools)
- Sync .gitattributes (KtsuTools)
- Sync global.json (KtsuTools)
- Sync .github\workflows\dotnet.yml (KtsuTools)

## v1.1.74 (patch)

Changes since v1.1.73:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.73 (patch)

Changes since v1.1.72:

- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync global.json (ktsu[bot])

## v1.1.72 (patch)

Changes since v1.1.71:

- Sync .github\workflows\update-sdks.yml (ktsu[bot])
- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync .github\workflows\dependabot-merge.yml (ktsu[bot])
- Sync .gitattributes (ktsu[bot])
- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.71 (patch)

Changes since v1.1.70:

- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.70 (patch)

Changes since v1.1.69:

- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.1.69 (patch)

Changes since v1.1.68:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.68 (patch)

Changes since v1.1.67:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.67 (patch)

Changes since v1.1.66:

- Bump MSTest.Sdk from 4.3.2 to 4.3.3 (dependabot[bot])
- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.66 (patch)

Changes since v1.1.65:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.65 (patch)

Changes since v1.1.64:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.64 (patch)

Changes since v1.1.63:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.63 (patch)

Changes since v1.1.62:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.62 (patch)

Changes since v1.1.61:

- Bump the ktsu group with 12 updates (dependabot[bot])

## v1.1.61 (patch)

Changes since v1.1.60:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.60 (patch)

Changes since v1.1.59:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.59 (patch)

Changes since v1.1.58:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.58 (patch)

Changes since v1.1.57:

- Bump the ktsu group with 5 updates (dependabot[bot])

## v1.1.57 (patch)

Changes since v1.1.56:

- Bump the ktsu group with 5 updates (dependabot[bot])

## v1.1.56 (patch)

Changes since v1.1.55:

- Bump MSTest.Sdk from 4.3.0 to 4.3.2 (dependabot[bot])
- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.55 (patch)

Changes since v1.1.54:

- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.54 (patch)

Changes since v1.1.53:

- Bump MSTest.Sdk from 4.2.3 to 4.3.0 (dependabot[bot])
- Bump the ktsu group with 5 updates (dependabot[bot])

## v1.1.53 (patch)

Changes since v1.1.52:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.52 (patch)

Changes since v1.1.51:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.51 (patch)

Changes since v1.1.50:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.50 (patch)

Changes since v1.1.49:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v1.1.49 (patch)

Changes since v1.1.48:

- Bump the ktsu group with 13 updates (dependabot[bot])

## v1.1.48 (patch)

Changes since v1.1.47:

- Bump the ktsu group with 6 updates (dependabot[bot])

## v1.1.47 (patch)

Changes since v1.1.46:

- Bump the ktsu group with 14 updates (dependabot[bot])

## v1.1.46 (patch)

Changes since v1.1.45:

- Bump the ktsu group with 5 updates (dependabot[bot])

## v1.1.45 (patch)

Changes since v1.1.44:

- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync global.json (ktsu[bot])

## v1.1.44 (patch)

Changes since v1.1.43:

- chore: remove Microsoft.SourceLink.GitHub package version (matt-edmondson)
- build: add console tools and JSON converter packages (matt-edmondson)

## v1.1.43 (patch)

Changes since v1.1.42:

- Sync .github\workflows\update-sdks.yml (ktsu[bot])
- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync .github\dependabot.yml (ktsu[bot])
- Sync .gitignore (ktsu[bot])
- Sync .gitattributes (ktsu[bot])
- Sync global.json (ktsu[bot])

## v1.1.42 (patch)

Changes since v1.1.41:

- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.41 (patch)

Changes since v1.1.40:

- Bump Polyfill from 10.8.0 to 10.8.1 (dependabot[bot])
- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.40 (patch)

Changes since v1.1.39:

- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.1.39 (patch)

Changes since v1.1.38:

- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.1.38 (patch)

Changes since v1.1.37:

- Bump Polyfill from 10.7.0 to 10.7.1 (dependabot[bot])

## v1.1.37 (patch)

Changes since v1.1.36:

- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.36 (patch)

Changes since v1.1.35:

- Bump Polyfill from 10.6.0 to 10.7.0 (dependabot[bot])

## v1.1.35 (patch)

Changes since v1.1.34:

- Bump Polyfill from 10.5.1 to 10.6.0 (dependabot[bot])
- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.1.34 (patch)

Changes since v1.1.33:

- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.33 (patch)

Changes since v1.1.32:

- Bump MSTest.Sdk from 4.2.2 to 4.2.3 (dependabot[bot])

## v1.1.32 (patch)

Changes since v1.1.31:

- Bump the microsoft group with 1 update (dependabot[bot])

## v1.1.31 (patch)

Changes since v1.1.30:

- Bump Polyfill from 10.5.0 to 10.5.1 (dependabot[bot])
- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.1.30 (patch)

Changes since v1.1.29:

- Add TAGS.md with NuGet package tags (matt-edmondson)

## v1.1.29 (patch)

Changes since v1.1.28:

- Bump Polyfill from 10.4.0 to 10.5.0 (dependabot[bot])

## v1.1.28 (patch)

Changes since v1.1.27:

- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.27 (patch)

Changes since v1.1.26:

- Bump Polyfill from 10.3.0 to 10.4.0 (dependabot[bot])

## v1.1.26 (patch)

Changes since v1.1.25:

- docs: add Stats verb to README (matt-edmondson)
- docs: replace placeholder README with usage and command reference (matt-edmondson)

## v1.1.25 (patch)

Changes since v1.1.24:

- Bump MSTest.Sdk from 4.2.1 to 4.2.2 (dependabot[bot])
- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.24 (patch)

Changes since v1.1.23:

- Bump the microsoft group with 1 update (dependabot[bot])

## v1.1.23 (patch)

Changes since v1.1.22:

- Bump the microsoft group with 1 update (dependabot[bot])

## v1.1.22 (patch)

Changes since v1.1.21:

- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.21 (patch)

Changes since v1.1.20:

- Bump Polyfill from 10.1.1 to 10.3.0 (dependabot[bot])
- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.20 (patch)

Changes since v1.1.19:

- Bump Polyfill from 10.0.0 to 10.1.1 (dependabot[bot])
- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.1.19 (patch)

Changes since v1.1.18:

- Bump MSTest.Sdk from 4.1.0 to 4.2.1 (dependabot[bot])
- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.18 (patch)

Changes since v1.1.17:

- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.17 (patch)

Changes since v1.1.16:

- Bump Polyfill from 9.24.0 to 10.0.0 (dependabot[bot])
- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.16 (patch)

Changes since v1.1.15:

- Bump Polyfill from 9.23.0 to 9.24.0 (dependabot[bot])

## v1.1.15 (patch)

Changes since v1.1.14:

- Bump Polyfill from 9.22.0 to 9.23.0 (dependabot[bot])
- Bump Polyfill from 9.18.0 to 9.22.0 (dependabot[bot])
- Bump the microsoft group with 1 update (dependabot[bot])
- Bump the microsoft group with 1 update (dependabot[bot])

## v1.1.14 (patch)

Changes since v1.1.13:

- Bump Polyfill from 9.17.0 to 9.18.0 (dependabot[bot])

## v1.1.13 (patch)

Changes since v1.1.12:

- Bump Polyfill from 9.14.0 to 9.17.0 (dependabot[bot])

## v1.1.12 (patch)

Changes since v1.1.11:

- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.1.11 (patch)

Changes since v1.1.10:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.10 (patch)

Changes since v1.1.9:

- Bump Polyfill from 9.12.0 to 9.13.0 (dependabot[bot])
- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.1.9 (patch)

Changes since v1.1.8:

- Bump Polyfill from 9.11.0 to 9.12.0 (dependabot[bot])
- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.1.8 (patch)

Changes since v1.1.7:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.7 (patch)

Changes since v1.1.6:

- Bump Polyfill from 9.10.0 to 9.11.0 (dependabot[bot])
- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.6 (patch)

Changes since v1.1.5:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v1.1.5 (patch)

Changes since v1.1.4:

- Bump Polyfill from 9.9.0 to 9.10.0 (dependabot[bot])

## v1.1.4 (patch)

Changes since v1.1.3:

- Bump the ktsu group with 1 update (dependabot[bot])

## v1.1.3 (patch)

Changes since v1.1.2:

- Bump the ktsu group with 1 update (dependabot[bot])
- Bump the microsoft group with 1 update (dependabot[bot])

## v1.1.2 (patch)

Changes since v1.1.1:

- Sync .github\workflows\dotnet.yml (KtsuTools)

## v1.1.2-pre.1 (prerelease)

Changes since v1.1.1:

- Sync .github\workflows\dotnet.yml (KtsuTools)

## v1.1.1 (patch)

Changes since v1.1.0:

- Merge remote-tracking branch 'refs/remotes/origin/main' (ktsu[bot])
- Sync .github\workflows\dotnet.yml (ktsu[bot])

## v1.1.0 (minor)

Changes since v1.0.0:

- Add unit tests and implement image processing features (matt-edmondson)
- Remove legacy build scripts (matt-edmondson)
- Add RoundTripStringJsonConverter to Import and Export verbs for improved JSON handling (matt-edmondson)
- Add Import verb for importing image descriptions from JSON and CSV files (matt-edmondson)
- Increase HttpClient timeout to 10 minutes for improved request handling (matt-edmondson)
- Enhance image description prompt by including known file paths for context (matt-edmondson)
- Add MaxConcurrentRequests property and update Scan verb for configurable concurrency (matt-edmondson)
- Update DescriptionPrompt for clarity and specificity; enhance image description guidelines (matt-edmondson)
- Reset PathString to default after scan completion; improve state management (matt-edmondson)

## v1.0.8-pre.1 (prerelease)

Changes since v1.0.7:

- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync .github\workflows\dotnet.yml (ktsu[bot])

## v1.0.7 (patch)

Changes since v1.0.6:

- Remove legacy build scripts (matt-edmondson)

## v1.0.7-pre.1 (prerelease)

Changes since v1.0.6:

- Sync .github\workflows\dotnet.yml (ktsu[bot])

## v1.0.6 (patch)

Changes since v1.0.5:

- Add RoundTripStringJsonConverter to Import and Export verbs for improved JSON handling (matt-edmondson)

## v1.0.5 (patch)

Changes since v1.0.4:

- Add Import verb for importing image descriptions from JSON and CSV files (matt-edmondson)

## v1.0.4 (patch)

Changes since v1.0.3:

- Increase HttpClient timeout to 10 minutes for improved request handling (matt-edmondson)

## v1.0.3 (patch)

Changes since v1.0.2:

- Enhance image description prompt by including known file paths for context (matt-edmondson)

## v1.0.2 (patch)

Changes since v1.0.1:

- Add MaxConcurrentRequests property and update Scan verb for configurable concurrency (matt-edmondson)
- Update DescriptionPrompt for clarity and specificity; enhance image description guidelines (matt-edmondson)

## v1.0.1 (patch)

Changes since v1.0.0:

- Reset PathString to default after scan completion; improve state management (matt-edmondson)

## v1.0.0 (major)

- Refactor Scan verb to deduplicate image paths and enhance description handling; improve output for discovered images (matt-edmondson)
- Refactor image file scanning logic to improve extension checking; streamline file path handling (matt-edmondson)
- Implement user input for path validation in Scan verb; enhance error handling for empty paths (matt-edmondson)
- Enhance ImageDescription to track multiple known paths; update export and search functionalities to reflect changes (matt-edmondson)
- Add suggested filename handling and prompts for image descriptions; update related classes and export functionality (matt-edmondson)
- Refactor code to use specific types for file paths and model settings; improve type safety and clarity across multiple classes. (matt-edmondson)
- Refactor ImageDescription to use specific types for FilePath and FileName; add OllamaEndpoint and OllamaModelName records (matt-edmondson)
- Refactor file handling to use AbsoluteFilePath for improved type safety and clarity (matt-edmondson)
- Refactor menu item creation to include help text in the display (matt-edmondson)
- Add Configure verb for Ollama endpoint and model settings (matt-edmondson)
- Initial Commit (matt-edmondson)
- Initial Commit (matt-edmondson)

