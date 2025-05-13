# 🎮 ActionFit Code Test - Unity Client Developer

---
## 리펙토링

1. Factory 패턴으로 생성 분리

BoardFactory - BoardBlockObject 생성 및 그룹화 (CheckBlockGroupDic 관리)
BlockGroupFactory - BlockGroupObject 및 BlockObject 생성, BlockDragHandler 연결
WallFactory -	WallObject 생성, WallCoordinateInfoDic 관리
ObjectFactory -	GameObject Instantiate 추상화


2. 입력 / 물리 / 데이터 / 렌더링 로직 분리

BlockDragController -	입력 감지 (OnMouseDown, OnMouseUp, 드래그 처리)
BlockPhysicsProcessor -	물리 연산 (속도, 충돌처리, Rigidbody 관리)
BlockDragHandler -	블록 그룹 드래그 상태 관리 (blocks 리스트, OnDragStart/End)
BoardController -	전체 조율 (Init, Stage Load, DestroyGroup 처리)

-> 입력-물리-데이터의 강결합을 해소하고, 명확하게 분리된 구조를 구현했습니다.

3. 의존성 주입 (DI) 기반 구조화
   
Factory 생성 시 필요한 데이터(프리팹, 거리, 딕셔너리)를 생성자 주입 방식으로 전달.
ObjectFactory 인터페이스를 통해 Instantiate를 추상화.
WallFactory, BoardFactory, BlockGroupFactory 모두 DI 기반 연결.

-> 테스트성, 유지보수성, 확장성이 크게 향상되었습니다.

4. Constants 클래스로 상수 통합 관리

-> Magic Number 제거

## Stage Editor

1. StageEditWindow

SO 기반 데이터 편집(StageData 직접 편집 가능)

2. Stage Editor

ObjectManager, EditController, UIController - UI를 이용한 커스텀 스테이지 구현
StageDataHandler - SO저장/로드

---
