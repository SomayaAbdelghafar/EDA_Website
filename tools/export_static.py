#!/usr/bin/env python3
"""Save the running EDA website as static files that GitHub Pages can host.

GitHub Pages only serves static files and cannot run ASP.NET. The workflow in
.github/workflows/deploy-pages.yml starts the site, then this script crawls it
from the home page and:

  * saves every page as <route>/index.html  (/about -> about/index.html,
    /projects?category=x -> projects/category-x/index.html, 404 page -> 404.html)
  * saves every image, stylesheet and script those pages use
  * rewrites links so they work under the Pages base path (e.g. /EDA_Website)

Standard library only. Exits with code 1 if any linked page or file is missing.

    python tools/export_static.py --source http://127.0.0.1:5080 --out _site --base-path /EDA_Website
"""
from __future__ import annotations

import argparse
import hashlib
import html
import re
import shutil
import sys
import urllib.error
import urllib.request
from pathlib import Path
from urllib.parse import parse_qsl, quote, unquote, urljoin, urlsplit

# Pages nothing links to that must still exist. GitHub Pages serves /404.html for unknown URLs.
EXTRA_PAGES = {"/status/404": "404.html"}

ATTR_RE = re.compile(r"""\b(?P<name>href|src|action|poster|content)\s*=\s*(?P<q>["'])(?P<val>.*?)(?P=q)""", re.I | re.S)
CSS_URL_RE = re.compile(r"""url\(\s*(?:"(?P<d>[^"]*)"|'(?P<s>[^']*)'|(?P<u>[^)'"\s]*))\s*\)""", re.I)
CSS_IMPORT_RE = re.compile(r"""@import\s+(?:"(?P<d>[^"]*)"|'(?P<s>[^']*)')""", re.I)
NOT_LOCAL_RE = re.compile(r"^(?:[a-z][a-z0-9+.-]*:|//|#)", re.I)  # https:, mailto:, data:, //cdn, #anchor
SAFE_SLUG_RE = re.compile(r"[A-Za-z0-9_-]+")


def route_key(path: str, query: str = "") -> str:
    """One key per page: trailing slashes ignored, query string kept (it selects different content)."""
    path = path.rstrip("/") or "/"
    return f"{path}?{query}" if query else path


def splice(m: re.Match, group: str, new: str) -> str:
    """Return the regex match text with one group's text replaced."""
    start, end = m.span(group)
    return m.group(0)[: start - m.start()] + new + m.group(0)[end - m.start():]


def css_group(m: re.Match) -> str:
    return next(g for g in ("d", "s", "u") if g in m.re.groupindex and m.group(g) is not None)


