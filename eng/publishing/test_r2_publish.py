import copy
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import r2_publish as r2


def catalog(version="0.4.0"):
    name = f"briosa-{version}-sa-2026.1.0529.7-win-x64"
    prefix = f"packages/server/2026.1.0529.7/{version}/"
    return {"schemaVersion": 1, "packages": [{
        "id": name, "component": "server", "version": version,
        "runtimeIdentifier": "win-x64", "spatialAnalyzerTarget": "2026.1.0529.7",
        "artifact": {"path": prefix + name + ".zip", "size": 3, "sha256": r2.digest(b"zip")},
        "provenance": {"path": prefix + name + ".provenance.json", "size": 2, "sha256": r2.digest(b"{}")},
    }]}


def encoded(value):
    return (json.dumps(value, indent=2) + "\n").encode()


class Missing(Exception):
    response = {"Error": {"Code": "NoSuchKey"}}


class FakeClient:
    def __init__(self):
        self.objects = {}
        self.writes = []

    def get_object(self, Bucket, Key):
        assert Bucket == r2.BUCKET
        if Key not in self.objects:
            raise Missing()
        data = self.objects[Key]
        return {"Body": io.BytesIO(data), "ContentLength": len(data), "ETag": r2.digest(data)}

    def put_object(self, **args):
        assert args["Bucket"] == r2.BUCKET
        current = self.objects.get(args["Key"])
        if args.get("IfNoneMatch") == "*" and current is not None:
            raise ValueError("Precondition failed.")
        if "IfMatch" in args and (current is None or r2.digest(current) != args["IfMatch"]):
            raise ValueError("Precondition failed.")
        self.objects[args["Key"]] = args["Body"]
        self.writes.append(args)
        return {"ETag": r2.digest(args["Body"])}


class PublishingTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.public = self.root / "public"
        self.public.mkdir()
        self.client = FakeClient()
        self.store = r2.Store(self.client)
        self.data = catalog()
        (self.public / r2.CATALOG).write_bytes(encoded(self.data))

    def tearDown(self):
        self.temp.cleanup()

    def test_catalog_validates_exact_product_paths(self):
        self.assertEqual(r2.parse_catalog(encoded(self.data)), self.data)
        for value in ("../escape.zip", "/absolute.zip", "packages/other.zip", "https://evil.invalid/x"):
            with self.subTest(value=value):
                changed = copy.deepcopy(self.data)
                changed["packages"][0]["artifact"]["path"] = value
                with self.assertRaises(ValueError):
                    r2.parse_catalog(encoded(changed))

    def test_duplicate_json_and_package_ids_are_rejected(self):
        with self.assertRaises(ValueError):
            r2.parse_catalog(b'{"schemaVersion":1,"schemaVersion":1,"packages":[]}')
        self.data["packages"] *= 2
        with self.assertRaises(ValueError):
            r2.parse_catalog(encoded(self.data))

    def test_retain_old_versions_without_changing_their_digests(self):
        combined = catalog()
        combined["packages"] += catalog("0.5.0")["packages"]
        r2.assert_retained(catalog(), combined)
        with self.assertRaises(ValueError):
            r2.assert_retained(catalog(), catalog("0.5.0"))
        changed = copy.deepcopy(combined)
        changed["packages"][0]["artifact"]["sha256"] = "0" * 64
        with self.assertRaises(ValueError):
            r2.assert_retained(catalog(), changed)

    def test_immutable_upload_is_idempotent_and_refuses_replacement(self):
        path = self.root / "payload.zip"
        path.write_bytes(b"zip")
        self.store.immutable("packages/payload.zip", path)
        self.store.immutable("packages/payload.zip", path)
        self.assertEqual(len(self.client.writes), 1)
        self.assertEqual(self.client.writes[0]["IfNoneMatch"], "*")
        self.assertEqual(self.client.writes[0]["CacheControl"], r2.IMMUTABLE)
        path.write_bytes(b"bad")
        with self.assertRaises(ValueError):
            self.store.immutable("packages/payload.zip", path)
        self.assertEqual(self.client.objects["packages/payload.zip"], b"zip")

    def test_access_denied_is_not_an_empty_bucket(self):
        class Denied(Exception):
            response = {"Error": {"Code": "AccessDenied"}}
        with patch.object(self.client, "get_object", side_effect=Denied()):
            with self.assertRaises(Denied):
                self.store.read(r2.CATALOG, 100)

    def test_oversized_remote_objects_fail_closed(self):
        self.client.objects[r2.CATALOG] = b"12345"
        with self.assertRaises(ValueError):
            self.store.read(r2.CATALOG, 4)

    def test_renewal_requires_unchanged_published_catalog(self):
        r2.snapshot(self.store, self.root)
        with self.assertRaises(ValueError):
            r2.guard_prior(self.root, "renew")
        (self.root / "prior" / r2.CATALOG).write_bytes(encoded(catalog()))
        r2.guard_prior(self.root, "renew")
        newer = catalog()
        newer["packages"] += catalog("0.5.0")["packages"]
        (self.public / r2.CATALOG).write_bytes(encoded(newer))
        with self.assertRaises(ValueError):
            r2.guard_prior(self.root, "renew")

    def test_metadata_drift_is_detected_before_writing(self):
        r2.snapshot(self.store, self.root)
        self.client.objects[r2.CATALOG] = b"another writer"
        with self.assertRaises(ValueError):
            r2.commit_metadata(self.store, self.root)
        self.assertEqual(self.client.writes, [])

    def test_matching_catalog_can_complete_interrupted_publication(self):
        # The reviewed source is authoritative; a failure between the two metadata
        # writes can be retried from a fresh snapshot without changing package bytes.
        self.client.objects[r2.CATALOG] = (self.public / r2.CATALOG).read_bytes()
        r2.snapshot(self.store, self.root)
        r2.guard_prior(self.root, "publish")
        (self.public / r2.SIGNATURE).write_bytes(b"signature verified by PowerShell")
        with patch.object(r2, "public_read", side_effect=lambda base, key, limit, fresh=False: self.client.objects[key]):
            r2.commit_metadata(self.store, self.root)
        self.assertEqual([x["Key"] for x in self.client.writes], [r2.CATALOG, r2.SIGNATURE, "index.html"])
        self.assertTrue(all(x["CacheControl"] == r2.FRESH for x in self.client.writes))
        self.assertEqual(self.client.objects[r2.SIGNATURE], (self.public / r2.SIGNATURE).read_bytes())

    def test_all_payloads_validate_before_any_upload(self):
        r2.snapshot(self.store, self.root)
        r2.guard_prior(self.root, "publish")
        for reference in r2.references(self.data):
            path = self.public / reference["path"]
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b"bad")
        with self.assertRaises(ValueError):
            r2.upload_payloads(self.store, self.root)
        self.assertEqual(self.client.writes, [])

    def test_public_redirects_are_rejected(self):
        with self.assertRaises(ValueError):
            r2.NoRedirect().redirect_request(None, None, 302, "", {}, "https://elsewhere.invalid/")

    def test_existing_metadata_uses_conditional_writes(self):
        self.client.objects[r2.CATALOG] = b"old"
        self.store.put(r2.CATALOG, b"new", r2.digest(b"old"), r2.FRESH)
        with self.assertRaises(ValueError):
            self.store.put(r2.CATALOG, b"stale", r2.digest(b"old"), r2.FRESH)
        self.assertEqual(self.client.objects[r2.CATALOG], b"new")


if __name__ == "__main__":
    unittest.main()
