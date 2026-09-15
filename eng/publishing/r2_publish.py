"""Publish only the reviewed catalog and immutable release files. Never delete objects."""
from __future__ import annotations

import argparse
import hashlib
import html
import json
import os
from pathlib import Path
import re
import sys
from urllib.request import HTTPRedirectHandler, Request, build_opener

BUCKET = "briosa-downloads"
PUBLIC = "https://briosa.dev/downloads/"
ORIGIN = "https://objects.briosa.dev/"
FRESH = "no-store, no-cache, max-age=0, must-revalidate"
IMMUTABLE = "public, max-age=31536000, immutable"
CATALOG = "catalog.json"
SIGNATURE = "catalog.json.signature.json"
MAX_METADATA = 1024 * 1024
ROOT = Path(__file__).resolve().parents[2]


def digest(data):
    return hashlib.sha256(data).hexdigest()


def file_digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON field.")
        result[key] = value
    return result


def parse_catalog(data):
    if len(data) > MAX_METADATA:
        raise ValueError("Catalog exceeds metadata limit.")
    catalog = json.loads(data, object_pairs_hook=unique_object)
    if catalog.get("schemaVersion") != 1 or not isinstance(catalog.get("packages"), list):
        raise ValueError("Unsupported catalog.")
    ids, paths = set(), {}
    for package in catalog["packages"]:
        identity = package["id"].casefold()
        if identity in ids:
            raise ValueError("Duplicate package identity.")
        ids.add(identity)
        version = package["version"]
        if not re.fullmatch(r"(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)", version):
            raise ValueError("Public publishing currently accepts stable release versions only.")
        if package["runtimeIdentifier"] != "win-x64":
            raise ValueError("Unsupported publishing runtime.")
        if package["component"] == "server":
            target = package["spatialAnalyzerTarget"]
            if not re.fullmatch(r"[0-9]{4}(?:\.[0-9]{1,8}){3}", target):
                raise ValueError("Invalid exact SA target.")
            name = f"briosa-{version}-sa-{target}-win-x64"
            prefix = f"packages/server/{target}/{version}/"
        elif package["component"] == "installer":
            name = f"briosa-installer-{version}-win-x64"
            prefix = f"packages/installer/{version}/"
        else:
            raise ValueError("Unsupported component.")
        if package["id"] != name:
            raise ValueError("Package identity differs from its coordinates.")
        for field, suffix in (("artifact", ".zip"), ("provenance", ".provenance.json")):
            reference = package[field]
            if reference["path"] != prefix + name + suffix:
                raise ValueError("Unexpected or unsafe release object path.")
            if not isinstance(reference["size"], int) or not 0 < reference["size"] <= 1024**3:
                raise ValueError("Invalid publication file size (maximum 1 GiB).")
            if not re.fullmatch(r"[0-9a-f]{64}", reference["sha256"]):
                raise ValueError("Invalid release digest.")
            if reference["path"] in paths and paths[reference["path"]] != reference:
                raise ValueError("Conflicting release object references.")
            paths[reference["path"]] = reference
    return catalog


def assert_retained(previous, current):
    retained = {p["id"].casefold(): p for p in current["packages"]}
    for package in previous["packages"]:
        if retained.get(package["id"].casefold()) != package:
            raise ValueError("Publication would remove or change a retained package.")


def content_type(key):
    if key.endswith(".zip"):
        return "application/zip"
    if key.endswith(".json"):
        return "application/json"
    if key.endswith(".html"):
        return "text/html; charset=utf-8"
    return "text/plain; charset=utf-8"


def create_client():
    import boto3
    from botocore.config import Config
    account = os.environ["R2_ACCOUNT_ID"]
    if not re.fullmatch(r"[0-9a-f]{32}", account):
        raise ValueError("Invalid R2 account ID.")
    return boto3.client(
        "s3", endpoint_url=f"https://{account}.r2.cloudflarestorage.com", region_name="auto",
        aws_access_key_id=os.environ["R2_ACCESS_KEY_ID"],
        aws_secret_access_key=os.environ["R2_SECRET_ACCESS_KEY"],
        config=Config(signature_version="s3v4", connect_timeout=10, read_timeout=120,
                      retries={"total_max_attempts": 1}, request_checksum_calculation="when_required",
                      response_checksum_validation="when_required", s3={"addressing_style": "path"}),
    )


