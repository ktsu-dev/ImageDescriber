## v1.2.18 (patch)

Changes since v1.2.17:

- Trim a suggested name's wrapping after its extension is cut too [patch] (#272) (matt-edmondson)
- Scan images in dot-folders and hidden files again [patch] (#276) (matt-edmondson)
- Match a Search keyword in folder names and the suggested file name [patch] (#280) (matt-edmondson)
- Strip a prose lead-in on the name's line from a suggested file name [patch] (#281) (matt-edmondson)
- Fail an import whose every entry is rejected, and read camelCase JSON [patch] (#283) (matt-edmondson)
- Don't follow directory symlinks when scanning for images [patch] (#284) (matt-edmondson)
- Stop a scan whose model isn't pulled, and show Ollama's error text [patch] (#285) (matt-edmondson)
- Exit 1 when another run holds the store lock [patch] (#286) (matt-edmondson)
- Skip a CSV row whose quote never closes or is followed by stray text [patch] (#288) (matt-edmondson)

