import React, { useState, useEffect, useRef, useCallback } from "react";

// localStorage 저장 키
const FLOOR_IMAGES_STORAGE_KEY = "space_editor_floor_images_v1";
const NODE_COLORS_STORAGE_KEY = "space_editor_node_colors_v1";

// 플로어별 2D 평면도 이미지 매핑
const FLOOR_IMAGES = {
    B1: "/images/floor_B1.jpg",
    "1F": "/images/floor_1.jpg",
    "2F": "/images/floor_2.jpg",
    "3F": "/images/floor_3.jpg",
};

// floor_info 문자열에서 플로어 탭 키 추출
const extractFloor = (floorInfo) => {
    if (!floorInfo) return "1F";
    if (floorInfo.includes("B1") || floorInfo.includes("b1")) return "B1";
    if (floorInfo.includes("3F") || floorInfo.includes("3f") || floorInfo.includes("3층")) return "3F";
    if (floorInfo.includes("2F") || floorInfo.includes("2f") || floorInfo.includes("2층")) return "2F";
    return "1F";
};

// 노드 색상 프리셋
const NODE_COLORS = [
    { value: "#135bec", label: "파랑" },
    { value: "#f43f5e", label: "빨강" },
    { value: "#10b981", label: "초록" },
    { value: "#f59e0b", label: "주황" },
    { value: "#8b5cf6", label: "보라" },
    { value: "#06b6d4", label: "하늘" },
];

// Immersal 좌표 → 캔버스 픽셀 퍼센트 변환 (범위: -5 ~ 5)
const coordToPercent = (coord) => {
    const num = parseFloat(coord);
    if (isNaN(num)) return 50;
    // -5 ~ 5 범위를 0 ~ 100%로 매핑
    return Math.max(5, Math.min(95, ((num + 5) / 10) * 100));
};

// 캔버스 픽셀 퍼센트 → Immersal 좌표 역변환
const percentToCoord = (percent) => {
    return ((percent / 100) * 10 - 5).toFixed(3);
};

