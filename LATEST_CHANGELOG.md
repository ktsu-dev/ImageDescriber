## v1.2.13 (patch)

Changes since v1.2.12:

- Fail on an unknown verb or stray word, and match verbs in any case [patch] (#245) (matt-edmondson)
- Report an unsupported -p, -i or -o path instead of crashing [patch] (#241) (matt-edmondson)
- Match a multi-word Search query when every keyword appears, in any order [patch] (#240) (matt-edmondson)
- Store a negative imported file size as 0, and total sizes without overflow in Stats [patch] (#236) (matt-edmondson)
- Skip TIFF images, which Ollama can't decode, and count them once per scan [patch] (#234) (matt-edmondson)
- Strip a suggested name's extension after removing its formatting, so it isn't doubled [patch] (#232) (matt-edmondson)

