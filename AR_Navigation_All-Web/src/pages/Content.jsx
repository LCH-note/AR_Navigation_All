import React, { useState, useRef, useEffect } from "react";
import { Link } from "react-router-dom";

const serverHost = "http://localhost:3000";
const apiUrl = `${serverHost}/api/display`;

function Content() {
    // [수정] 위치 정보 필드(ar_marker_id, pos_x, pos_y, floor_info) 추가
    const [formData, setFormData] = useState({
        title: "",
        feature: "",
        contents: "",
        ar_marker_id: "",
        pos_x: "",
        pos_z: "",
        floor_info: "Museum 1F",
    });

    const [previewImage, setPreviewImage] = useState("/images/nophoto.png");
    const [selectedFile, setSelectedFile] = useState(null);
    const [selectedItemId, setSelectedItemId] = useState(null);
    const fileInputRef = useRef(null);
    const [items, setItems] = useState([]);

    const fetchItems = async () => {
        try {
            const response = await fetch(apiUrl);
            if (response.ok) {
                const data = await response.json();
                setItems(data);
            }
        } catch (error) {
            console.error("목록 불러오기 실패:", error);
        }
    };

    useEffect(() => {
        fetchItems();
    }, []);

    // [수정] 선택 시 위치 정보 데이터도 폼에 채워지도록 수정
    const handleSelectItem = (item) => {
        setSelectedItemId(item.id);
        setFormData({
            title: item.title || "",
            feature: item.feature || "",
            contents: item.contents || "",
            ar_marker_id: item.ar_marker_id || "",
            pos_x: item.pos_x || "",
            pos_z: item.pos_z || "",
            floor_info: item.floor_info || "Museum 1F",
        });

        if (item.image_path) {
            setPreviewImage(`${serverHost}${item.image_path}`);
        } else {
            setPreviewImage("/images/nophoto.png");
        }
        setSelectedFile(null);
    };

    const handleDelete = async (id, e) => {
        e.stopPropagation();
        if (window.confirm("정말 삭제하시겠습니까?")) {
            try {
                const response = await fetch(`${apiUrl}/${id}`, {
                    method: "DELETE",
                });
                if (response.ok) {
                    alert("삭제되었습니다.");
                    fetchItems();
                    if (selectedItemId === id) {
                        resetForm();
                    }
                }
            } catch (error) {
                console.error("삭제 중 오류:", error);
            }
        }
    };

    // [추가] 폼 초기화 공통 함수
    const resetForm = () => {
        setFormData({
            title: "",
            feature: "",
            contents: "",
            ar_marker_id: "",
            pos_x: "",
            pos_z: "",
            floor_info: "Museum 1F",
        });
        setPreviewImage("/images/nophoto.png");
        setSelectedItemId(null);
    };

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData((prev) => ({
            ...prev,
            [name]: value,
        }));
    };

    const handleImageChange = (e) => {
        const file = e.target.files[0];
        if (file) {
            const imageUrl = URL.createObjectURL(file);
            setPreviewImage(imageUrl);
            setSelectedFile(file);
        }
    };

    const handleTriggerUpload = () => {
        fileInputRef.current.click();
    };

    const handleSave = async () => {
        const data = new FormData();
        // [수정] 모든 필드를 FormData에 추가
        Object.keys(formData).forEach((key) => {
            data.append(key, formData[key]);
        });

        if (selectedFile) {
            data.append("image", selectedFile);
        }

        try {
            const response = await fetch(apiUrl, {
                method: "POST",
                body: data,
            });

            if (response.ok) {
                alert("성공적으로 저장되었습니다!");
                fetchItems();
                resetForm(); // 모든 필드 리셋
            } else {
                alert("저장 실패");
            }
        } catch (error) {
            console.error("에러 발생:", error);
            alert("서버 연결 오류");
        }
    };

    const formatDate = (dateString) => {
        if (!dateString) return "";
        const date = new Date(dateString);
        return date.toLocaleString("ko-KR", { month: "long", day: "numeric", hour: "2-digit", minute: "2-digit" });
    };

    return (
        <div className="flex h-screen w-full bg-[#101622] font-display text-white overflow-hidden antialiased">
            <nav className="w-64 border-r border-slate-800 bg-[#101622] flex-shrink-0 flex flex-col justify-between p-4 hidden md:flex z-20">
                <div className="flex flex-col gap-6">
                    <div className="flex items-center px-2">
                        <img src="/images/arlogo.jpg" alt="로고" className="w-12 h-12 rounded-lg object-cover" />
                        <div className="ml-3 flex flex-col">
                            <h1 className="text-xl font-bold leading-tight text-white">AR System</h1>
                            <p className="text-slate-400 text-sm">관리자 모드</p>
                        </div>
                    </div>
                    <div className="flex flex-col gap-2">
                        <Link
                            to="/"
                            className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-300 hover:bg-slate-800 transition-colors"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "24px" }}>
                                dashboard
                            </span>
                            <span className="text-sm font-medium">홈</span>
                        </Link>
                        <Link
                            to="/space"
                            className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-300 hover:bg-slate-800 transition-colors"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "24px" }}>
                                map
                            </span>
                            <span className="text-sm font-medium">공간 관리</span>
                        </Link>
                        <Link
                            to="/content"
                            className="flex items-center gap-3 px-3 py-2 rounded-lg bg-primary/10 text-primary border-l-4 border-primary transition-colors"
                        >
                            <span className="material-symbols-outlined icon-filled" style={{ fontSize: "24px" }}>
                                inventory_2
                            </span>
                            <span className="text-sm font-medium">전시콘텐츠 관리</span>
                        </Link>
                        <Link
                            to="/user"
                            className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-300 hover:bg-slate-800 transition-colors"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "24px" }}>
                                rate_review
                            </span>
                            <span className="text-sm font-medium">사용자 리뷰 관리</span>
                        </Link>
                    </div>
                </div>

                {/* 사이드바 하단 실시간 시계 컴포넌트 마운트 */}
                <SidebarClock />
            </nav>

            <div className="w-80 border-r border-slate-800 bg-[#151a25] flex flex-col flex-shrink-0 z-10">
                <div className="p-5 border-b border-slate-800 bg-[#151a25]">
                    <h2 className="text-xl font-bold mb-4 text-white">전시 품목 ({items.length})</h2>
                </div>

                <div className="flex-1 overflow-y-auto p-3 space-y-1 bg-[#151a25]">
                    {items.map((item) => (
                        <div
                            key={item.id}
                            onClick={() => handleSelectItem(item)}
                            className={`group flex items-start gap-3 p-3 rounded-lg border transition-all cursor-pointer relative shadow-sm ${selectedItemId === item.id ? "bg-primary/10 border-primary ring-1 ring-primary" : "bg-[#111318] border-slate-800 hover:border-primary/50"}`}
                        >
                            <div className="w-12 h-12 rounded bg-[#101622] flex items-center justify-center flex-shrink-0 overflow-hidden relative">
                                <img
                                    className="w-full h-full object-cover opacity-80"
                                    alt="thumbnail"
                                    src={
                                        item.image_path
                                            ? `${serverHost}${item.image_path}`
                                            : "https://via.placeholder.com/150"
                                    }
                                />
                                <div className="absolute inset-0 bg-black/10"></div>
                            </div>
                            <div className="flex-1 min-w-0">
                                <div className="flex justify-between items-start">
                                    <h3
                                        className={`text-sm font-bold truncate transition-colors ${selectedItemId === item.id ? "text-primary" : "text-slate-200"}`}
                                    >
                                        {item.title}
                                    </h3>
                                    <button
                                        onClick={(e) => handleDelete(item.id, e)}
                                        className="text-slate-500 hover:text-red-500 transition-colors"
                                    >
                                        <span className="material-symbols-outlined" style={{ fontSize: "18px" }}>
                                            delete
                                        </span>
                                    </button>
                                </div>
                                <p className="text-xs text-slate-400 mt-0.5">
                                    {item.created_at ? formatDate(item.created_at) : "날짜 없음"}
                                </p>
                            </div>
                        </div>
                    ))}
                </div>
            </div>

            <main className="flex-1 flex flex-col h-full bg-[#101622] overflow-y-auto relative">
                <header className="h-16 border-b border-slate-800 flex items-center justify-end px-8 bg-[#101622]/90 backdrop-blur sticky top-0 z-10">
                    <div className="flex items-center gap-3">
                        <button
                            onClick={resetForm}
                            className="p-2 rounded-lg text-slate-400 hover:bg-white/5 hover:text-white transition-colors"
                        >
                            <span className="material-symbols-outlined">add</span>
                        </button>
                        <div className="h-6 w-px bg-slate-700 mx-1"></div>
                        <button
                            onClick={handleSave}
                            className="px-4 py-2 rounded-lg bg-primary hover:bg-blue-600 text-white text-sm font-bold shadow-md transition-all active:scale-[0.98]"
                        >
                            {selectedItemId ? "수정하기" : "등록하기"}
                        </button>
                    </div>
                </header>

                <div className="p-8 pb-32 max-w-6xl mx-auto w-full flex flex-col gap-8">
                    <section className="flex flex-col gap-4">
                        <div
                            className="relative w-full aspect-video bg-gradient-to-br from-slate-900 to-black rounded-xl overflow-hidden shadow-2xl border border-slate-800 group cursor-pointer"
                            onClick={handleTriggerUpload}
                        >
                            <img
                                className="absolute inset-0 w-full h-full object-cover"
                                alt="Preview"
                                src={previewImage}
                            />
                            <input
                                type="file"
                                ref={fileInputRef}
                                onChange={handleImageChange}
                                className="hidden"
                                accept="image/*"
                            />
                            <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
                                <div className="size-16 rounded-full bg-white/10 backdrop-blur-sm flex items-center justify-center mb-3 group-hover:scale-110 transition-transform">
                                    <span className="material-symbols-outlined text-white text-3xl">cloud_upload</span>
                                </div>
                                <span className="text-white font-medium text-sm bg-black/50 px-3 py-1 rounded-full backdrop-blur-md">
                                    이미지 변경
                                </span>
                            </div>
                        </div>
                    </section>

                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                        <div className="lg:col-span-1 flex flex-col gap-6">
                            <h3 className="text-base font-semibold text-slate-200 border-b border-slate-800 pb-2">
                                위치
                            </h3>
                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-slate-400 mb-1">
                                        AR Marker ID
                                    </label>
                                    <input
                                        name="ar_marker_id"
                                        value={formData.ar_marker_id}
                                        onChange={handleChange}
                                        className="w-full bg-[#151a25] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white"
                                        type="text"
                                        placeholder="ID"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-400 mb-1">X</label>
                                    <input
                                        name="pos_x"
                                        value={formData.pos_x}
                                        onChange={handleChange}
                                        className="w-full bg-[#151a25] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white"
                                        type="text"
                                        placeholder="X축"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-400 mb-1">Z</label>
                                    <input
                                        name="pos_z"
                                        value={formData.pos_z}
                                        onChange={handleChange}
                                        className="w-full bg-[#151a25] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white"
                                        type="text"
                                        placeholder="Z축"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-400 mb-1">FLOOR</label>
                                    <select
                                        name="floor_info"
                                        value={formData.floor_info}
                                        onChange={handleChange}
                                        className="w-full px-4 py-2 bg-[#151a25] border border-slate-700 rounded-lg text-sm text-white appearance-none outline-none"
                                    >
                                        <option value="Museum 1F">Museum 1F</option>
                                        <option value="Museum 2F">Museum 2F</option>
                                        <option value="Museum 3F">Museum 3F</option>
                                        <option value="Museum B1">Museum B1</option>
                                    </select>
                                </div>
                            </div>
                        </div>

                        <div className="lg:col-span-2 flex flex-col gap-6">
                            <h3 className="text-base font-semibold text-slate-200 border-b border-slate-800 pb-2">
                                전시품 & 설명
                            </h3>
                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-slate-400 mb-1">품명</label>
                                    <input
                                        name="title"
                                        value={formData.title}
                                        onChange={handleChange}
                                        className="w-full bg-[#151a25] border border-slate-700 rounded-lg px-4 py-2.5 text-white"
                                        type="text"
                                        placeholder="품명을 입력하시오."
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-400 mb-1">특징</label>
                                    <textarea
                                        name="feature"
                                        value={formData.feature}
                                        onChange={handleChange}
                                        className="w-full bg-[#151a25] border border-slate-700 rounded-lg px-4 py-2.5 text-slate-200"
                                        rows="3"
                                        placeholder="특징을 입력하시오."
                                    ></textarea>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-400 mb-1">설명</label>
                                    <textarea
                                        name="contents"
                                        value={formData.contents}
                                        onChange={handleChange}
                                        className="w-full bg-[#151a25] border border-slate-700 rounded-lg p-4 text-slate-200"
                                        rows="6"
                                        placeholder="설명을 입력하시오."
                                    ></textarea>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    );
}