class Store:
    def __init__(self, client):
        self.client = client

    def read(self, key, limit):
        try:
            response = self.client.get_object(Bucket=BUCKET, Key=key)
        except Exception as error:
            code = getattr(error, "response", {}).get("Error", {}).get("Code")
            if code in ("NoSuchKey", "404", "NotFound"):
                return None
            raise
        body = response["Body"]
        try:
            if response["ContentLength"] > limit:
                raise ValueError("Remote object exceeds its expected size.")
            data = body.read(limit + 1)
            if len(data) > limit or len(data) != response["ContentLength"]:
                raise ValueError("Remote object length differs.")
        finally:
            body.close()
        return {"data": data, "etag": response["ETag"]}

    def put(self, key, data, expected_etag, cache):
        condition = {"IfMatch": expected_etag} if expected_etag else {"IfNoneMatch": "*"}
        return self.client.put_object(
            Bucket=BUCKET, Key=key, Body=data, ContentLength=len(data),
            ContentType=content_type(key), CacheControl=cache,
            Metadata={"sha256": digest(data)}, **condition,
        )

    def immutable(self, key, path):
        data = path.read_bytes()
        current = self.read(key, len(data))
        if current:
            if current["data"] != data:
                raise ValueError(f"Refusing to replace immutable object: {key}")
            print(f"Retained identical object: {key}")
            return
        self.put(key, data, None, IMMUTABLE)
        uploaded = self.read(key, len(data))
        if not uploaded or uploaded["data"] != data:
            raise ValueError(f"Uploaded object verification failed: {key}")
        print(f"Uploaded and verified: {key}")


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise ValueError("Public downloads must not redirect.")


def public_read(base, key, limit, fresh=False):
    request = Request(base + key, headers={"Accept-Encoding": "identity"})
    with build_opener(NoRedirect).open(request, timeout=120) as response:
        if response.status != 200:
            raise ValueError("Public download did not return HTTP 200.")
        if fresh:
            cache = response.headers.get("Cache-Control", "").lower()
            status = response.headers.get("CF-Cache-Status", "").upper()
            if "no-store" not in cache or status in ("HIT", "STALE", "UPDATING", "REVALIDATED"):
                raise ValueError("Mutable public metadata is being cached.")
        data = response.read(limit + 1)
        if len(data) > limit:
            raise ValueError("Public object exceeds expected size.")
        return data


def references(catalog):
    for package in catalog["packages"]:
        yield package["artifact"]
        yield package["provenance"]


def support_files(public, catalog):
    key = ROOT / "eng/signing/catalog-public.pem"
    fingerprint = json.loads((ROOT / "eng/signing/azure.json").read_text())["catalogPublisherSha256"]
    (public / "keys").mkdir(exist_ok=True)
    (public / "keys/catalog-public.pem").write_bytes(key.read_bytes())
    (public / "keys/catalog-publisher-sha256.txt").write_text(
        "SHA-256 of the Briosa catalog public key's SubjectPublicKeyInfo DER bytes:\n"
        + fingerprint + "\n", encoding="utf-8")
    (public / "hosting-check.txt").write_text("Briosa downloads are served from Cloudflare R2.\n", encoding="utf-8")
    rows = []
    for package in catalog["packages"]:
        path = "/downloads/" + package["artifact"]["path"]
        target = package.get("spatialAnalyzerTarget", "Installer")
        rows.append("<tr><td>" + html.escape(target) + "</td><td>" + html.escape(package["version"])
                    + '</td><td><a href="' + html.escape(path, quote=True) + '">Windows x64 ZIP</a>'
                    + ' · <a href="' + html.escape(path + ".sha256", quote=True) + '">SHA-256</a></td></tr>')
    page = """<!doctype html><html lang="en"><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>Briosa downloads</title>
<style>:root{color-scheme:light dark;font-family:system-ui,sans-serif}body{max-width:60rem;margin:4rem auto;padding:0 1.5rem;line-height:1.6}
h1{line-height:1.2}table{border-collapse:collapse;width:100%}th,td{text-align:left;padding:.8rem;border-bottom:1px solid #8886}
a{color:light-dark(#005b9a,#66c5ee)}code{overflow-wrap:anywhere}footer{margin-top:2rem}</style>
<h1>Briosa downloads</h1><p>Versioned Windows packages for Briosa. SpatialAnalyzer is installed and licensed separately.</p>
<p><a href="/">Documentation</a> · <a href="https://github.com/spatialanalyzer/briosa/releases">Release notes and developer artifacts</a></p>
<table><thead><tr><th>Product / SA target</th><th>Briosa version</th><th>Download</th></tr></thead><tbody>"""
    page += "".join(rows) + """</tbody></table><h2>Installer and enterprise sources</h2>
<p>Use <code>https://briosa.dev/downloads/catalog.json</code> as the package source.
An enterprise Generic remote can mirror this downloads directory without changing its file paths or bytes.</p>
<p><a href="/downloads/catalog.json">Catalog</a> · <a href="/downloads/catalog.json.signature.json">Catalog signature</a>
 · <a href="/downloads/keys/catalog-public.pem">Public verification key</a></p>
<p>Catalog publisher fingerprint (SHA-256 of SPKI DER): <code>""" + fingerprint + """</code></p>
<footer>Windows code publisher: David Lucas. Briosa is open-source software, independent of Hexagon.</footer></html>
"""
    (public / "index.html").write_text(page, encoding="utf-8")


