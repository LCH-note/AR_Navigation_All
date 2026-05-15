import React, { useState, useEffect } from "react";

// 경로 관리 페이지 — 전시품 선택으로 추천 경로 생성 및 관리
function SpaceRoute() {
    const [artworks, setArtworks] = useState([]);
    const [routes, setRoutes] = useState([]);
    const [loadingArtworks, setLoadingArtworks] = useState(true);
    const [loadingRoutes, setLoadingRoutes] = useState(true);
    const [saving, setSaving] = useState(false);

    // 경로 폼 상태
    const [form, setForm] = useState({
        routeId: "",
        routeName: "",
        destination: "",
        estimatedDistance: "",
        estimatedTime: "",
    });

    // 선택된 웨이포인트 목록 (전시품에서 선택 시 추가)
    const [waypoints, setWaypoints] = useState([]);

    // 좌측 패널 선택된 경로 상세
    const [selectedRoute, setSelectedRoute] = useState(null);
    const [deleting, setDeleting] = useState(null); // 삭제 중인 경로 id

    // 전시품 목록 로드 (GET /api/artworks)
    const fetchArtworks = async () => {
        try {
            const res = await fetch("/api/artworks");
            if (res.ok) {
                const result = await res.json();
                setArtworks(result.data || []);
            }
        } catch (err) {
            console.error("전시품 목록 로드 실패:", err);
        } finally {
            setLoadingArtworks(false);
        }
    };

    // 경로 목록 로드 (GET /api/routes — @RawResponse, .data 언래핑 불필요)
    const fetchRoutes = async () => {
        try {
            const res = await fetch("/api/routes");
            if (res.ok) {
                const data = await res.json();
                // @RawResponse이므로 순수 배열 반환
                setRoutes(Array.isArray(data) ? data : []);
            }
        } catch (err) {
            console.error("경로 목록 로드 실패:", err);
        } finally {
            setLoadingRoutes(false);
        }
    };

    useEffect(() => {
        fetchArtworks();
        fetchRoutes();
    }, []);

    // 전시품 클릭 → 웨이포인트 추가 (중복 방지)
    const handleAddWaypoint = (artwork) => {
        if (waypoints.some((w) => w.artworkId === artwork.id)) return;
        const wp = {
            artworkId: artwork.id,
            displayName: artwork.title,
            instruction: "",
            x: parseFloat(artwork.pos_x) || 0,
            y: 0,
            z: parseFloat(artwork.pos_z) || 0,
            image_url: artwork.image_url,
            floor_info: artwork.floor_info,
        };
        const newWaypoints = [...waypoints, wp];
        setWaypoints(newWaypoints);
        // 마지막 전시품을 목적지로 자동 설정
        setForm((p) => ({ ...p, destination: artwork.title }));
    };

    // 웨이포인트 순서 위로 이동
    const moveUp = (index) => {
        if (index === 0) return;
        const arr = [...waypoints];
        [arr[index - 1], arr[index]] = [arr[index], arr[index - 1]];
        setWaypoints(arr);
    };

    // 웨이포인트 순서 아래로 이동
    const moveDown = (index) => {
        if (index === waypoints.length - 1) return;
        const arr = [...waypoints];
        [arr[index], arr[index + 1]] = [arr[index + 1], arr[index]];
        setWaypoints(arr);
    };

    // 웨이포인트 제거
    const removeWaypoint = (artworkId) => {
        setWaypoints((p) => p.filter((w) => w.artworkId !== artworkId));
    };

    // 웨이포인트 안내 문구 수정
    const updateInstruction = (artworkId, instruction) => {
        setWaypoints((p) =>
            p.map((w) => (w.artworkId === artworkId ? { ...w, instruction } : w))
        );
    };

    // 경로 삭제 (DELETE /api/routes/:id)
    const handleDeleteRoute = async (id, name) => {
        if (!window.confirm(`"${name}" 경로를 삭제하시겠습니까?`)) return;
        setDeleting(id);
        try {
            const res = await fetch(`/api/routes/${id}`, { method: "DELETE" });
            if (res.ok) {
                if (selectedRoute?.id === id) setSelectedRoute(null);
                await fetchRoutes();
            } else {
                alert("경로 삭제에 실패했습니다.");
            }
        } catch (err) {
            console.error("경로 삭제 실패:", err);
            alert("서버 연결 오류");
        } finally {
            setDeleting(null);
        }
    };

    // 폼 초기화
    const resetForm = () => {
        setForm({ routeId: "", routeName: "", destination: "", estimatedDistance: "", estimatedTime: "" });
        setWaypoints([]);
    };

    // 경로 등록 (POST /api/routes)
    const handleSubmit = async () => {
        if (!form.routeId.trim() || !form.routeName.trim()) {
            alert("경로 ID와 경로 이름을 입력해주세요.");
            return;
        }
        if (waypoints.length === 0) {
            alert("최소 하나 이상의 전시품을 선택해주세요.");
            return;
        }
        setSaving(true);
        try {
            const body = {
                route_id: form.routeId,
                route_name: form.routeName,
                destination: form.destination,
                estimated_distance: form.estimatedDistance || "알 수 없음",
                estimated_time: form.estimatedTime || "알 수 없음",
                waypoints: waypoints.map((w) => ({
                    x: w.x,
                    y: w.y,
                    z: w.z,
                    displayName: w.displayName,
                    instruction: w.instruction,
                })),
            };
            const res = await fetch("/api/routes", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(body),
            });
            if (res.ok) {
                alert("경로가 등록되었습니다.");
                resetForm();
                await fetchRoutes();
            } else if (res.status === 401 || res.status === 403) {
                alert("관리자 인증이 필요합니다.\n현재 인증 기능 구현 전으로 경로 등록이 제한됩니다.");
            } else {
                const err = await res.json();
                alert(`등록 실패: ${err?.error?.message || "알 수 없는 오류"}`);
            }
        } catch (err) {
            console.error("경로 등록 실패:", err);
            alert("서버 연결 오류");
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="flex-1 flex overflow-hidden bg-[#101622]">
            {/* 좌측 패널: 등록된 경로 목록 */}
            <div className="w-72 flex-shrink-0 border-r border-slate-800 bg-[#151a25] flex flex-col">
                <div className="p-4 border-b border-slate-800">
                    <h3 className="text-sm font-bold text-white">등록된 경로</h3>
                    <p className="text-xs text-slate-500 mt-0.5">
                        {loadingRoutes ? "불러오는 중..." : `${routes.length}개`}
                    </p>
                </div>

                <div className="flex-1 overflow-y-auto p-3 space-y-2">
                    {loadingRoutes ? (
                        <div className="space-y-2">
                            {[1, 2].map((i) => (
                                <div key={i} className="h-20 bg-[#1e2430] rounded-lg animate-pulse" />
                            ))}
                        </div>
                    ) : routes.length === 0 ? (
                        <div className="flex flex-col items-center justify-center py-10 text-slate-600">
                            <span className="material-symbols-outlined text-3xl mb-2">route</span>
                            <p className="text-xs text-center">등록된 경로가 없습니다</p>
                        </div>
                    ) : (
                        routes.map((route, i) => (
                            <div
                                key={route.routeId || i}
                                onClick={() => setSelectedRoute(selectedRoute?.routeId === route.routeId ? null : route)}
                                className={`p-3 rounded-lg border cursor-pointer transition-all ${
                                    selectedRoute?.routeId === route.routeId
                                        ? "bg-primary/10 border-primary"
                                        : "bg-[#1e2430] border-slate-700 hover:border-slate-600"
                                }`}
                            >
                                <div className="flex items-start justify-between">
                                    <div className="flex-1 min-w-0">
                                        <p className="text-sm font-semibold text-white truncate">{route.routeName}</p>
                                        <p className="text-xs text-slate-400 mt-0.5 truncate">
                                            목적지: {route.destination || "-"}
                                        </p>
                                        <div className="flex items-center gap-2 mt-1">
                                            <span className="text-xs text-slate-500">
                                                {route.estimatedDistance}
                                            </span>
                                            <span className="text-slate-700">·</span>
                                            <span className="text-xs text-slate-500">
                                                {route.estimatedTime}
                                            </span>
                                        </div>
                                        <span className="text-[10px] text-slate-500">
                                            웨이포인트 {route.waypoints?.length || 0}개
                                        </span>
                                    </div>
                                    <button
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            handleDeleteRoute(route.id, route.routeName);
                                        }}
                                        disabled={deleting === route.id}
                                        className="text-slate-600 hover:text-rose-500 transition-colors ml-2 flex-shrink-0 disabled:opacity-40"
                                    >
                                        <span className="material-symbols-outlined" style={{ fontSize: "16px" }}>
                                            {deleting === route.id ? "hourglass_empty" : "delete"}
                                        </span>
                                    </button>
                                </div>
                            </div>
                        ))
                    )}
                </div>

                {/* 선택된 경로 웨이포인트 상세 */}
                {selectedRoute && (
                    <div className="border-t border-slate-800 p-3 max-h-48 overflow-y-auto">
                        <p className="text-xs font-semibold text-slate-400 mb-2">웨이포인트 순서</p>
                        {selectedRoute.waypoints?.map((wp, i) => (
                            <div key={i} className="flex items-center gap-2 py-1.5">
                                <span className="w-5 h-5 rounded-full bg-primary text-white text-[10px] flex items-center justify-center flex-shrink-0 font-bold">
                                    {i + 1}
                                </span>
                                <span className="text-xs text-slate-300 truncate">{wp.displayName}</span>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* 우측 영역: 새 경로 만들기 */}
            <div className="flex-1 flex flex-col overflow-hidden">
                {/* 헤더 */}
                <div className="flex-shrink-0 px-6 py-4 border-b border-slate-800 flex items-center justify-between">
                    <div>
                        <h2 className="text-base font-bold text-white">새 경로 만들기</h2>
                        <p className="text-xs text-slate-400 mt-0.5">전시품을 선택하여 추천 경로를 구성합니다</p>
                    </div>
                    <div className="flex gap-2">
                        <button
                            onClick={resetForm}
                            className="px-3 py-1.5 text-xs text-slate-400 border border-slate-700 rounded-lg hover:bg-slate-800 transition-colors"
                        >
                            초기화
                        </button>
                        <button
                            onClick={handleSubmit}
                            disabled={saving}
                            className="flex items-center gap-1.5 px-4 py-1.5 bg-primary hover:bg-blue-600 text-white text-xs font-bold rounded-lg shadow-md transition-all disabled:opacity-50"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "16px" }}>
                                upload
                            </span>
                            {saving ? "등록 중..." : "경로 등록하기"}
                        </button>
                    </div>
                </div>

                <div className="flex-1 overflow-y-auto p-6 space-y-6">
                    {/* 경로 기본 정보 폼 */}
                    <section>
                        <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center gap-2">
                            <span className="material-symbols-outlined" style={{ fontSize: "14px" }}>
                                info
                            </span>
                            경로 기본 정보
                        </h3>
                        <div className="grid grid-cols-2 gap-4">
                            <div>
                                <label className="block text-xs font-medium text-slate-400 mb-1">
                                    경로 ID *
                                </label>
                                <input
                                    type="text"
                                    value={form.routeId}
                                    onChange={(e) => setForm((p) => ({ ...p, routeId: e.target.value }))}
                                    placeholder="예: route_all_stops"
                                    className="w-full bg-[#151a25] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-primary"
                                />
                            </div>
                            <div>
                                <label className="block text-xs font-medium text-slate-400 mb-1">
                                    경로 이름 *
                                </label>
                                <input
                                    type="text"
                                    value={form.routeName}
                                    onChange={(e) => setForm((p) => ({ ...p, routeName: e.target.value }))}
                                    placeholder="예: 전체 순회 (A→B→C)"
                                    className="w-full bg-[#151a25] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-primary"
                                />
                            </div>
                            <div>
                                <label className="block text-xs font-medium text-slate-400 mb-1">
                                    목적지
                                </label>
                                <input
                                    type="text"
                                    value={form.destination}
                                    onChange={(e) => setForm((p) => ({ ...p, destination: e.target.value }))}
                                    placeholder="예: 전시품 C"
                                    className="w-full bg-[#151a25] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-primary"
                                />
                            </div>
                            <div className="grid grid-cols-2 gap-2">
                                <div>
                                    <label className="block text-xs font-medium text-slate-400 mb-1">
                                        예상 거리
                                    </label>
                                    <input
                                        type="text"
                                        value={form.estimatedDistance}
                                        onChange={(e) => setForm((p) => ({ ...p, estimatedDistance: e.target.value }))}
                                        placeholder="약 5m"
                                        className="w-full bg-[#151a25] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-primary"
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs font-medium text-slate-400 mb-1">
                                        예상 시간
                                    </label>
                                    <input
                                        type="text"
                                        value={form.estimatedTime}
                                        onChange={(e) => setForm((p) => ({ ...p, estimatedTime: e.target.value }))}
                                        placeholder="약 3분"
                                        className="w-full bg-[#151a25] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-primary"
                                    />
                                </div>
                            </div>
                        </div>
                    </section>

                    {/* 전시품 선택 + 웨이포인트 구성 */}
                    <section>
                        <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3 flex items-center gap-2">
                            <span className="material-symbols-outlined" style={{ fontSize: "14px" }}>
                                route
                            </span>
                            웨이포인트 구성{" "}
                            <span className="text-primary normal-case font-normal">
                                ({waypoints.length}개 선택됨)
                            </span>
                        </h3>

                        <div className="grid grid-cols-2 gap-4">
                            {/* 전시품 선택 패널 */}
                            <div className="bg-[#151a25] border border-slate-700 rounded-xl overflow-hidden">
                                <div className="px-4 py-3 border-b border-slate-700">
                                    <p className="text-xs font-semibold text-white">전시품 목록</p>
                                    <p className="text-[10px] text-slate-500 mt-0.5">클릭하여 경로에 추가</p>
                                </div>
                                <div className="overflow-y-auto max-h-64 p-2 space-y-1">
                                    {loadingArtworks ? (
                                        <p className="text-xs text-slate-500 p-4 text-center">불러오는 중...</p>
                                    ) : artworks.length === 0 ? (
                                        <p className="text-xs text-slate-500 p-4 text-center">
                                            등록된 전시품이 없습니다
                                        </p>
                                    ) : (
                                        artworks.map((artwork) => {
                                            const selected = waypoints.some((w) => w.artworkId === artwork.id);
                                            return (
                                                <div
                                                    key={artwork.id}
                                                    onClick={() => handleAddWaypoint(artwork)}
                                                    className={`flex items-center gap-3 p-2 rounded-lg cursor-pointer transition-all ${
                                                        selected
                                                            ? "bg-primary/10 border border-primary/30 text-primary"
                                                            : "hover:bg-[#1e2430] text-slate-300"
                                                    }`}
                                                >
                                                    <div className="w-8 h-8 rounded bg-[#111318] overflow-hidden flex-shrink-0">
                                                        <img
                                                            src={artwork.image_url || "/images/nophoto.png"}
                                                            alt={artwork.title}
                                                            className="w-full h-full object-cover opacity-80"
                                                        />
                                                    </div>
                                                    <div className="flex-1 min-w-0">
                                                        <p className="text-xs font-medium truncate">{artwork.title}</p>
                                                        <p className="text-[10px] text-slate-500">{artwork.floor_info || "-"}</p>
                                                    </div>
                                                    {selected && (
                                                        <span className="material-symbols-outlined text-primary" style={{ fontSize: "16px" }}>
                                                            check_circle
                                                        </span>
                                                    )}
                                                </div>
                                            );
                                        })
                                    )}
                                </div>
                            </div>

                            {/* 웨이포인트 순서 패널 */}
                            <div className="bg-[#151a25] border border-slate-700 rounded-xl overflow-hidden">
                                <div className="px-4 py-3 border-b border-slate-700">
                                    <p className="text-xs font-semibold text-white">경로 순서</p>
                                    <p className="text-[10px] text-slate-500 mt-0.5">순서 변경 및 안내 문구 설정</p>
                                </div>
                                <div className="overflow-y-auto max-h-64 p-2 space-y-2">
                                    {waypoints.length === 0 ? (
                                        <div className="flex flex-col items-center justify-center py-8 text-slate-600">
                                            <span className="material-symbols-outlined text-2xl mb-1">
                                                arrow_back
                                            </span>
                                            <p className="text-xs text-center">좌측에서 전시품을 선택하세요</p>
                                        </div>
                                    ) : (
                                        waypoints.map((wp, index) => (
                                            <div
                                                key={wp.artworkId}
                                                className="bg-[#111318] border border-slate-700 rounded-lg p-2"
                                            >
                                                <div className="flex items-center gap-2 mb-1.5">
                                                    {/* 순서 번호 */}
                                                    <span className="w-5 h-5 rounded-full bg-primary text-white text-[10px] flex items-center justify-center flex-shrink-0 font-bold">
                                                        {index + 1}
                                                    </span>
                                                    <span className="text-xs font-medium text-white flex-1 truncate">
                                                        {wp.displayName}
                                                    </span>
                                                    {/* 순서 이동 버튼 */}
                                                    <button
                                                        onClick={() => moveUp(index)}
                                                        disabled={index === 0}
                                                        className="text-slate-500 hover:text-white disabled:opacity-30 transition-colors"
                                                    >
                                                        <span className="material-symbols-outlined" style={{ fontSize: "14px" }}>
                                                            keyboard_arrow_up
                                                        </span>
                                                    </button>
                                                    <button
                                                        onClick={() => moveDown(index)}
                                                        disabled={index === waypoints.length - 1}
                                                        className="text-slate-500 hover:text-white disabled:opacity-30 transition-colors"
                                                    >
                                                        <span className="material-symbols-outlined" style={{ fontSize: "14px" }}>
                                                            keyboard_arrow_down
                                                        </span>
                                                    </button>
                                                    {/* 제거 버튼 */}
                                                    <button
                                                        onClick={() => removeWaypoint(wp.artworkId)}
                                                        className="text-slate-500 hover:text-rose-500 transition-colors"
                                                    >
                                                        <span className="material-symbols-outlined" style={{ fontSize: "14px" }}>
                                                            close
                                                        </span>
                                                    </button>
                                                </div>
                                                {/* 안내 문구 입력 */}
                                                <input
                                                    type="text"
                                                    value={wp.instruction}
                                                    onChange={(e) => updateInstruction(wp.artworkId, e.target.value)}
                                                    placeholder="도달 시 안내 문구"
                                                    className="w-full bg-[#1e2430] border border-slate-700 rounded px-2 py-1 text-[10px] text-slate-300 placeholder-slate-600 focus:outline-none focus:border-primary"
                                                />
                                                <p className="text-[10px] text-slate-600 mt-1">
                                                    X: {wp.x.toFixed(3)} / Z: {wp.z.toFixed(3)}
                                                </p>
                                            </div>
                                        ))
                                    )}
                                </div>
                            </div>
                        </div>
                    </section>
                </div>
            </div>
        </div>
    );
}

export default SpaceRoute;
