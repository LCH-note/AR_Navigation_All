import React, { useState, useEffect, useRef } from "react";

// 맵 타입 레이블 및 뱃지 색상 매핑
const MAP_TYPE_LABELS = {
    floor_plan: "2D 평면도",
    immersal_map: "3D 임머셜 맵",
    immersal_map_b: "3D 임머셜 맵 B",
    "3d_model": "3D 전체도",
};

const MAP_TYPE_BADGE = {
    floor_plan: "bg-green-900/30 text-green-400 border border-green-800/50",
    immersal_map: "bg-blue-900/30 text-blue-400 border border-blue-800/50",
    immersal_map_b: "bg-violet-900/30 text-violet-400 border border-violet-800/50",
    "3d_model": "bg-orange-900/30 text-orange-400 border border-orange-800/50",
};

// 맵 파일 관리 페이지 — 2D 평면도 및 3D 임머셜 맵 파일 추가/삭제
function SpaceMap() {
    const [maps, setMaps] = useState([]);
    const [loading, setLoading] = useState(true);
    // 필터: 전체 / floor_plan / immersal_map
    const [filterType, setFilterType] = useState("all");
    const [showModal, setShowModal] = useState(false);
    // 모달 폼 상태
    const [form, setForm] = useState({ name: "", map_type: "floor_plan", floor: "" });
    const [selectedFile, setSelectedFile] = useState(null);
    const [saving, setSaving] = useState(false);
    const fileInputRef = useRef(null);

    // 맵 목록 로드
    const fetchMaps = async () => {
        setLoading(true);
        try {
            const res = await fetch("/api/maps");
            if (res.ok) {
                const result = await res.json();
                setMaps(result.data || []);
            }
        } catch (err) {
            console.error("맵 목록 로드 실패:", err);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchMaps();
    }, []);

    // 맵 삭제 처리
    const handleDelete = async (id, name) => {
        if (!window.confirm(`"${name}" 맵을 삭제하시겠습니까?`)) return;
        try {
            const res = await fetch(`/api/maps/${id}`, { method: "DELETE" });
            if (res.ok) {
                await fetchMaps();
            } else {
                alert("삭제에 실패했습니다.");
            }
        } catch (err) {
            console.error("맵 삭제 실패:", err);
            alert("서버 연결 오류");
        }
    };

    // 새 맵 등록: 메타데이터 생성 → 파일 업로드 순서
    const handleSubmit = async () => {
        if (!form.name.trim()) {
            alert("맵 이름을 입력해주세요.");
            return;
        }
        setSaving(true);
        try {
            // 1단계: 맵 메타데이터 생성
            const createRes = await fetch("/api/maps", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    name: form.name,
                    map_type: form.map_type,
                    ...(form.floor ? { floor: form.floor } : {}),
                }),
            });
            if (!createRes.ok) {
                alert("맵 생성에 실패했습니다.");
                return;
            }
            const createResult = await createRes.json();
            const newMapId = createResult.data?.id;

            // 2단계: 파일이 있으면 업로드
            if (selectedFile && newMapId) {
                const fileData = new FormData();
                fileData.append("file", selectedFile);
                const uploadRes = await fetch(`/api/maps/${newMapId}/upload`, {
                    method: "POST",
                    body: fileData,
                });
                if (!uploadRes.ok) {
                    alert("맵은 생성됐으나 파일 업로드에 실패했습니다.");
                }
            }

            // 성공 후 초기화
            setShowModal(false);
            setForm({ name: "", map_type: "floor_plan", floor: "" });
            setSelectedFile(null);
            await fetchMaps();
        } catch (err) {
            console.error("맵 등록 실패:", err);
            alert("서버 연결 오류");
        } finally {
            setSaving(false);
        }
    };

    // 파일 선택 처리
    const handleFileChange = (e) => {
        const file = e.target.files[0];
        if (file) setSelectedFile(file);
    };

    // 필터링된 맵 목록
    const filteredMaps = filterType === "all" ? maps : maps.filter((m) => m.map_type === filterType);

    const formatDate = (dateStr) => {
        if (!dateStr) return "-";
        return new Date(dateStr).toLocaleDateString("ko-KR", { year: "numeric", month: "short", day: "numeric" });
    };

    return (
        <div className="flex-1 overflow-y-auto bg-[#101622]">
            {/* 상단 헤더 */}
            <div className="sticky top-0 z-10 bg-[#101622]/95 backdrop-blur border-b border-slate-800 px-8 py-4 flex items-center justify-between">
                <div>
                    <h2 className="text-lg font-bold text-white">맵 파일 관리</h2>
                    <p className="text-xs text-slate-400 mt-0.5">2D 평면도 및 3D 임머셜 맵 파일을 관리합니다</p>
                </div>
                <button
                    onClick={() => setShowModal(true)}
                    className="flex items-center gap-2 px-4 py-2 bg-primary hover:bg-blue-600 text-white text-sm font-bold rounded-lg shadow-lg shadow-blue-500/20 transition-all active:scale-95"
                >
                    <span className="material-symbols-outlined" style={{ fontSize: "18px" }}>
                        add
                    </span>
                    새 맵 추가
                </button>
            </div>

            <div className="px-8 py-6 space-y-6">
                {/* 타입 필터 탭 */}
                <div className="flex items-center gap-2">
                    {[
                        { key: "all", label: "전체", count: maps.length },
                        { key: "floor_plan", label: "2D 평면도", count: maps.filter((m) => m.map_type === "floor_plan").length },
                        {
                            key: "immersal_map",
                            label: "3D 임머셜 맵",
                            count: maps.filter((m) => m.map_type !== "floor_plan").length,
                        },
                    ].map((tab) => (
                        <button
                            key={tab.key}
                            onClick={() => setFilterType(tab.key)}
                            className={`flex items-center gap-1.5 px-4 py-1.5 rounded-full text-sm font-medium transition-all ${
                                filterType === tab.key
                                    ? "bg-primary text-white shadow-md shadow-blue-500/20"
                                    : "bg-[#1e2430] text-slate-400 hover:text-white border border-slate-700"
                            }`}
                        >
                            {tab.label}
                            <span
                                className={`text-xs px-1.5 py-0.5 rounded-full ${
                                    filterType === tab.key ? "bg-white/20 text-white" : "bg-slate-700 text-slate-300"
                                }`}
                            >
                                {tab.count}
                            </span>
                        </button>
                    ))}
                </div>

                {/* 맵 카드 그리드 */}
                {loading ? (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                        {[1, 2, 3].map((i) => (
                            <div key={i} className="h-40 bg-[#1e2430] rounded-xl animate-pulse border border-slate-700" />
                        ))}
                    </div>
                ) : filteredMaps.length === 0 ? (
                    <div className="flex flex-col items-center justify-center py-20 text-slate-500">
                        <span className="material-symbols-outlined text-5xl mb-3">layers</span>
                        <p className="text-sm">등록된 맵이 없습니다</p>
                        <button
                            onClick={() => setShowModal(true)}
                            className="mt-4 text-primary hover:underline text-sm"
                        >
                            첫 번째 맵을 추가해보세요
                        </button>
                    </div>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                        {filteredMaps.map((map) => (
                            <MapCard
                                key={map.id}
                                map={map}
                                onDelete={handleDelete}
                                formatDate={formatDate}
                            />
                        ))}
                    </div>
                )}
            </div>

            {/* 새 맵 추가 모달 */}
            {showModal && (
                <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
                    <div className="bg-[#1e2430] border border-slate-700 rounded-2xl p-6 w-full max-w-md shadow-2xl">
                        <div className="flex items-center justify-between mb-5">
                            <h3 className="text-base font-bold text-white">새 맵 추가</h3>
                            <button
                                onClick={() => {
                                    setShowModal(false);
                                    setForm({ name: "", map_type: "floor_plan", floor: "" });
                                    setSelectedFile(null);
                                }}
                                className="text-slate-400 hover:text-white transition-colors"
                            >
                                <span className="material-symbols-outlined">close</span>
                            </button>
                        </div>

                        <div className="space-y-4">
                            {/* 맵 이름 */}
                            <div>
                                <label className="block text-xs font-medium text-slate-400 mb-1">맵 이름 *</label>
                                <input
                                    type="text"
                                    value={form.name}
                                    onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
                                    placeholder="예: Museum 1F 평면도"
                                    className="w-full bg-[#111318] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-primary"
                                />
                            </div>

                            {/* 맵 타입 */}
                            <div>
                                <label className="block text-xs font-medium text-slate-400 mb-1">맵 타입</label>
                                <select
                                    value={form.map_type}
                                    onChange={(e) => setForm((p) => ({ ...p, map_type: e.target.value }))}
                                    className="w-full bg-[#111318] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-primary appearance-none"
                                >
                                    <option value="floor_plan">2D 평면도</option>
                                    <option value="immersal_map">3D 임머셜 맵 (Primary)</option>
                                    <option value="immersal_map_b">3D 임머셜 맵 (Secondary)</option>
                                    <option value="3d_model">3D 전체도 (.fbx → .glb)</option>
                                </select>
                            </div>

                            {/* 플로어 — floor_plan / 3d_model 타입일 때 표시 */}
                            {(form.map_type === "floor_plan" || form.map_type === "3d_model") && (
                                <div>
                                    <label className="block text-xs font-medium text-slate-400 mb-1">
                                        플로어{" "}
                                        <span className="text-slate-600 font-normal">
                                            (앱 전체지도 화면에서 해당 층에 표시)
                                        </span>
                                    </label>
                                    <select
                                        value={form.floor}
                                        onChange={(e) => setForm((p) => ({ ...p, floor: e.target.value }))}
                                        className="w-full bg-[#111318] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-primary appearance-none"
                                    >
                                        <option value="">미지정</option>
                                        <option value="B1">B1</option>
                                        <option value="1F">1F</option>
                                        <option value="2F">2F</option>
                                        <option value="3F">3F</option>
                                    </select>
                                </div>
                            )}

                            {/* 파일 업로드 */}
                            <div>
                                <label className="block text-xs font-medium text-slate-400 mb-1">파일 업로드 (선택)</label>
                                {form.map_type === "3d_model" && (
                                    <p className="text-xs text-amber-400 mb-2">
                                        ⚠ 앱에서 로드하려면 <strong>.glb</strong> 형식이 필요합니다. FBX는 Blender에서 GLB로 변환 후 업로드하세요.
                                    </p>
                                )}
                                <div
                                    onClick={() => fileInputRef.current?.click()}
                                    className="border-2 border-dashed border-slate-600 hover:border-primary/50 rounded-xl p-6 text-center cursor-pointer transition-colors group"
                                >
                                    <input
                                        ref={fileInputRef}
                                        type="file"
                                        className="hidden"
                                        onChange={handleFileChange}
                                    />
                                    {selectedFile ? (
                                        <div className="flex flex-col items-center gap-1">
                                            <span className="material-symbols-outlined text-primary text-3xl">
                                                check_circle
                                            </span>
                                            <p className="text-sm text-white font-medium">{selectedFile.name}</p>
                                            <p className="text-xs text-slate-500">
                                                {(selectedFile.size / 1024 / 1024).toFixed(2)} MB
                                            </p>
                                        </div>
                                    ) : (
                                        <div className="flex flex-col items-center gap-1">
                                            <span className="material-symbols-outlined text-slate-500 group-hover:text-primary text-3xl transition-colors">
                                                cloud_upload
                                            </span>
                                            <p className="text-sm text-slate-400">클릭하여 파일 선택</p>
                                            <p className="text-xs text-slate-600">
                                                {form.map_type === "3d_model" ? ".glb, .gltf, .fbx" : ".bytes, .png, .jpg 등"}
                                            </p>
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>

                        {/* 버튼 */}
                        <div className="flex gap-3 mt-6">
                            <button
                                onClick={() => {
                                    setShowModal(false);
                                    setForm({ name: "", map_type: "floor_plan", floor: "" });
                                    setSelectedFile(null);
                                }}
                                className="flex-1 py-2.5 text-sm font-medium text-slate-400 border border-slate-700 rounded-lg hover:bg-slate-800 transition-colors"
                            >
                                취소
                            </button>
                            <button
                                onClick={handleSubmit}
                                disabled={saving}
                                className="flex-[2] py-2.5 text-sm font-bold text-white bg-primary hover:bg-blue-600 rounded-lg shadow-lg shadow-blue-500/20 transition-colors disabled:opacity-50"
                            >
                                {saving ? "등록 중..." : "등록하기"}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

// 개별 맵 카드 컴포넌트
function MapCard({ map, onDelete, formatDate }) {
    const [expanded, setExpanded] = useState(false);

    const handleCopyUrl = () => {
        if (map.file_url) {
            navigator.clipboard.writeText(map.file_url);
            alert("URL이 클립보드에 복사되었습니다.");
        }
    };

    return (
        <div className="bg-[#1e2430] border border-slate-700 rounded-xl hover:border-slate-600 transition-all overflow-hidden">
            {/* 카드 헤더 */}
            <div
                className="p-4 flex items-start gap-3 cursor-pointer"
                onClick={() => setExpanded((p) => !p)}
            >
                {/* 파일 아이콘 */}
                <div className="w-10 h-10 rounded-lg bg-[#111318] border border-slate-700 flex items-center justify-center flex-shrink-0">
                    <span className="material-symbols-outlined text-slate-400" style={{ fontSize: "20px" }}>
                        {map.map_type === "floor_plan" ? "image" : map.map_type === "3d_model" ? "deployed_code" : "view_in_ar"}
                    </span>
                </div>

                <div className="flex-1 min-w-0">
                    {/* 맵 이름 */}
                    <h3 className="text-sm font-semibold text-white truncate">{map.name}</h3>
                    <div className="flex items-center gap-2 mt-1 flex-wrap">
                        {/* 타입 뱃지 */}
                        <span className={`text-[10px] px-2 py-0.5 rounded-full font-medium ${MAP_TYPE_BADGE[map.map_type] || "bg-slate-700 text-slate-300"}`}>
                            {MAP_TYPE_LABELS[map.map_type] || map.map_type}
                        </span>
                        {/* 플로어 뱃지 — floor_plan / 3d_model 타입이고 floor 값이 있을 때 표시 */}
                        {(map.map_type === "floor_plan" || map.map_type === "3d_model") && map.floor && (
                            <span className="text-[10px] px-2 py-0.5 rounded-full font-bold bg-amber-900/30 text-amber-400 border border-amber-800/50">
                                {map.floor}
                            </span>
                        )}
                        {/* 플로어 미지정 안내 */}
                        {(map.map_type === "floor_plan" || map.map_type === "3d_model") && !map.floor && (
                            <span className="text-[10px] px-2 py-0.5 rounded-full text-slate-600 bg-slate-800">
                                플로어 미지정
                            </span>
                        )}
                    </div>
                    {/* 업로드 날짜 */}
                    <p className="text-xs text-slate-500 mt-1">{formatDate(map.updated_at || map.created_at)}</p>
                </div>

                {/* 삭제 버튼 */}
                <button
                    onClick={(e) => {
                        e.stopPropagation();
                        onDelete(map.id, map.name);
                    }}
                    className="text-slate-500 hover:text-rose-500 transition-colors"
                >
                    <span className="material-symbols-outlined" style={{ fontSize: "18px" }}>
                        delete
                    </span>
                </button>
            </div>

            {/* 확장 영역 — 파일 URL 표시 */}
            {expanded && (
                <div className="px-4 pb-4 border-t border-slate-700/50 pt-3">
                    {map.file_url ? (
                        <div className="space-y-2">
                            <p className="text-xs text-slate-400 font-medium">파일 URL</p>
                            <p className="text-xs text-slate-500 bg-[#111318] rounded-lg px-3 py-2 break-all leading-relaxed">
                                {map.file_url}
                            </p>
                            <div className="flex gap-2">
                                <button
                                    onClick={handleCopyUrl}
                                    className="flex items-center gap-1.5 text-xs text-primary hover:text-blue-400 transition-colors"
                                >
                                    <span className="material-symbols-outlined" style={{ fontSize: "14px" }}>
                                        content_copy
                                    </span>
                                    URL 복사
                                </button>
                                <a
                                    href={map.file_url}
                                    target="_blank"
                                    rel="noreferrer"
                                    className="flex items-center gap-1.5 text-xs text-slate-400 hover:text-white transition-colors"
                                >
                                    <span className="material-symbols-outlined" style={{ fontSize: "14px" }}>
                                        open_in_new
                                    </span>
                                    새 탭에서 열기
                                </a>
                            </div>
                        </div>
                    ) : (
                        <p className="text-xs text-slate-500">업로드된 파일 없음</p>
                    )}
                </div>
            )}
        </div>
    );
}

export default SpaceMap;