def snapshot(store, work):
    prior = work / "prior"
    prior.mkdir()
    state = {}
    for key in (CATALOG, SIGNATURE, "index.html"):
        current = store.read(key, MAX_METADATA)
        state[key] = current["etag"] if current else None
        if current:
            (prior / key).write_bytes(current["data"])
    (work / "state.json").write_text(json.dumps(state), encoding="utf-8")


def guard_prior(work, mode):
    current = parse_catalog((work / "public" / CATALOG).read_bytes())
    if not current["packages"]:
        raise ValueError("Refusing to publish an empty catalog.")
    prior = work / "prior" / CATALOG
    if prior.exists():
        assert_retained(parse_catalog(prior.read_bytes()), current)
        if mode == "renew" and prior.read_bytes() != (work / "public" / CATALOG).read_bytes():
            raise ValueError("Renewal cannot publish a changed catalog; publish the reviewed release first.")
    elif mode == "renew":
        raise ValueError("Nothing has been published to renew.")
    support_files(work / "public", current)


def upload_payloads(store, work):
    public = work / "public"
    catalog = parse_catalog((public / CATALOG).read_bytes())
    files = []
    for reference in references(catalog):
        path = public / reference["path"]
        if path.stat().st_size != reference["size"] or file_digest(path) != reference["sha256"]:
            raise ValueError("Staged package differs from the reviewed catalog.")
        files.append(reference["path"])
    for package in catalog["packages"]:
        ref = package["artifact"]
        checksum = ref["path"] + ".sha256"
        expected = ref["sha256"] + "  " + Path(ref["path"]).name
        if (public / checksum).read_text().strip() != expected:
            raise ValueError("Adjacent checksum differs from the reviewed package.")
        files.append(checksum)
    files += ["keys/catalog-public.pem", "keys/catalog-publisher-sha256.txt", "hosting-check.txt"]
    # Validate every staged input before the first write.
    for key in files:
        if not (public / key).is_file():
            raise ValueError("Staged publication file is missing.")
    for key in files:
        store.immutable(key, public / key)
        expected = (public / key).read_bytes()
        if public_read(PUBLIC, key, len(expected)) != expected:
            raise ValueError(f"Public route changed uploaded bytes: {key}")
    print("Immutable payloads and the public downloads route verified.")


def commit_metadata(store, work):
    public = work / "public"
    state = json.loads((work / "state.json").read_text())
    # Conditional writes also guard against writers outside the workflow concurrency group.
    for key in (CATALOG, SIGNATURE, "index.html"):
        current = store.read(key, MAX_METADATA)
        if (current["etag"] if current else None) != state[key]:
            raise ValueError("Published metadata changed during preparation; start a fresh run.")
    for key in (CATALOG, SIGNATURE, "index.html"):
        data = (public / key).read_bytes()
        store.put(key, data, state[key], FRESH)
        for base in (PUBLIC, ORIGIN):
            if public_read(base, key, len(data), fresh=True) != data:
                raise ValueError("Public metadata differs from the approved bytes.")
        print(f"Published and verified: {key}")


def check_public(work):
    expected = (ROOT / "eng/publishing/catalog.json").read_bytes()
    if public_read(PUBLIC, CATALOG, MAX_METADATA, fresh=True) != expected:
        raise ValueError("Public catalog differs from the reviewed canonical catalog.")
    public = work / "public"
    public.mkdir(parents=True, exist_ok=True)
    (public / CATALOG).write_bytes(expected)
    (public / SIGNATURE).write_bytes(public_read(PUBLIC, SIGNATURE, 16384, fresh=True))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("command", choices=["snapshot", "guard", "upload", "commit", "check"])
    parser.add_argument("--work", type=Path, required=True)
    parser.add_argument("--mode", choices=["publish", "renew"], default="publish")
    args = parser.parse_args()
    if args.command == "check":
        check_public(args.work)
    elif args.command == "guard":
        guard_prior(args.work, args.mode)
    else:
        store = Store(create_client())
        {"snapshot": snapshot, "upload": upload_payloads, "commit": commit_metadata}[args.command](store, args.work)


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        response = getattr(error, "response", None)
        detail = response.get("Error", {}).get("Code", "R2 request failed") if response else str(error)
        print(f"Publication failed: {detail}", file=sys.stderr)
        sys.exit(1)
