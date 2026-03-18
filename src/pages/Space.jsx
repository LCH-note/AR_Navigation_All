import { useState, useEffect } from "react"; // [추가] 시계 구동을 위한 Hook 임포트
import { Link } from "react-router-dom";

function Space() {
    return (
        <div className="flex h-screen w-full bg-[#101622] font-display text-white overflow-hidden antialiased">
            {/* 1. Left Navigation */}
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

                        {/* Active Item */}
                        <Link
                            to="/space"
                            className="flex items-center gap-3 px-3 py-2 rounded-lg bg-primary/10 text-primary border-l-4 border-primary transition-colors"
                        >
                            <span className="material-symbols-outlined icon-filled" style={{ fontSize: "24px" }}>
                                map
                            </span>
                            <span className="text-sm font-medium">공간 관리</span>
                        </Link>

                        <Link
                            to="/content"
                            className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-300 hover:bg-slate-800 transition-colors"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "24px" }}>
                                inventory_2
                            </span>
                            <span className="text-sm font-medium">전시콘텐츠 관리</span>
                        </Link>
                    </div>
                </div>

                {/* [추가] 사이드바 하단 실시간 시계 컴포넌트 마운트 */}
                <SidebarClock />
            </nav>

            {/* 2. Main Workspace */}
            <main className="flex-1 flex flex-col h-full bg-[#101622] overflow-hidden relative">
                {/* Header */}

                {/* Editor Layout */}
                <div className="flex-1 flex overflow-hidden">
                    <div className="w-72 bg-[#151a25] border-r border-slate-800 flex flex-col z-10">
                        {/* Floor Selector */}
                        <div className="p-4 border-b border-slate-800">
                            <label className="block text-xs font-semibold text-slate-400 mb-2 uppercase tracking-wide">
                                Floor Plan
                            </label>
                            <div className="relative">
                                {/* Select 배경 수정: bg-[#111318] */}
                                <select className="w-full pl-10 pr-4 py-2 bg-[#111318] border border-slate-700 rounded-lg text-sm font-medium text-white focus:ring-2 focus:ring-primary/50 cursor-pointer appearance-none outline-none">
                                    <option>Museum 1F</option>
                                    <option>Museum 2F</option>
                                    <option>Museum 3F</option>
                                    <option>Museum B1</option>
                                </select>
                                <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-lg pointer-events-none">
                                    layers
                                </span>
                                <span className="material-symbols-outlined absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 text-lg pointer-events-none">
                                    expand_more
                                </span>
                            </div>
                        </div>

                        {/* Layers */}
                        <div className="flex-1 overflow-y-auto p-4">
                            <div className="flex justify-between items-center mb-3">
                                <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wide">
                                    Visibility Layers
                                </label>
                            </div>
                            <div className="space-y-3">
                                <div className="flex items-center justify-between p-2 rounded-lg hover:bg-white/5 group cursor-pointer border border-transparent hover:border-slate-700">
                                    <div className="flex items-center gap-3">
                                        <span className="material-symbols-outlined text-slate-500 group-hover:text-primary transition-colors">
                                            map
                                        </span>
                                        <span className="text-sm text-slate-300">도면 (Map)</span>
                                    </div>
                                    <input
                                        defaultChecked
                                        className="rounded border-slate-600 bg-[#111318] text-primary focus:ring-primary/20 focus:ring-offset-0"
                                        type="checkbox"
                                    />
                                </div>
                                <div className="flex items-center justify-between p-2 rounded-lg bg-primary/10 border border-primary/20 group cursor-pointer">
                                    <div className="flex items-center gap-3">
                                        <span className="material-symbols-outlined text-primary">hub</span>
                                        <span className="text-sm font-medium text-white">AR 노드 (Nodes)</span>
                                    </div>
                                    <input
                                        defaultChecked
                                        className="rounded border-slate-600 bg-[#111318] text-primary focus:ring-primary/20 focus:ring-offset-0"
                                        type="checkbox"
                                    />
                                </div>
                                <div className="flex items-center justify-between p-2 rounded-lg hover:bg-white/5 group cursor-pointer border border-transparent hover:border-slate-700">
                                    <div className="flex items-center gap-3">
                                        <span className="material-symbols-outlined text-slate-500 group-hover:text-primary transition-colors">
                                            route
                                        </span>
                                        <span className="text-sm text-slate-300">이동 경로 (Edges)</span>
                                    </div>
                                    <input
                                        defaultChecked
                                        className="rounded border-slate-600 bg-[#111318] text-primary focus:ring-primary/20 focus:ring-offset-0"
                                        type="checkbox"
                                    />
                                </div>
                                <div className="flex items-center justify-between p-2 rounded-lg hover:bg-white/5 group cursor-pointer border border-transparent hover:border-slate-700">
                                    <div className="flex items-center gap-3">
                                        <span className="material-symbols-outlined text-slate-500 group-hover:text-primary transition-colors">
                                            stars
                                        </span>
                                        <span className="text-sm text-slate-300">전시물 (POIs)</span>
                                    </div>
                                    <input
                                        defaultChecked
                                        className="rounded border-slate-600 bg-[#111318] text-primary focus:ring-primary/20 focus:ring-offset-0"
                                        type="checkbox"
                                    />
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Center Panel: Map Canvas */}
                    <div className="flex-1 bg-[#0f1218] relative overflow-hidden flex items-center justify-center">
                        {/* Grid Pattern */}
                        <div
                            className="absolute inset-0 opacity-20 pointer-events-none"
                            style={{
                                backgroundImage:
                                    "linear-gradient(to right, #334155 1px, transparent 1px), linear-gradient(to bottom, #334155 1px, transparent 1px)",
                                backgroundSize: "20px 20px",
                            }}
                        ></div>

                        {/* Map Container */}
                        <div className="relative w-[800px] h-[600px] bg-[#1e293b] shadow-2xl rounded-sm border border-slate-700 overflow-hidden transform scale-95 origin-center">
                            {/* Floor Plan Image */}
                            <div
                                className="absolute inset-0 w-full h-full bg-cover bg-center opacity-30"
                                style={{
                                    backgroundImage:
                                        "url('https://images.unsplash.com/photo-1558036117-15ea8473e720?ixlib=rb-1.2.1&auto=format&fit=crop&w=1200&q=80')",
                                }}
                            ></div>

                            {/* SVG Paths */}
                            <svg className="absolute inset-0 w-full h-full pointer-events-none">
                                <line
                                    stroke="#475569"
                                    strokeDasharray="4"
                                    strokeWidth="2"
                                    x1="200"
                                    x2="350"
                                    y1="300"
                                    y2="250"
                                ></line>
                                <line stroke="#475569" strokeWidth="2" x1="350" x2="500" y1="250" y2="300"></line>
                                <line stroke="#475569" strokeWidth="2" x1="500" x2="500" y1="300" y2="450"></line>
                                <line stroke="#3b82f6" strokeWidth="3" x1="350" x2="450" y1="250" y2="150"></line>
                            </svg>

                            {/* Nodes & POIs (Overlay Elements) */}
                            <div className="absolute left-[200px] top-[300px] -translate-x-1/2 -translate-y-1/2 size-3 bg-slate-700 border-2 border-slate-400 rounded-full hover:scale-125 hover:bg-slate-500 transition-transform cursor-pointer shadow-sm"></div>
                            <div className="absolute left-[350px] top-[250px] -translate-x-1/2 -translate-y-1/2 size-4 bg-primary border-2 border-white/80 rounded-full shadow-lg shadow-primary/30 z-10 cursor-pointer ring-4 ring-primary/20"></div>
                            <div className="absolute left-[500px] top-[300px] -translate-x-1/2 -translate-y-1/2 size-3 bg-slate-700 border-2 border-slate-400 rounded-full hover:scale-125 hover:bg-slate-500 transition-transform cursor-pointer shadow-sm"></div>
                            <div className="absolute left-[500px] top-[450px] -translate-x-1/2 -translate-y-1/2 size-3 bg-slate-700 border-2 border-slate-400 rounded-full hover:scale-125 hover:bg-slate-500 transition-transform cursor-pointer shadow-sm"></div>
                            <div className="absolute left-[450px] top-[150px] -translate-x-1/2 -translate-y-1/2 size-3 bg-slate-700 border-2 border-slate-400 rounded-full hover:scale-125 hover:bg-slate-500 transition-transform cursor-pointer shadow-sm"></div>

                            <div className="absolute left-[450px] top-[120px] -translate-x-1/2 -translate-y-full flex flex-col items-center group cursor-pointer">
                                <div className="bg-[#111318] text-white text-[10px] px-2 py-0.5 rounded border border-slate-600 shadow-lg mb-1 opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap">
                                    고려 청자
                                </div>
                                <span className="material-symbols-outlined text-rose-500 text-3xl drop-shadow-md">
                                    location_on
                                </span>
                            </div>
                            <div className="absolute left-[200px] top-[270px] -translate-x-1/2 -translate-y-full flex flex-col items-center group cursor-pointer">
                                <div className="bg-[#111318] text-white text-[10px] px-2 py-0.5 rounded border border-slate-600 shadow-lg mb-1 opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap">
                                    안내 데스크
                                </div>
                                <span className="material-symbols-outlined text-emerald-500 text-3xl drop-shadow-md">
                                    support_agent
                                </span>
                            </div>
                        </div>

                        {/* Bottom Controls */}
                        <div className="absolute bottom-6 left-1/2 -translate-x-1/2 bg-[#1e2430] rounded-full shadow-lg shadow-black/30 border border-slate-700 flex items-center px-4 py-2 gap-4 text-white">
                            <button className="text-slate-400 hover:text-white transition-colors">
                                <span className="material-symbols-outlined">remove</span>
                            </button>
                            <span className="text-xs font-medium w-12 text-center">85%</span>
                            <button className="text-slate-400 hover:text-white transition-colors">
                                <span className="material-symbols-outlined">add</span>
                            </button>
                            <div className="w-px h-4 bg-slate-600 mx-1"></div>
                            <button className="text-slate-400 hover:text-white transition-colors" title="화면 맞춤">
                                <span className="material-symbols-outlined">fit_screen</span>
                            </button>
                        </div>
                    </div>

                    <div className="w-80 bg-[#151a25] border-l border-slate-800 flex flex-col z-10">
                        <div className="p-4 border-b border-slate-800 bg-[#151a25] flex justify-between items-center">
                            <h3 className="text-sm font-bold text-white">상세 속성</h3>
                        </div>

                        <div className="flex-1 overflow-y-auto p-5 space-y-6">
                            {/* Basic Info */}
                            <div>
                                <h4 className="text-xs font-bold text-slate-400 mb-3 uppercase flex items-center gap-2">
                                    <span className="material-symbols-outlined text-sm">info</span> 기본 정보
                                </h4>
                                <div className="space-y-3">
                                    <div>
                                        <label className="block text-xs font-medium text-slate-400 mb-1">
                                            식별자 (ID)
                                        </label>
                                        <input
                                            className="w-full text-sm border-slate-700 rounded-lg bg-[#111318] text-slate-300 focus:ring-primary focus:border-primary placeholder-gray-500"
                                            readOnly
                                            type="text"
                                            defaultValue="node_main_hall_04"
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-xs font-medium text-slate-400 mb-1">
                                            타입 (Type)
                                        </label>
                                        <select
                                            className="w-full text-sm border-slate-700 bg-[#111318] text-white rounded-lg focus:ring-primary focus:border-primary outline-none"
                                            defaultValue="교차로 (Junction)"
                                        >
                                            <option>일반 노드 (Normal)</option>
                                            <option>교차로 (Junction)</option>
                                            <option>목적지 (Destination)</option>
                                            <option>계단/엘리베이터 (Vertical)</option>
                                        </select>
                                    </div>
                                </div>
                            </div>
                            <div className="h-px bg-slate-700"></div>

                            {/* Coordinates */}
                            <div>
                                <h4 className="text-xs font-bold text-slate-400 mb-3 uppercase flex items-center gap-2">
                                    <span className="material-symbols-outlined text-sm">my_location</span> 좌표
                                    (Coordinates)
                                </h4>
                                <div className="grid grid-cols-2 gap-3">
                                    <div>
                                        <label className="block text-xs font-medium text-slate-400 mb-1">X Pos</label>
                                        <input
                                            className="w-full text-sm border-slate-700 bg-[#111318] text-white rounded-lg focus:ring-primary focus:border-primary outline-none"
                                            type="number"
                                            defaultValue="350"
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-xs font-medium text-slate-400 mb-1">Y Pos</label>
                                        <input
                                            className="w-full text-sm border-slate-700 bg-[#111318] text-white rounded-lg focus:ring-primary focus:border-primary outline-none"
                                            type="number"
                                            defaultValue="250"
                                        />
                                    </div>
                                </div>
                            </div>
                            <div className="h-px bg-slate-700"></div>

                            {/* Accessibility */}
                            <div>
                                <h4 className="text-xs font-bold text-slate-400 mb-3 uppercase flex items-center gap-2">
                                    <span className="material-symbols-outlined text-sm">accessible_forward</span> 접근성
                                    설정
                                </h4>
                                <div className="space-y-2">
                                    <label className="flex items-center gap-3 p-2 border border-slate-700 rounded-lg cursor-pointer hover:bg-white/5 transition-colors">
                                        <input
                                            defaultChecked
                                            className="rounded border-slate-600 bg-[#111318] text-primary focus:ring-primary focus:ring-offset-0"
                                            type="checkbox"
                                        />
                                        <span className="text-sm text-white">휠체어 접근 가능</span>
                                    </label>
                                    <label className="flex items-center gap-3 p-2 border border-slate-700 rounded-lg cursor-pointer hover:bg-white/5 transition-colors">
                                        <input
                                            className="rounded border-slate-600 bg-[#111318] text-primary focus:ring-primary focus:ring-offset-0"
                                            type="checkbox"
                                        />
                                        <span className="text-sm text-white">경사로 있음 (Ramp)</span>
                                    </label>
                                </div>
                            </div>
                            <div className="h-px bg-slate-700"></div>

                            {/* Connected Nodes */}
                            <div>
                                <h4 className="text-xs font-bold text-slate-400 mb-3 uppercase flex items-center gap-2">
                                    <span className="material-symbols-outlined text-sm">hub</span> 연결된 노드 (Edges)
                                </h4>
                                <div className="flex flex-col gap-2">
                                    <div className="flex items-center justify-between p-2 bg-[#101622] rounded border border-slate-700 text-xs hover:border-slate-500 transition-colors">
                                        <span className="font-medium text-white">To: node_main_hall_03</span>
                                        <span className="text-slate-500">12m</span>
                                        <button className="text-rose-400 hover:bg-rose-500/10 p-1 rounded transition-colors">
                                            <span className="material-symbols-outlined text-sm">close</span>
                                        </button>
                                    </div>
                                    <div className="flex items-center justify-between p-2 bg-[#101622] rounded border border-slate-700 text-xs hover:border-slate-500 transition-colors">
                                        <span className="font-medium text-white">To: node_corridor_01</span>
                                        <span className="text-slate-500">8m</span>
                                        <button className="text-rose-400 hover:bg-rose-500/10 p-1 rounded transition-colors">
                                            <span className="material-symbols-outlined text-sm">close</span>
                                        </button>
                                    </div>
                                    <button className="w-full py-2 border border-dashed border-slate-600 text-slate-400 text-xs rounded hover:bg-white/5 hover:text-primary hover:border-primary transition-colors">
                                        + 연결 추가하기
                                    </button>
                                </div>
                            </div>
                        </div>

                        {/* Bottom Actions */}
                        <div className="p-4 border-t border-slate-800 bg-[#151a25] flex gap-2">
                            <button className="flex-1 py-2 text-sm text-rose-500 font-medium border border-rose-900/50 bg-rose-900/20 rounded-lg hover:bg-rose-900/40 transition-colors">
                                삭제
                            </button>
                            <button className="flex-[2] py-2 text-sm text-white font-medium bg-primary rounded-lg hover:bg-blue-600 shadow-lg shadow-blue-500/20 transition-colors">
                                적용하기
                            </button>
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

    // [추가] getDay()를 이용해 0(일요일)부터 6(토요일)까지의 값을 요일 문자열로 변환
    const dayOfWeek = ["일", "월", "화", "수", "목", "금", "토"][currentTime.getDay()];

    const hours = String(currentTime.getHours()).padStart(2, "0");
    const minutes = String(currentTime.getMinutes()).padStart(2, "0");

    return (
        <div className="mt-auto pt-6 border-t border-slate-800">
            <div className="flex flex-col gap-1">
                {/* [수정] 렌더링 영역에 요일({dayOfWeek}) 추가 */}
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

export default Space;
