## v1.2.15 (patch)

Changes since v1.2.14:

- Fail a scan and name the images it couldn't hash [patch] (#262) (matt-edmondson)
- Skip a sentence lead-in before the suggested name [patch] (#248) (matt-edmondson)
- Match a Search keyword at the start of a word, not inside one [patch] (#249) (matt-edmondson)
- Take the store lock only around Configure's save, not while it prompts [patch] (#250) (matt-edmondson)
- Strip typographic quotes, brackets and a trailing comma from a suggested name [patch] (#255) (matt-edmondson)
- Strip a lead-in label that holds digits or parentheses from a suggested name [patch] (#256) (matt-edmondson)
- Report an import file that can't be read instead of crashing [patch] (#260) (matt-edmondson)
- Strip the quotes from a path pasted at the Scan, Import and Export prompts [patch] (#261) (matt-edmondson)
- Fail an image whose Ollama answer is empty or cut off [patch] (#265) (matt-edmondson)
- Treat known paths that differ only in letter case as one file on Windows [patch] (#266) (matt-edmondson)
- Strip a .tif or .tiff the model puts on a suggested name [patch] (#270) (matt-edmondson)
- Write the JSON export without a byte order mark [patch] (#271) (matt-edmondson)

