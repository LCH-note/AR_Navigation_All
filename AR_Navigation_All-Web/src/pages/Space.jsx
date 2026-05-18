import { useState, useEffect } from "react";
import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import { removeToken } from "../utils/auth";

// 공간관리 레이아웃 컴포넌트 — 사이드바 + 서브탭 내비게이션 + Outlet(하위 페이지) 포함
function Space() {
    const navigate = useNavigate();

    const handleLogout = () => {
        removeToken();
        navigate('/login', { replace: true });
    };

    // 마운트 시 기본 탭(지도 에디터)으로 리다이렉트
    useEffect(() => {
        if (window.location.pathname === "/space" || window.location.pathname === "/space/") {
            navigate("/space/editor", { replace: true });
        }
    }, [navigate]);

    return (
        <div className="flex h-screen w-full bg-[#101622] font-display text-white overflow-hidden antialiased">
            {/* 1. 좌측 내비게이션 사이드바 */}
            <nav className="w-64 border-r border-slate-800 bg-[#101622] flex-shrink-0 flex flex-col justify-between p-4 hidden md:flex z-20">
                <div className="flex flex-col gap-6">
                    {/* 로고 영역 */}
                    <div className="flex items-center px-2">
                        <img src="/images/arlogo.jpg" alt="로고" className="w-12 h-12 rounded-lg object-cover" />
                        <div className="ml-3 flex flex-col">
                            <h1 className="text-xl font-bold leading-tight text-white">AR System</h1>
                            <p className="text-slate-400 text-sm">관리자 모드</p>
                        </div>
                    </div>

                    {/* 메뉴 목록 */}
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

                        {/* 현재 페이지 강조 */}
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

                {/* 사이드바 하단: 로그아웃 버튼 + 시계 */}
                <div className="flex flex-col gap-2">
                    <button
                        onClick={handleLogout}
                        className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-400 hover:bg-red-900/20 hover:text-red-400 transition-colors w-full text-left"
                    >
                        <span className="material-symbols-outlined" style={{ fontSize: '24px' }}>logout</span>
                        <span className="text-sm font-medium">로그아웃</span>
                    </button>
                    <SidebarClock />
                </div>
            </nav>

            {/* 2. 메인 콘텐츠 영역 */}
            <main className="flex-1 flex flex-col h-full overflow-hidden">
                {/* 서브탭 내비게이션 바 */}
                <div className="flex-shrink-0 bg-[#151a25] border-b border-slate-800 px-6 flex items-center gap-1 h-14">
                    <div className="flex items-center gap-1">
                        {/* 지도 에디터 탭 */}
                        <NavLink
                            to="/space/editor"
                            className={({ isActive }) =>
                                `flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all ${
                                    isActive
                                        ? "bg-primary/10 text-primary border border-primary/30"
                                        : "text-slate-400 hover:text-white hover:bg-slate-800"
                                }`
                            }
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "18px" }}>
                                edit_location
                            </span>
                            지도 에디터
                        </NavLink>

                        {/* 맵 관리 탭 */}
                        <NavLink
                            to="/space/map"
                            className={({ isActive }) =>
                                `flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all ${
                                    isActive
                                        ? "bg-primary/10 text-primary border border-primary/30"
                                        : "text-slate-400 hover:text-white hover:bg-slate-800"
                                }`
                            }
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "18px" }}>
                                layers
                            </span>
                            맵 관리
                        </NavLink>

                        {/* 경로 관리 탭 */}
                        <NavLink
                            to="/space/route"
                            className={({ isActive }) =>
                                `flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all ${
                                    isActive
                                        ? "bg-primary/10 text-primary border border-primary/30"
                                        : "text-slate-400 hover:text-white hover:bg-slate-800"
                                }`
                            }
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "18px" }}>
                                route
                            </span>
                            경로 관리
                        </NavLink>
                    </div>

                    {/* 우측 공간 타이틀 */}
                    <div className="ml-auto flex items-center gap-2 text-slate-500 text-xs">
                        <span className="material-symbols-outlined" style={{ fontSize: "16px" }}>
                            map
                        </span>
                        공간 관리
                    </div>
                </div>

                {/* 하위 페이지 렌더링 영역 */}
                <div className="flex-1 flex overflow-hidden">
                    <Outlet />
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
    const dayOfWeek = ["일", "월", "화", "수", "목", "금", "토"][currentTime.getDay()];
    const hours = String(currentTime.getHours()).padStart(2, "0");
    const minutes = String(currentTime.getMinutes()).padStart(2, "0");

    return (
        <div className="mt-auto pt-6 border-t border-slate-800">
            <div className="flex flex-col gap-1">
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
