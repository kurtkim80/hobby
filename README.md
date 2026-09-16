# 🎸 HobbyCollector | AI 취미 & 트렌드 허브 (WASM)

> **서버 비용 0원!** 100% 브라우저(WebAssembly)에서 구동되는 AI 시맨틱 벡터 검색 취미 허브입니다.  
> **GitHub Actions**가 백엔드 크론 서버 역할을 대신하여 6시간마다 YouTube 공식 RSS 피드를 무인 수집하고, **GitHub Pages**에 정적 SPA로 자동 배포됩니다.

[![Deploy Blazor WASM to GitHub Pages](https://github.com/kurtkim80/hobby/actions/workflows/deploy.yml/badge.svg)](https://github.com/kurtkim80/hobby/actions/workflows/deploy.yml)
[![Scheduled Hobby Video Collector](https://github.com/kurtkim80/hobby/actions/workflows/collect.yml/badge.svg)](https://github.com/kurtkim80/hobby/actions/workflows/collect.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0%20%7C%208.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://blazor.net)

---

## 🌐 라이브 데모 페이지 (Live Website)

아래 링크를 클릭하면 별도 설치나 로그인 없이 브라우저에서 즉시 실행됩니다:

👉 **[https://kurtkim80.github.io/hobby/](https://kurtkim80.github.io/hobby/)**

- **GitHub 저장소**: [https://github.com/kurtkim80/hobby](https://github.com/kurtkim80/hobby)
- **GitHub Actions 현황**: [https://github.com/kurtkim80/hobby/actions](https://github.com/kurtkim80/hobby/actions)

---

## ✨ 주요 기능 및 특징

1. **100% 클라이언트 사이드 실행 (Serverless Blazor WebAssembly)**
   - 백엔드 서버 없이 브라우저 단독으로 C# 코드가 실행됩니다.
   - 호스팅 비용 0원으로 평생 무료 정적 웹사이트 운영이 가능합니다.

2. **순수 C# 384차원 AI 시맨틱 벡터 검색 엔진**
   - Blazor WASM에서 지원되지 않는 C++ 네이티브 바이너리(SQLite dll, ONNX Runtime 등)를 완전히 배제했습니다.
   - 단어 해싱 + N-gram(2글자) 서브워드 + 문맥 가중치 해시 알고리즘 기반 순수 C# 벡터라이저(`LocalEmbeddingService.cs`) 탑재.
   - `System.Numerics.Tensors`를 통한 SIMD 고속 코사인 유사도 연산으로 단순 키워드 일치가 아닌 문맥 기반 AI 검색 지원.

3. **무인 백엔드 자동 수집 (GitHub Actions)**
   - 6시간 주기 크론(`0 */6 * * *`) 및 수동 실행 지원.
   - YouTube API 키가 필요 없는 공식 XML RSS 피드(`https://www.youtube.com/feeds/videos.xml?channel_id=...`)를 활용하여 차단 없이 안정적 수집.
   - 수집된 영상은 `seed_videos.json`에 누적되어 자동 Git 커밋/푸시되며, 즉시 GitHub Pages 재배포를 트리거합니다.

4. **브라우저 실시간 수집 지원 (Invidious CORS API 연동)**
   - 사용자가 "홍김동전", "스타크래프트" 등 임의의 키워드를 검색하고 **[⚡ 웹에서 추가수집]**을 누르면 브라우저 CORS 차단 없이 YouTube 실시간 검색 영상을 즉시 가져와 로컬 DB(`localStorage`)에 저장합니다.

5. **감각적인 다크 테마 & 동적 애니메이션 UI**
   - 부유하는 **실시간 앰비언트 오로라 배경 흐름**
   - 마우스 호버 시 썸네일 중앙에 튀어나오는 **동적 플레이 버튼(▶) 오버레이**
   - **Pretendard 폰트** 기반 글래스모피즘(Glassmorphism) 카드 디자인
   - 스마트 키워드 관리: 태그가 많아져도 기본 8개 노출 및 **[▼ 더보기/접기]** 토글 지원
   - **모바일, 태블릿, 데스크톱 완벽 대응 반응형(Responsive)** 레이아웃

---

## 🗂️ 9대 취미 & 관심사 카테고리 (280+ 영상 수집 중)

| 카테고리 | 대표 등록 채널 | 주요 콘텐츠 |
|---|---|---|
| 🎸 **기타 & 락** | Steve Vai, Rick Beato, Bernth | 일렉/어쿠스틱 기타 강좌, 락/메탈 분석 |
| 🍎 **Mac & 테크** | ITSub잇섭, 퀘이사존 | Mac mini, MacBook, 최신 PC/IT 하드웨어 |
| 🎮 **스타크래프트** | 액션홍구, 이영호FlaSh, 택신TV [김택용] | ASL 대회 하이라이트, 전설의 프로게이머 명경기 |
| 📺 **TV 예능** | 놀면 뭐하니?, 뜬뜬(핑계고), 홍인규 게임TV | 인기 코미디/토크쇼, 골프/게임 예능 |
| 🤖 **AI & 로봇** | Boston Dynamics | 휴머노이드 로봇(아틀라스), 최첨단 피지컬 AI |
| 💻 **개발 & 코딩** | 조코딩, 노마드코더 | 최신 웹 개발, AI 활용 코딩, 테크 트렌드 |
| 🎬 **영화 & 리뷰** | 지무비 (G Movie) | 1위 영화/명작 드라마 요약 및 몰아보기 |
| 🔬 **과학 & 지식** | 안될과학 (Unrealscience) | 우주, 양자역학, 신기술 교양 과학 |
| 🕹️ **게임 & 스트리머** | 침착맨 | 종합 게임 플레이 및 만담 토크 |

---

## 📂 프로젝트 아키텍처

```
HobbyCollector/
├── .github/workflows/
│   ├── deploy.yml             # GitHub Pages 자동 빌드 및 배포 (CI/CD)
│   └── collect.yml            # 6시간 주기 무인 YouTube 수집 크론 워크플로우
├── Models/
│   └── Models.cs              # VideoItem, SearchResultItem, UserKeywordItem, TrendingTopic
├── Pages/
│   └── Index.razor            # 메인 대시보드 UI (다크 테마 반응형 SPA)
├── Services/
│   ├── IEmbeddingService.cs   # 임베딩 인터페이스
│   ├── LocalEmbeddingService.cs # 순수 C# 384차원 N-gram 해싱 벡터 생성기
│   ├── WasmVectorStore.cs     # 인메모리 벡터 인덱스 + localStorage 영구 동기화
│   └── WasmCollectorService.cs # 브라우저용 실시간 수집기 (Invidious CORS API 연동)
├── scripts/
│   └── collect_videos.py      # GitHub Actions에서 실행되는 YouTube RSS 무인 수집기
├── wwwroot/
│   ├── data/
│   │   └── seed_videos.json   # 280+ 검증 영상 데이터베이스
│   ├── index.html             # 오로라 애니메이션 CSS 및 동적 base href 주입
│   ├── 404.html               # GitHub Pages SPA 라우팅 fallback
│   └── .nojekyll              # GitHub Pages Jekyll 빌드 비활성화
├── App.razor
├── _Imports.razor
├── Program.cs                 # WebAssemblyHostBuilder 진입점
└── HobbyCollector.csproj
```

---

## 🛠️ 로컬 개발 환경에서 실행하는 방법

### 1. 필수 구성 요소
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) 또는 **.NET 10.0 SDK** 이상
- Python 3.10 이상 (수집 스크립트 수동 실행 시)

### 2. 프로젝트 클론 및 실행
```bash
# 저장소 복제
git clone https://github.com/kurtkim80/hobby.git
cd hobby

# 로컬 개발 서버 실행
dotnet run
```
브라우저에서 `http://localhost:5000` (또는 콘솔에 안내되는 포트)로 접속합니다.

### 3. 수집 스크립트 로컬 수동 테스트
```bash
python3 scripts/collect_videos.py
```

---

## 📄 라이선스 (License)
This project is open source and available under the [MIT License](LICENSE).