// 지도 에디터 페이지 — 2D 평면도 위 전시품 노드 드래그 배치
function SpaceEditor() {
    const [artworks, setArtworks] = useState([]);
    const [loading, setLoading] = useState(true);
    const [activeFloor, setActiveFloor] = useState("1F");

    // 맵 관리에서 등록된 2D 평면도 목록
    const [floorPlanMaps, setFloorPlanMaps] = useState([]);
    // 플로어별 선택된 지도 URL: { '1F': url, 'B1': url, ... } — localStorage에서 초기값 복원
    const [selectedFloorImages, setSelectedFloorImages] = useState(() => {
        try {
            const saved = localStorage.getItem(FLOOR_IMAGES_STORAGE_KEY);
            return saved ? JSON.parse(saved) : {};
        } catch {
            return {};
        }
    });
    // 지도 변경 드롭다운 열림 여부
    const [mapPickerOpen, setMapPickerOpen] = useState(false);
    const mapPickerRef = useRef(null);

    // 노드 표시 상태: { artworkId: boolean }
    const [visibleNodes, setVisibleNodes] = useState({});

    // 노드 위치 상태: { artworkId: { x: percent, y: percent } }
    const [nodePositions, setNodePositions] = useState({});

    // 노드 색상 상태: { artworkId: colorHex } — localStorage에서 초기값 복원
    const [nodeColors, setNodeColors] = useState(() => {
        try {
            const saved = localStorage.getItem(NODE_COLORS_STORAGE_KEY);
            return saved ? JSON.parse(saved) : {};
        } catch {
            return {};
        }
    });

    // 드래그 상태
    const [dragging, setDragging] = useState(null); // { artworkId, startX, startY, origX, origY }

    // 줌 레벨
    const [zoom, setZoom] = useState(1.0);

    // 색상 피커 팝업 표시 상태: artworkId | null
    const [colorPickerFor, setColorPickerFor] = useState(null);

    // 저장 상태 피드백
    const [saveMsg, setSaveMsg] = useState("");

    // 호버 툴팁 상태
    const [tooltip, setTooltip] = useState(null); // { artworkId, mouseX, mouseY }

    // 선택된 노드 (우측 패널 강조)
    const [selectedNode, setSelectedNode] = useState(null);

    // 캔버스 크기 — 이미지 로드 시 원본 비율에 맞게 동적 조정
    const [canvasSize, setCanvasSize] = useState({ width: 800, height: 560 });

    const canvasRef = useRef(null);
    const imageLoadError = useRef({});

    // 전시품 목록 로드
    const fetchArtworks = async () => {
        try {
            const res = await fetch("/api/artworks");
            if (res.ok) {
                const result = await res.json();
                const data = result.data || [];
                setArtworks(data);

                // 초기 위치·색상·표시 상태 설정
                const positions = {};
                const colors = {};
                const visible = {};
                data.forEach((a) => {
                    positions[a.id] = {
                        x: coordToPercent(a.pos_x),
                        // Z축은 화면 Y축 (Z 값 반전: -z+5 → 0~100%)
                        y: Math.max(5, Math.min(95, ((-parseFloat(a.pos_z) + 5) / 10) * 100)),
                    };
                    colors[a.id] = NODE_COLORS[0].value; // 기본 파랑, 저장값이 있으면 덮어씌워짐
                    // pos_x, pos_z 값이 있으면 기본 표시
                    visible[a.id] = !!(a.pos_x && a.pos_z);
                });
                setNodePositions(positions);
                // 저장된 색상(prev)을 기본값(colors) 위에 덮어씌워 복원
                setNodeColors((prev) => ({ ...colors, ...prev }));
                setVisibleNodes(visible);
            }
        } catch (err) {
            console.error("전시품 로드 실패:", err);
        } finally {
            setLoading(false);
        }
    };

    // 맵 관리에서 등록된 floor_plan 목록 로드 + DB floor 필드로 선택 상태 복원
    const fetchFloorPlanMaps = async () => {
        try {
            const res = await fetch("/api/maps");
            if (res.ok) {
                const result = await res.json();
                // file_url이 있는 2D 평면도만 필터링
                const plans = (result.data || []).filter(
                    (m) => m.map_type === "floor_plan" && m.file_url
                );
                setFloorPlanMaps(plans);

                // DB의 floor 필드로 선택 상태 복원 (localStorage보다 DB 우선)
                const dbFloorImages = {};
                plans.forEach((m) => {
                    if (m.floor && m.file_url) {
                        dbFloorImages[m.floor] = m.file_url;
                    }
                });
                if (Object.keys(dbFloorImages).length > 0) {
                    setSelectedFloorImages((prev) => ({ ...prev, ...dbFloorImages }));
                }
            }
        } catch (err) {
            console.error("평면도 맵 로드 실패:", err);
        }
    };

    useEffect(() => {
        fetchArtworks();
        fetchFloorPlanMaps();
    }, []);

    // 드롭다운 외부 클릭 시 닫기
    useEffect(() => {
        const handleClickOutside = (e) => {
            if (mapPickerRef.current && !mapPickerRef.current.contains(e.target)) {
                setMapPickerOpen(false);
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    // 현재 플로어에 해당하는 전시품 필터링
    const floorArtworks = artworks.filter((a) => extractFloor(a.floor_info) === activeFloor);

    // 노드 드래그 시작
    const handleMouseDown = useCallback(
        (e, artworkId) => {
            e.preventDefault();
            e.stopPropagation();
            setSelectedNode(artworkId);
            setColorPickerFor(null);
            const canvas = canvasRef.current;
            if (!canvas) return;
            const rect = canvas.getBoundingClientRect();
            setDragging({
                artworkId,
                startX: e.clientX,
                startY: e.clientY,
                origX: nodePositions[artworkId]?.x ?? 50,
                origY: nodePositions[artworkId]?.y ?? 50,
                rectW: rect.width,
                rectH: rect.height,
            });
        },
        [nodePositions]
    );

    // 드래그 이동
    const handleMouseMove = useCallback(
        (e) => {
            if (!dragging) return;
            const canvas = canvasRef.current;
            if (!canvas) return;
            const dx = e.clientX - dragging.startX;
            const dy = e.clientY - dragging.startY;
            const newX = Math.max(2, Math.min(98, dragging.origX + (dx / dragging.rectW) * 100));
            const newY = Math.max(2, Math.min(98, dragging.origY + (dy / dragging.rectH) * 100));
            setNodePositions((p) => ({
                ...p,
                [dragging.artworkId]: { x: newX, y: newY },
            }));
        },
        [dragging]
    );

    // 드래그 종료
    const handleMouseUp = useCallback(() => {
        setDragging(null);
    }, []);

    // 이미지 로드 완료 시 원본 비율에 맞게 캔버스 크기 재계산
    const handleImageLoad = useCallback((e) => {
        const { naturalWidth, naturalHeight } = e.target;
        if (!naturalWidth || !naturalHeight) return;
        const MAX_W = 800;
        const MAX_H = 700;
        let w = MAX_W;
        let h = Math.round(MAX_W * (naturalHeight / naturalWidth));
        if (h > MAX_H) {
            h = MAX_H;
            w = Math.round(MAX_H * (naturalWidth / naturalHeight));
        }
        setCanvasSize({ width: w, height: h });
    }, []);

    useEffect(() => {
        if (dragging) {
            window.addEventListener("mousemove", handleMouseMove);
            window.addEventListener("mouseup", handleMouseUp);
        }
        return () => {
            window.removeEventListener("mousemove", handleMouseMove);
            window.removeEventListener("mouseup", handleMouseUp);
        };
    }, [dragging, handleMouseMove, handleMouseUp]);

    // 노드 표시 토글
    const toggleVisible = (artworkId) => {
        setVisibleNodes((p) => ({ ...p, [artworkId]: !p[artworkId] }));
    };

    // 변경된 위치 서버에 저장 (PATCH /api/artworks/:id)
    const handleSavePositions = async () => {
        const changed = artworks.filter((a) => visibleNodes[a.id]);
        let successCount = 0;
        for (const artwork of changed) {
            const pos = nodePositions[artwork.id];
            if (!pos) continue;
            const newPosX = percentToCoord(pos.x);
            // Y퍼센트 → Z좌표 역변환 (Y축 반전)
            const newPosZ = (-(pos.y / 100 * 10 - 5)).toFixed(3);
            try {
                const res = await fetch(`/api/artworks/${artwork.id}`, {
                    method: "PATCH",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ pos_x: String(newPosX), pos_z: String(newPosZ) }),
                });
                if (res.ok) {
                    successCount++;
                } else if (res.status === 401 || res.status === 403) {
                    setSaveMsg("관리자 인증 후 저장 가능합니다 (로컬 상태는 유지됨)");
                    return;
                }
            } catch (err) {
                console.error(`위치 저장 실패 (${artwork.title}):`, err);
            }
        }
        // 노드 색상도 함께 localStorage에 보존
        try {
            localStorage.setItem(NODE_COLORS_STORAGE_KEY, JSON.stringify(nodeColors));
        } catch (err) {
            console.error("색상 저장 실패:", err);
        }
        setSaveMsg(`${successCount}개 위치·색상 저장 완료`);
        setTimeout(() => setSaveMsg(""), 2500);
    };

    // 선택된 지도 이미지를 DB floor 필드와 localStorage에 동시 저장
    const handleSaveFloorImages = async () => {
        // DB 동기화: 플로어별 선택된 맵의 floor 필드를 업데이트
        for (const [floor, fileUrl] of Object.entries(selectedFloorImages)) {
            if (!fileUrl) continue;
            const map = floorPlanMaps.find((m) => m.file_url === fileUrl);
            if (!map) continue;
            try {
                await fetch(`/api/maps/${map.id}`, {
                    method: "PATCH",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ floor }),
                });
            } catch (err) {
                console.error(`지도 floor 동기화 실패 (${floor}):`, err);
            }
        }
        // localStorage에도 보존 (오프라인 폴백)
        try {
            localStorage.setItem(FLOOR_IMAGES_STORAGE_KEY, JSON.stringify(selectedFloorImages));
            setSaveMsg("지도 설정 저장 완료 (앱에 반영됨)");
            setTimeout(() => setSaveMsg(""), 2500);
        } catch (err) {
            console.error("지도 설정 저장 실패:", err);
        }
    };

    // 툴팁에 표시할 전시품 데이터 (artworkId로 찾기)
    const tooltipArtwork = tooltip ? artworks.find((a) => a.id === tooltip.artworkId) : null;
    const tooltipPos = tooltip ? nodePositions[tooltip.artworkId] : null;

    return (
        <div
            className="flex-1 flex overflow-hidden bg-[#101622]"
            style={{ userSelect: dragging ? "none" : "auto" }}
        >
            {/* 호버 툴팁 — transform 컨테이너 바깥에서 렌더링해야 fixed 좌표가 뷰포트 기준으로 동작 */}
            {tooltip && tooltipArtwork && (
                <div
                    className="fixed z-[9999] pointer-events-none"
                    style={{
                        left: tooltip.mouseX + 14,
                        top: tooltip.mouseY - 8,
                    }}
                >
                    <div className="bg-[#111318] border border-slate-600 rounded-lg p-2 w-36 shadow-2xl">
                        <div className="w-full h-16 rounded overflow-hidden mb-1.5 bg-[#1e2430]">
                            <img
                                src={tooltipArtwork.image_url || "/images/nophoto.png"}
                                alt={tooltipArtwork.title}
                                className="w-full h-full object-cover"
                            />
                        </div>
                        <p className="text-xs font-semibold text-white truncate">
                            {tooltipArtwork.title}
                        </p>
                        {tooltipPos && (
                            <p className="text-[10px] text-slate-400 mt-0.5">
                                X: {percentToCoord(tooltipPos.x)} / Z:{" "}
                                {(-(tooltipPos.y / 100 * 10 - 5)).toFixed(3)}
                            </p>
                        )}
                    </div>
                </div>
            )}
            {/* 중앙: 지도 캔버스 영역 */}
            <div className="flex-1 flex flex-col overflow-hidden">
                {/* 플로어 선택 탭 */}
                <div className="flex-shrink-0 px-4 py-3 border-b border-slate-800 bg-[#101622] flex items-center gap-2">
                    {["B1", "1F", "2F", "3F"].map((floor) => (
                        <button
                            key={floor}
                            onClick={() => setActiveFloor(floor)}
                            className={`px-3 py-1.5 rounded-lg text-xs font-bold transition-all ${
                                activeFloor === floor
                                    ? "bg-primary text-white shadow-md shadow-blue-500/20"
                                    : "text-slate-400 hover:text-white hover:bg-slate-800"
                            }`}
                        >
                            {floor}
                        </button>
                    ))}

                    <div className="ml-auto flex items-center gap-2">
                        {saveMsg && (
                            <span className="text-xs text-emerald-400">{saveMsg}</span>
                        )}

                        {/* 지도 변경 드롭다운 버튼 */}
                        <div className="relative" ref={mapPickerRef}>
                            <button
                                onClick={() => setMapPickerOpen((p) => !p)}
                                className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold rounded-lg border transition-all ${
                                    mapPickerOpen
                                        ? "bg-primary/10 border-primary text-primary"
                                        : "border-slate-700 text-slate-400 hover:text-white hover:border-slate-500"
                                }`}
                            >
                                <span className="material-symbols-outlined" style={{ fontSize: "14px" }}>
                                    map
                                </span>
                                지도 변경
                                <span className="material-symbols-outlined" style={{ fontSize: "14px" }}>
                                    {mapPickerOpen ? "expand_less" : "expand_more"}
                                </span>
                            </button>

                            {/* 드롭다운 패널 */}
                            {mapPickerOpen && (
                                <div className="absolute right-0 top-full mt-1.5 w-64 bg-[#1e2430] border border-slate-700 rounded-xl shadow-2xl z-50 overflow-hidden">
                                    <div className="px-3 py-2 border-b border-slate-700">
                                        <p className="text-xs font-semibold text-white">2D 평면도 선택</p>
                                        <p className="text-[10px] text-slate-500 mt-0.5">{activeFloor} 지도에 적용됩니다</p>
                                    </div>
                                    <div className="max-h-56 overflow-y-auto py-1">
                                        {/* 기본 이미지로 초기화 옵션 */}
                                        <button
                                            onClick={() => {
                                                setSelectedFloorImages((p) => {
                                                    const next = { ...p };
                                                    delete next[activeFloor];
                                                    return next;
                                                });
                                                setMapPickerOpen(false);
                                            }}
                                            className={`w-full flex items-center gap-3 px-3 py-2 text-left hover:bg-slate-700/50 transition-colors ${
                                                !selectedFloorImages[activeFloor] ? "text-primary" : "text-slate-400"
                                            }`}
                                        >
                                            <div className="w-8 h-8 rounded bg-[#111318] border border-slate-600 flex items-center justify-center flex-shrink-0">
                                                <span className="material-symbols-outlined text-slate-500" style={{ fontSize: "16px" }}>
                                                    image
                                                </span>
                                            </div>
                                            <div className="flex-1 min-w-0">
                                                <p className="text-xs font-medium">기본 이미지</p>
                                                <p className="text-[10px] text-slate-500">로컬 기본 평면도 사용</p>
                                            </div>
                                            {!selectedFloorImages[activeFloor] && (
                                                <span className="material-symbols-outlined text-primary" style={{ fontSize: "16px" }}>
                                                    check
                                                </span>
                                            )}
                                        </button>

                                        {/* 등록된 평면도 목록 */}
                                        {floorPlanMaps.length === 0 ? (
                                            <p className="text-[10px] text-slate-600 text-center py-4">
                                                등록된 2D 평면도가 없습니다
                                            </p>
                                        ) : (
                                            floorPlanMaps.map((m) => {
                                                const isActive = selectedFloorImages[activeFloor] === m.file_url;
                                                return (
                                                    <button
                                                        key={m.id}
                                                        onClick={() => {
                                                            setSelectedFloorImages((p) => ({
                                                                ...p,
                                                                [activeFloor]: m.file_url,
                                                            }));
                                                            setMapPickerOpen(false);
                                                        }}
                                                        className={`w-full flex items-center gap-3 px-3 py-2 text-left hover:bg-slate-700/50 transition-colors ${
                                                            isActive ? "text-primary" : "text-slate-300"
                                                        }`}
                                                    >
                                                        {/* 썸네일 미리보기 */}
                                                        <div className="w-8 h-8 rounded bg-[#111318] border border-slate-600 flex-shrink-0 overflow-hidden">
                                                            <img
                                                                src={m.file_url}
                                                                alt={m.name}
                                                                className="w-full h-full object-cover"
                                                                onError={(e) => {
                                                                    e.target.style.display = "none";
                                                                    e.target.parentNode.innerHTML =
                                                                        '<span class="material-symbols-outlined text-slate-500 flex items-center justify-center w-full h-full text-base">layers</span>';
                                                                }}
                                                            />
                                                        </div>
                                                        <div className="flex-1 min-w-0">
                                                            <p className="text-xs font-medium truncate">{m.name}</p>
                                                            <p className="text-[10px] text-slate-500">
                                                                {new Date(m.updated_at || m.created_at).toLocaleDateString("ko-KR")}
                                                            </p>
                                                        </div>
                                                        {isActive && (
                                                            <span className="material-symbols-outlined text-primary" style={{ fontSize: "16px" }}>
                                                                check
                                                            </span>
                                                        )}
                                                    </button>
                                                );
                                            })
                                        )}
                                    </div>
                                </div>
                            )}
                        </div>

                        <button
                            onClick={handleSaveFloorImages}
                            className="flex items-center gap-1.5 px-3 py-1.5 bg-indigo-700 hover:bg-indigo-600 text-white text-xs font-bold rounded-lg transition-colors"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "14px" }}>
                                save
                            </span>
                            지도 저장
                        </button>

                        <button
                            onClick={handleSavePositions}
                            className="flex items-center gap-1.5 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold rounded-lg transition-colors"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "14px" }}>
                                save
                            </span>
                            위치 저장
                        </button>
                    </div>
                </div>

                {/* 캔버스 영역 */}
                <div className="flex-1 bg-[#0f1218] relative overflow-hidden flex items-center justify-center">
                    {/* 그리드 배경 패턴 */}
                    <div
                        className="absolute inset-0 opacity-20 pointer-events-none"
                        style={{
                            backgroundImage:
                                "linear-gradient(to right, #334155 1px, transparent 1px), linear-gradient(to bottom, #334155 1px, transparent 1px)",
                            backgroundSize: "20px 20px",
                        }}
                    />

                    {/* 지도 컨테이너 — 이미지 원본 비율에 맞게 크기 동적 조정 */}
                    <div
                        ref={canvasRef}
                        className="relative bg-[#1e293b] shadow-2xl rounded-sm border border-slate-700 overflow-hidden"
                        style={{
                            width: `${canvasSize.width}px`,
                            height: `${canvasSize.height}px`,
                            transform: `scale(${zoom})`,
                            transformOrigin: "center",
                        }}
                        onClick={() => {
                            setSelectedNode(null);
                            setColorPickerFor(null);
                        }}
                    >
                        {/* 평면도 이미지 — 맵 관리에서 선택한 이미지 우선, 없으면 로컬 기본 이미지 */}
                        <img
                            key={selectedFloorImages[activeFloor] || activeFloor}
                            src={selectedFloorImages[activeFloor] || FLOOR_IMAGES[activeFloor]}
                            alt={`${activeFloor} 평면도`}
                            className="absolute inset-0 w-full h-full object-fill"
                            onLoad={handleImageLoad}
                            onError={(e) => {
                                // 이미지 로드 실패 시 숨김 처리
                                e.target.style.display = "none";
                            }}
                        />

                        {/* 전시품 노드 오버레이 */}
                        {floorArtworks
                            .filter((a) => visibleNodes[a.id])
                            .map((artwork) => {
                                const pos = nodePositions[artwork.id] || { x: 50, y: 50 };
                                const color = nodeColors[artwork.id] || NODE_COLORS[0].value;
                                const isSelected = selectedNode === artwork.id;

                                return (
                                    <div key={artwork.id}>
                                        {/* 노드 원형 마커 */}
                                        <div
                                            className="absolute cursor-grab active:cursor-grabbing transition-transform"
                                            style={{
                                                left: `${pos.x}%`,
                                                top: `${pos.y}%`,
                                                transform: "translate(-50%, -50%)",
                                                zIndex: isSelected ? 30 : 20,
                                            }}
                                            onMouseDown={(e) => handleMouseDown(e, artwork.id)}
                                            onMouseEnter={(e) =>
                                                setTooltip({ artworkId: artwork.id, mouseX: e.clientX, mouseY: e.clientY })
                                            }
                                            onMouseLeave={() => setTooltip(null)}
                                            onMouseMove={(e) =>
                                                setTooltip((t) =>
                                                    t ? { ...t, mouseX: e.clientX, mouseY: e.clientY } : null
                                                )
                                            }
                                        >
                                            <div
                                                className="rounded-full border-2 border-white/70 shadow-lg transition-all"
                                                style={{
                                                    width: isSelected ? 24 : 18,
                                                    height: isSelected ? 24 : 18,
                                                    backgroundColor: color,
                                                    boxShadow: isSelected
                                                        ? `0 0 0 4px ${color}40, 0 4px 12px ${color}80`
                                                        : `0 2px 8px ${color}60`,
                                                }}
                                            />
                                        </div>

                                    </div>
                                );
                            })}
                    </div>

                    {/* 하단 줌 컨트롤 */}
                    <div className="absolute bottom-6 left-1/2 -translate-x-1/2 bg-[#1e2430] rounded-full shadow-lg border border-slate-700 flex items-center px-4 py-2 gap-3 text-white">
                        <button
                            onClick={() => setZoom((z) => Math.max(0.5, z - 0.1))}
                            className="text-slate-400 hover:text-white transition-colors"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "18px" }}>
                                remove
                            </span>
                        </button>
                        <span className="text-xs font-medium w-10 text-center">
                            {Math.round(zoom * 100)}%
                        </span>
                        <button
                            onClick={() => setZoom((z) => Math.min(2, z + 0.1))}
                            className="text-slate-400 hover:text-white transition-colors"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "18px" }}>
                                add
                            </span>
                        </button>
                        <div className="w-px h-4 bg-slate-600" />
                        <button
                            onClick={() => setZoom(1.0)}
                            className="text-slate-400 hover:text-white transition-colors"
                            title="100%로 초기화"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "18px" }}>
                                fit_screen
                            </span>
                        </button>
                    </div>
                </div>
            </div>

            {/* 우측 패널: 전시품 목록 */}
            <div className="w-72 flex-shrink-0 border-l border-slate-800 bg-[#151a25] flex flex-col">
                <div className="p-4 border-b border-slate-800">
                    <h3 className="text-sm font-bold text-white">전시품 목록</h3>
                    <p className="text-xs text-slate-500 mt-0.5">
                        {activeFloor} — {floorArtworks.length}개
                    </p>
                </div>

                <div className="flex-1 overflow-y-auto p-3 space-y-1.5">
                    {loading ? (
                        <div className="space-y-2">
                            {[1, 2, 3].map((i) => (
                                <div key={i} className="h-14 bg-[#1e2430] rounded-lg animate-pulse" />
                            ))}
                        </div>
                    ) : floorArtworks.length === 0 ? (
                        <div className="flex flex-col items-center justify-center py-10 text-slate-600">
                            <span className="material-symbols-outlined text-3xl mb-2">inventory_2</span>
                            <p className="text-xs text-center">이 층에 등록된 전시품이 없습니다</p>
                        </div>
                    ) : (
                        floorArtworks.map((artwork) => {
                            const isVisible = !!visibleNodes[artwork.id];
                            const isSelected = selectedNode === artwork.id;
                            const color = nodeColors[artwork.id] || NODE_COLORS[0].value;

                            return (
                                <div
                                    key={artwork.id}
                                    onClick={() => {
                                        setSelectedNode(artwork.id);
                                        setColorPickerFor(null);
                                    }}
                                    className={`p-2.5 rounded-lg border cursor-pointer transition-all ${
                                        isSelected
                                            ? "bg-primary/10 border-primary"
                                            : "bg-[#1e2430] border-slate-700 hover:border-slate-600"
                                    }`}
                                >
                                    <div className="flex items-center gap-2">
                                        {/* 썸네일 */}
                                        <div className="w-8 h-8 rounded overflow-hidden bg-[#111318] flex-shrink-0">
                                            <img
                                                src={artwork.image_url || "/images/nophoto.png"}
                                                alt={artwork.title}
                                                className="w-full h-full object-cover opacity-80"
                                            />
                                        </div>

                                        <div className="flex-1 min-w-0">
                                            <p className="text-xs font-medium text-white truncate">{artwork.title}</p>
                                            <p className="text-[10px] text-slate-500">{artwork.floor_info || "-"}</p>
                                        </div>

                                        {/* 색상 선택 버튼 */}
                                        <button
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                setColorPickerFor((p) => (p === artwork.id ? null : artwork.id));
                                                setSelectedNode(artwork.id);
                                            }}
                                            className="w-5 h-5 rounded-full border-2 border-white/30 flex-shrink-0 shadow-md"
                                            style={{ backgroundColor: color }}
                                            title="색상 변경"
                                        />

                                        {/* 노드 표시 토글 */}
                                        <button
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                toggleVisible(artwork.id);
                                            }}
                                            className={`transition-colors ${
                                                isVisible ? "text-primary" : "text-slate-600 hover:text-slate-400"
                                            }`}
                                        >
                                            <span className="material-symbols-outlined" style={{ fontSize: "16px" }}>
                                                {isVisible ? "visibility" : "visibility_off"}
                                            </span>
                                        </button>
                                    </div>

                                    {/* 색상 피커 팝업 */}
                                    {colorPickerFor === artwork.id && (
                                        <div
                                            className="mt-2 flex gap-1.5 flex-wrap"
                                            onClick={(e) => e.stopPropagation()}
                                        >
                                            {NODE_COLORS.map((c) => (
                                                <button
                                                    key={c.value}
                                                    onClick={() => {
                                                        setNodeColors((p) => ({ ...p, [artwork.id]: c.value }));
                                                        setColorPickerFor(null);
                                                    }}
                                                    className="w-6 h-6 rounded-full border-2 transition-transform hover:scale-110"
                                                    style={{
                                                        backgroundColor: c.value,
                                                        borderColor:
                                                            color === c.value ? "white" : "transparent",
                                                    }}
                                                    title={c.label}
                                                />
                                            ))}
                                        </div>
                                    )}

                                    {/* 현재 좌표 표시 */}
                                    {isSelected && nodePositions[artwork.id] && (
                                        <p className="text-[10px] text-slate-500 mt-1.5">
                                            X: {percentToCoord(nodePositions[artwork.id].x)} / Z:{" "}
                                            {(-(nodePositions[artwork.id].y / 100 * 10 - 5)).toFixed(3)}
                                        </p>
                                    )}
                                </div>
                            );
                        })
                    )}
                </div>

                {/* 하단 안내 */}
                <div className="p-4 border-t border-slate-800">
                    <p className="text-[10px] text-slate-600 leading-relaxed">
                        노드를 드래그하여 위치를 조정하세요.
                        <br />
                        "위치 저장" 버튼으로 서버에 반영합니다.
                        <br />
                        <span className="text-amber-600/80">※ 저장은 관리자 인증 필요</span>
                    </p>
                </div>
            </div>
        </div>
    );
}

export default SpaceEditor;