// 실시간 시간 및 요일을 렌더링하는 시계 컴포넌트
function SidebarClock() {
    const [currentTime, setCurrentTime] = useState(new Date());

    useEffect(() => {
        // 1초마다 현재 시간 업데이트
        const timer = setInterval(() => setCurrentTime(new Date()), 1000);
        return () => clearInterval(timer);
    }, []);

    const year = currentTime.getFullYear();
    const month = String(currentTime.getMonth() + 1).padStart(2, "0");
    const day = String(currentTime.getDate()).padStart(2, "0");

    // [추가] 요일 계산 로직
    const dayOfWeek = ["일", "월", "화", "수", "목", "금", "토"][currentTime.getDay()];

    const hours = String(currentTime.getHours()).padStart(2, "0");
    const minutes = String(currentTime.getMinutes()).padStart(2, "0");

    return (
        <div className="mt-auto pt-6 border-t border-slate-800">
            <div className="flex flex-col gap-1">
                {/* [수정] 요일 렌더링 추가 */}
                <div className="flex items-center text-slate-500 text-xs tracking-wider">
                    {year}. {month}. {day} ({dayOfWeek})
                </div>
                <div className="flex items-center text-white font-medium">
                    <span className="mr-2 text-blue-400">⏱︎</span>
                    <span className="text-lg">
                        {hours}:{minutes}
                    </span>
                </div>
            </div>
        </div>
    );
}

export default Content;
