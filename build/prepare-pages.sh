#!/usr/bin/env bash
# Adjusts a published Blazor PWA (the wwwroot folder) for GitHub Pages hosting at /<repo-name>/.
# Usage: build/prepare-pages.sh <publish/wwwroot> <repo-name>
set -euo pipefail

site="$1"
repo="$2"

# 1. Serve from /<repo-name>/ rather than the domain root.
sed -i "s|<base href=\"/\" />|<base href=\"/${repo}/\" />|" "$site/index.html"
grep -q "<base href=\"/${repo}/\" />" "$site/index.html"

# 2. The service worker fetches index.html with its build-time integrity hash. After the edit
#    above that hash is stale, the worker install fails and offline support silently breaks.
hash="sha256-$(openssl dgst -sha256 -binary "$site/index.html" | base64)"
python3 - "$site/service-worker-assets.js" "$hash" <<'PY'
import re, sys
path, new_hash = sys.argv[1], sys.argv[2]
text = open(path).read()
text, count = re.subn(r'("hash":\s*")[^"]+("\s*,\s*"url":\s*"index\.html")', rf'\g<1>{new_hash}\g<2>', text)
if count != 1:
    sys.exit(f"Expected one index.html entry in {path}, found {count}")
open(path, "w").write(text)
PY

# 3. Deep links: Pages serves 404.html for unknown paths, which boots the SPA router.
cp "$site/index.html" "$site/404.html"

# 4. Stop Jekyll ignoring the _framework folder.
touch "$site/.nojekyll"
