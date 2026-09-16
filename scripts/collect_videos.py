import json
import urllib.request
import xml.etree.ElementTree as ET
import ssl
import os

SEED_FILE = "wwwroot/data/seed_videos.json"

# 검증된 실제 YouTube 채널 ID 목록
CHANNELS = [
    # 🎮 스타크래프트
    {"category": "🎮 스타크래프트", "channel_name": "액션홍구", "channel_id": "UCaNGiz6D2vHP7XLj9d5691A"},
    # 📺 TV 예능 & 코미디
    {"category": "📺 TV 예능 & 코미디", "channel_name": "홍인규 게임TV", "channel_id": "UC543-TDm6IvHHZXwdSFioMw"},
    {"category": "📺 TV 예능 & 코미디", "channel_name": "뜬뜬(핑계고)", "channel_id": "UCDNvRZRgvkBTUkQzFoT_8rA"},
    # 🎸 기타 & 락/메탈
    {"category": "🎸 기타 & 락/메탈", "channel_name": "Steve Vai", "channel_id": "UCdkBa5GZKEAfiTjqfqotWhQ"},
    {"category": "🎸 기타 & 락/메탈", "channel_name": "Rick Beato", "channel_id": "UCJquYOG5EL82sKTfH9aMA9Q"},
    {"category": "🎸 기타 & 락/메탈", "channel_name": "Bernth", "channel_id": "UCZvo8TZtUZkLgiH3rJsj-Ow"},
    # 🍎 Mac & 테크
    {"category": "🍎 Mac & 테크", "channel_name": "퀘이사존", "channel_id": "UC17_4RLogNieDO33smNlcWw"},
    # 🤖 AI & 로봇
    {"category": "🤖 AI & 로봇", "channel_name": "Boston Dynamics", "channel_id": "UC7vVhkEfw4nOGp8TyDk7RcQ"}
]

ssl_context = ssl.create_default_context()
ssl_context.check_hostname = False
ssl_context.verify_mode = ssl.CERT_NONE

def load_existing():
    if os.path.exists(SEED_FILE):
        with open(SEED_FILE, "r", encoding="utf-8") as f:
            try:
                return json.load(f)
            except Exception:
                return []
    return []

def fetch_channel_rss(channel_id, channel_name, category):
    url = f"https://www.youtube.com/feeds/videos.xml?channel_id={channel_id}"
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)"})
    videos = []
    try:
        with urllib.request.urlopen(req, context=ssl_context, timeout=10) as resp:
            content = resp.read().decode("utf-8")
            root = ET.fromstring(content)
            ns = {
                "atom": "http://www.w3.org/2005/Atom",
                "yt": "http://www.youtube.com/xml/schemas/2015",
                "media": "http://search.yahoo.com/mrss/"
            }
            
            for entry in root.findall("atom:entry", ns)[:15]:
                vid_elem = entry.find("yt:videoId", ns)
                title_elem = entry.find("atom:title", ns)
                pub_elem = entry.find("atom:published", ns)
                desc_elem = entry.find("media:group/media:description", ns)
                
                if vid_elem is None or title_elem is None:
                    continue
                    
                vid = vid_elem.text
                title = title_elem.text
                pub = pub_elem.text if pub_elem is not None else ""
                desc = desc_elem.text if desc_elem is not None and desc_elem.text else f"{channel_name} 공식 최신 영상"
                
                videos.append({
                    "Id": vid,
                    "Title": title,
                    "ChannelTitle": channel_name,
                    "Category": category,
                    "Url": f"https://www.youtube.com/watch?v={vid}",
                    "PublishedAt": pub,
                    "Description": desc[:200]
                })
    except Exception as e:
        print(f"[{channel_name}] RSS 조회 실패: {e}")
    return videos

def main():
    existing = load_existing()
    existing_dict = {v.get("Id") or v.get("id"): v for v in existing if (v.get("Id") or v.get("id"))}
    initial_count = len(existing_dict)

    print(f"기존 저장된 영상 수: {initial_count}")

    for ch in CHANNELS:
        new_vids = fetch_channel_rss(ch["channel_id"], ch["channel_name"], ch["category"])
        print(f"[{ch['channel_name']}] 수집된 영상 수: {len(new_vids)}")
        for v in new_vids:
            existing_dict[v["Id"]] = v

    final_list = list(existing_dict.values())
    final_list.sort(key=lambda x: x.get("PublishedAt") or x.get("publishedAt") or "", reverse=True)

    with open(SEED_FILE, "w", encoding="utf-8") as f:
        json.dump(final_list, f, ensure_ascii=False, indent=2)

    print(f"수집 완료: 총 {len(final_list)}개 저장됨 (신규 추가: {len(final_list) - initial_count}개)")

if __name__ == "__main__":
    main()
