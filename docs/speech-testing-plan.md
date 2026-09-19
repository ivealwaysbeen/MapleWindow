# 대사(스케줄러 발화) 라이브 테스트 플랜

## Context

`SpeakCycleService`와 5개 `PhraseRule`(daily quest, 몬스터파크, weekly, boss weekly, boss monthly)은 108개
유닛 테스트로 모든 분기(all/some/none, 요일/날짜 버킷, 완료 판정)를 검증했지만, **실제 등록된 스케줄러 데이터로
라이브 검증은 아직 안 했다.** 캐릭터 렌더링/애니메이션/외형은 이번 세션에서 이미 실제 API로 라이브 검증 완료.

발화 주기가 기본 30분(`SpeakIntervalSeconds`)이라 그냥 기다리면서 테스트하기엔 비효율적 — 아래 "준비 작업"을
먼저 하고 시작할 것.

## 준비 작업 (테스트 시작 전)

1. **트레이 메뉴에 수동 트리거 추가.** `SpeakCycleService.EvaluateAndRaise()`는 이미 `public`이라(테스트를
   염두에 두고 그렇게 설계함) `TrayIconManager`에 "지금 확인" 같은 메뉴 항목만 추가하면 됨 —
   `AppBootstrapper.InitializeTray()`에서 이벤트 하나 더 연결. 30분 기다릴 필요 없이 즉시 발화 사이클을
   돌려볼 수 있고, 테스트 이후에도 사용자가 수동으로 확인하고 싶을 때 쓸 수 있는 정상 기능이라 남겨둬도 됨.
2. (선택) 위 트리거를 안 만들 경우엔 `%AppData%\MapleWindow\config.json`의 `SpeakIntervalSeconds`를 임시로
   30~60초로 낮춰서 테스트 후 1800으로 되돌리는 방법도 가능.

## 테스트 체크리스트

- [ ] 실제 캐릭터로 인게임 스케줄러에 일일퀘스트/몬스터파크/위클리 콘텐츠/보스 중 최소 하나 이상 등록해두기
      (등록 안 된 게 하나도 없으면 멘트 자체가 안 뜸 — 정상 동작이지만 테스트가 안 됨)
- [ ] 앱 실행 → 캐릭터 선택 → 초기 폴링(`PollOnceAsync`) 완료 대기
- [ ] 수동 트리거로 발화 사이클 실행 → 말풍선에 멘트가 뜨는지, 텍스트가 자연스러운지 확인
- [ ] `%LocalAppData%\MapleWindow\logs\app-*.log`에서 실제 `/scheduler/character-state` 응답 원문을 보고,
      화면에 뜬 멘트가 규칙과 맞는지 대조:
      - `registration_flag == "true"`인 항목만 대상인지
      - daily → weekly → boss(weekly → monthly) 순서로 큐에 쌓이는지
      - 완료 판정(contents: now/max 비교, quest: quest_state, boss: complete_flag)이 실제 데이터와 맞는지
- [ ] 여러 사이클 관찰: 말풍선이 5초 간격으로 큐를 순차 재생하고, 다 끝나면 사라지는지
- [ ] "설정" 창에서 등록된 콘텐츠 하나 뮤트 → 다음 사이클에서 그 멘트만 빠지는지
- [ ] "하루 1회만 보기" 설정 → 같은 날 두 번째 사이클부터 스킵되는지 (앱 재시작 후에도 유지되는지)
- [ ] 등록된 콘텐츠가 하나도 없거나 전부 완료 상태일 때 → 에러 없이 조용히 넘어가는지

## 실사용으로만 검증 가능한 가정 (다르면 코드에서 조정)

- **그룹 집계 멘트의 뮤트 처리**: 그룹 내 모든 개별 항목이 뮤트된 경우에만 집계 멘트도 스킵하도록 되어 있음
  (`daily.quest.allUndone`, `boss.weekly.allFalse.*`, `boss.weekly.allTrueUnderLimit`).
- **월간 보스 날짜 경계**: 1~20일(포함) / 21일~말일로 나뉨 — 사용자 의도와 다르면
  `BossMonthlyRule.EarlyLateBoundaryDay` 값만 조정.
- **몬스터파크 외 다른 daily `contents` 항목, `bossWeekly`/`bossMonthly` 외 다른 `cycle` 값**은 현재 멘트가
  없어 무음 처리됨 — 필요하면 `NamedDailyContentRule`/새 규칙 클래스 + `phrases.json`에 키 추가로 확장.

## 참고 경로

- 설정/캐시: `%AppData%\MapleWindow\config.json`, `phrases.json`, `notification-prefs.json`,
  `notification-state.json`
- 로그: `%LocalAppData%\MapleWindow\logs\app-*.log`
- 발화 규칙 코드: `src/MapleWindow.Core/Scheduler/PhraseRules/`
- 발화 문구(사용자 편집 가능): `%AppData%\MapleWindow\phrases.json` (앱 최초 실행 시
  `src/MapleWindow.Core/Phrases/phrases.default.json`에서 시드됨)