class Exporter:
    def __init__(self, source: str, out: Path, base_path: str):
        self.source = source.rstrip("/")
        self.out = out
        self.base = "/" + base_path.strip("/") if base_path.strip("/") else ""
        self.pages: dict[str, str] = {}                     # route key -> html
        self.assets: dict[str, tuple[bytes, str]] = {}      # path -> (body, content type)
        self.page_files: dict[str, str] = {}                # route key -> output file
        self.broken: list[str] = []

    # ---------- crawling ----------
    def fetch(self, url: str) -> tuple[int, str, bytes]:
        req = urllib.request.Request(self.source + quote(url, safe="/:?#[]@!$&'()*+,;=%~"),
                                     headers={"User-Agent": "eda-static-export"})
        try:
            with urllib.request.urlopen(req, timeout=60) as r:
                return r.status, r.headers.get_content_type(), r.read()
        except urllib.error.HTTPError as e:
            return e.code, e.headers.get_content_type(), e.read()

    def crawl(self) -> None:
        queue: list[tuple[str, str]] = [("/", "(start)")] + [(p, "(extra)") for p in EXTRA_PAGES]
        seen: set[str] = set()
        while queue:
            url, found_on = queue.pop(0)
            parts = urlsplit(url)
            key = route_key(parts.path, parts.query)
            # ?v=... cache busters on files don't need separate copies
            if key in seen or parts.path in self.assets:
                continue
            seen.add(key)

            status, ctype, body = self.fetch(key)
            if status >= 400 and key not in EXTRA_PAGES:
                self.broken.append(f"{key}  (HTTP {status}, linked from {found_on})")
                continue

            found: list[str] = []
            collect = lambda u: found.append(u) or u  # noqa: E731
            if ctype == "text/html":
                text = body.decode("utf-8")
                self.pages[key] = text
                self.map_html(text, key, collect)
            else:
                self.assets[parts.path] = (body, ctype)
                if ctype == "text/css":
                    self.map_css(body.decode("utf-8"), parts.path, collect)
            queue += [(u, key) for u in found]

        for key in self.pages:
            self.page_files[key] = self.page_file(key)
        clashes = {f for f in self.page_files.values() if list(self.page_files.values()).count(f) > 1}
        if clashes:
            sys.exit(f"Two pages map to the same file: {sorted(clashes)}")

    @staticmethod
    def page_file(key: str) -> str:
        if key in EXTRA_PAGES:
            return EXTRA_PAGES[key]
        parts = urlsplit(key)
        segments = [unquote(s) for s in parts.path.split("/") if s]
        for name, value in parse_qsl(parts.query):
            # Arabic or other unsafe values get a short stable hash so folder names stay plain ASCII
            slug = value if SAFE_SLUG_RE.fullmatch(value) else hashlib.sha1(value.encode()).hexdigest()[:8]
            segments.append(f"{name}-{slug}")
        return "/".join(segments + ["index.html"])

    # ---------- link mapping (shared by crawling and rewriting) ----------
    @staticmethod
    def resolve(value: str, context: str) -> str | None:
        value = value.strip()
        if not value or NOT_LOCAL_RE.match(value):
            return None
        return urljoin(context, value)

    def map_html(self, text: str, context: str, fn) -> str:
        def attr(m: re.Match) -> str:
            value = html.unescape(m.group("val"))
            # <meta content="..."> holds plain text too; only treat root paths as links
            if m.group("name").lower() == "content" and not (value.startswith("/") and not value.startswith("//")):
                return m.group(0)
            url = self.resolve(value, context)
            return m.group(0) if url is None else splice(m, "val", html.escape(fn(url), quote=True))

        def css_url(m: re.Match) -> str:
            group = css_group(m)
            url = self.resolve(html.unescape(m.group(group)), context)
            return m.group(0) if url is None else splice(m, group, html.escape(fn(url), quote=True))

        return CSS_URL_RE.sub(css_url, ATTR_RE.sub(attr, text))

    def map_css(self, text: str, context: str, fn) -> str:
        def sub(m: re.Match) -> str:
            group = css_group(m)
            url = self.resolve(m.group(group), context)
            return m.group(0) if url is None else splice(m, group, fn(url))

        return CSS_IMPORT_RE.sub(sub, CSS_URL_RE.sub(sub, text))

    def public_url(self, url: str) -> str:
        parts = urlsplit(url)
        fragment = f"#{parts.fragment}" if parts.fragment else ""
        key = route_key(parts.path, parts.query)
        if key in self.page_files:
            file = self.page_files[key]
            route = file[: -len("index.html")] if file.endswith("index.html") else file
            return f"{self.base}/{quote(route)}{fragment}"
        query = f"?{parts.query}" if parts.query else ""
        return f"{self.base}{parts.path}{query}{fragment}"

    # ---------- output ----------
    def write(self) -> None:
        if self.out.exists():
            if (self.out / ".git").exists() or self.out.resolve() == Path.cwd().resolve():
                sys.exit(f"Refusing to clear {self.out}: pick an empty output folder such as _site")
            shutil.rmtree(self.out)
        self.out.mkdir(parents=True)

        for path, (body, ctype) in self.assets.items():
            dest = self.out / unquote(path).lstrip("/")
            dest.parent.mkdir(parents=True, exist_ok=True)
            if ctype == "text/css":
                body = self.map_css(body.decode("utf-8"), path, self.public_url).encode("utf-8")
            dest.write_bytes(body)

        for key, text in self.pages.items():
            dest = self.out / self.page_files[key]
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_text(self.map_html(text, key, self.public_url), encoding="utf-8")

        (self.out / ".nojekyll").write_text("")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--source", default="http://127.0.0.1:5080", help="URL of the running site")
    parser.add_argument("--out", default="_site", type=Path, help="output folder (cleared first)")
    parser.add_argument("--base-path", default="", help="path the site is served under, e.g. /EDA_Website")
    args = parser.parse_args()

    exporter = Exporter(args.source, args.out, args.base_path)
    exporter.crawl()
    exporter.write()

    size = sum(f.stat().st_size for f in args.out.rglob("*") if f.is_file())
    print(f"Exported {len(exporter.pages)} pages and {len(exporter.assets)} files "
          f"({size / 1_048_576:.1f} MB) to {args.out} with base path '{exporter.base or '/'}'")
    for page in sorted(exporter.page_files.values()):
        print(f"  {page}")
    if exporter.broken:
        print("\nBroken links:", *exporter.broken, sep="\n  ", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
