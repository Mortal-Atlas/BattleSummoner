import os
import sys
import time
import json
import urllib.request
import urllib.error
import urllib.parse
from pathlib import Path

# Fix Windows console UTF-8 output
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass

BASE_URL = "https://www.mixamo.com/api/v1"
FALLBACK_CHAR_ID = "4f5d21e1-4c4b-4910-b92b-092c179f1108" # Mixamo default Y-Bot UUID

def load_env_token():
    env_file = Path(".env")
    if env_file.exists():
        with open(env_file, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line.startswith("MIXAMO_TOKEN="):
                    token = line.split("=", 1)[1].strip()
                    if (token.startswith('"') and token.endswith('"')) or (token.startswith("'") and token.endswith("'")):
                        token = token[1:-1]
                    return token
    token = os.environ.get("MIXAMO_TOKEN")
    if token:
        return token
    raise ValueError("MIXAMO_TOKEN not found in .env or environment variables.")

class MixamoClient:
    def __init__(self, token):
        self.token = token
        self.api_key = "mixamo1" # Starts with mixamo1; automatically retries with mixamo2 on 401/403

    def get_headers(self):
        return {
            "Authorization": f"Bearer {self.token}",
            "X-Api-Key": self.api_key,
            "X-Requested-With": "XMLHttpRequest",
            "Accept": "application/json",
            "Content-Type": "application/json",
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        }

    def request(self, method, endpoint, data=None):
        url = endpoint if endpoint.startswith("http") else f"{BASE_URL}{endpoint}"
        payload = json.dumps(data).encode("utf-8") if data is not None else None

        for attempt in range(4):
            req = urllib.request.Request(url, data=payload, headers=self.get_headers(), method=method)
            try:
                with urllib.request.urlopen(req, timeout=40) as resp:
                    resp_data = resp.read().decode("utf-8")
                    return json.loads(resp_data) if resp_data else {}
            except urllib.error.HTTPError as e:
                if e.code in (401, 403) and self.api_key == "mixamo2":
                    print(f"[*] API key mixamo2 returned HTTP {e.code}. Retrying with mixamo1...")
                    self.api_key = "mixamo1"
                    continue
                elif e.code in (401, 403) and self.api_key == "mixamo1":
                    print(f"[*] API key mixamo1 returned HTTP {e.code}. Retrying with mixamo2...")
                    self.api_key = "mixamo2"
                    continue
                elif e.code == 429:
                    wait_sec = 6 * (attempt + 1)
                    print(f"[!] HTTP 429 (Rate Limit). Backing off for {wait_sec}s...")
                    time.sleep(wait_sec)
                    continue
                err_body = e.read().decode("utf-8", errors="ignore")
                print(f"[!] HTTP {e.code} on {url}: {err_body}")
                raise e
            except Exception as e:
                print(f"[!] Error on {url}: {e}")
                raise e

    def get_primary_character_id(self):
        try:
            res = self.request("GET", "/characters/primary")
            if isinstance(res, dict):
                char_id = res.get("primary_character_id") or res.get("id")
                if char_id:
                    name = res.get("primary_character_name", "Primary")
                    print(f"[*] Primary character found: {name} (ID: {char_id})")
                    return char_id
        except Exception as e:
            print(f"[!] Warning on /characters/primary: {e}")

        try:
            res = self.request("GET", "/characters")
            items = res if isinstance(res, list) else res.get("results", [])
            if items:
                # Prefer user's character like malestarter
                for c in items:
                    if c.get("source") == "user" or c.get("type") == "user":
                        char_id = c.get("uuid") or c.get("id")
                        print(f"[*] Selected user character: {c.get('name')} (ID: {char_id})")
                        return char_id
                first = items[0]
                char_id = first.get("uuid") or first.get("id")
                print(f"[*] Selected system character: {first.get('name')} (ID: {char_id})")
                return char_id
        except Exception as e:
            print(f"[!] Warning on /characters: {e}")

        print(f"[*] Using fallback character ID: {FALLBACK_CHAR_ID}")
        return FALLBACK_CHAR_ID

    def fetch_animation(self, query: str, in_place: bool, output_name: str, char_id: str):
        print(f"\n========================================================")
        print(f"[*] Harvesting: '{query}' -> {output_name}.fbx (InPlace={in_place})")
        print(f"========================================================")

        encoded_query = urllib.parse.quote(query)
        search_res = self.request("GET", f"/products?page=1&limit=10&type=Motion&query={encoded_query}")

        items = search_res.get("results", search_res) if isinstance(search_res, dict) else search_res
        if not items or not isinstance(items, list):
            raise RuntimeError(f"No animations found for query: '{query}'")

        selected = None
        for item in items:
            if item.get("type") == "Motion":
                selected = item
                break
        if not selected:
            selected = items[0]

        anim_id = selected.get("id")
        anim_name = selected.get("name", query)
        print(f"[*] Found '{anim_name}' (ID: {anim_id})")

        prod_res = self.request("GET", f"/products/{anim_id}?similar=0&character_id={char_id}")
        details = prod_res.get("details", prod_res)
        gms_hash = details.get("gms_hash", {})
        if not gms_hash:
            raise RuntimeError(f"No gms_hash returned for animation: {anim_id}")

        params = gms_hash.get("params", [])
        new_params = []
        for p in params:
            if isinstance(p, (list, tuple)) and len(p) >= 2:
                name, val = p[0], p[1]
                name_str = str(name).lower()
                if "inplace" in name_str or "in place" in name_str:
                    val = 1 if in_place else 0
                new_params.append(val)
            elif isinstance(p, dict):
                name = p.get("name", "")
                val = p.get("value", 0)
                name_str = str(name).lower()
                if "inplace" in name_str or "in place" in name_str:
                    val = 1 if in_place else 0
                new_params.append(val)
            else:
                new_params.append(p)

        gms_hash["params"] = ",".join(str(int(float(x))) for x in new_params)
        gms_hash["inplace"] = in_place
        if "trim" in gms_hash and isinstance(gms_hash["trim"], (list, tuple)) and len(gms_hash["trim"]) >= 2:
            gms_hash["trim"] = [int(float(gms_hash["trim"][0])), int(float(gms_hash["trim"][1]))]

        export_payload = {
            "character_id": char_id,
            "gms_hash": [gms_hash],
            "preferences": {
                "format": "fbx7_2019",
                "skin": "false",
                "fps": "30",
                "reducekf": "0"
            },
            "type": "Motion",
            "product_name": output_name
        }

        print(f"[*] Requesting export from Mixamo...")
        export_res = self.request("POST", "/animations/export", export_payload)
        print(f"[*] Export initiated: {export_res}")

        # Poll monitor
        max_polls = 60
        download_url = None
        for p in range(max_polls):
            time.sleep(2.5)
            mon = self.request("GET", f"/characters/{char_id}/monitor")
            status = mon.get("status")
            print(f"  [Poll {p+1}/{max_polls}] Status: {status}")
            if status == "completed":
                download_url = mon.get("job_result") or mon.get("result") or mon.get("download_url") or mon.get("url")
                break
            elif status in ("error", "failed"):
                raise RuntimeError(f"Mixamo export job failed: {mon}")

        if not download_url:
            raise TimeoutError(f"Export job timed out for {output_name}")

        out_dir = Path("Assets/Animations/Mixamo/InPlace")
        out_dir.mkdir(parents=True, exist_ok=True)
        out_file = out_dir / f"{output_name}.fbx"

        print(f"[*] Downloading FBX to {out_file}...")
        urllib.request.urlretrieve(download_url, out_file)
        file_size_kb = out_file.stat().st_size / 1024.0
        print(f"[+] Successfully saved {out_file.name} ({file_size_kb:.1f} KB)")

        # Brief delay to prevent rate limiting
        time.sleep(3.5)
        return str(out_file)

def main():
    token = load_env_token()
    client = MixamoClient(token)
    char_id = client.get_primary_character_id()

    animations_to_harvest = [
        ("sword and shield idle", True, "Combat_Idle"),
        ("fast run", True, "Combat_Run"),
        ("sword and shield slash", True, "Attack_Slash_1"),
        ("sword and shield attack", True, "Attack_Slash_2"),
        ("great sword spin attack", True, "Attack_Spin_Heavy"),
        ("stand to roll", True, "Combat_Dodge_Roll")
    ]

    results = []
    for query, in_place, name in animations_to_harvest:
        try:
            path = client.fetch_animation(query, in_place, name, char_id)
            results.append((name, True, path))
        except Exception as e:
            print(f"[-] Failed to harvest {name}: {e}")
            results.append((name, False, str(e)))
            time.sleep(5) # Delay on failure before next attempt

    print("\n========================================================")
    print("HARVEST SUMMARY:")
    for name, success, info in results:
        status_sym = "[SUCCESS]" if success else "[FAILED]"
        print(f" {status_sym} {name}: {info}")
    print("========================================================")

if __name__ == "__main__":
    main()

