import { useState, useEffect } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { authFetch, removeToken } from "../utils/auth";

function Index() {
    const location = useLocation();
    const navigate = useNavigate();
    const handleLogout = () => {
        removeToken();
        navigate('/login', { replace: true });
    };

    // --- 리뷰 목록 페이지네이션 및 데이터 상태 ---
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;

    // 데이터 상태 변수명을 리뷰에 맞게 변경
    const [reviewsData, setReviewsData] = useState([]);

    // --- 리뷰 데이터 패칭 함수 ---
    const fetchReviews = async () => {
        try {
            const response = await authFetch("/api/reviews");
            if (response.ok) {
                const result = await response.json();
                setReviewsData(result.data);
            } else if (response.status === 401) {
                removeToken();
                navigate('/login', { replace: true });
            }
        } catch (error) {
            console.error("리뷰 데이터 로드 실패:", error);
        }
    };

    useEffect(() => {
        fetchReviews();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    // --- 리뷰 삭제 핸들러 ---
    const handleDeleteReview = async (id) => {
        // 삭제 전 사용자 확인 다이얼로그 표시
        if (!window.confirm("이 리뷰를 삭제하시겠습니까?")) return;

        try {
            const response = await authFetch(`/api/reviews/${id}`, {
                method: "DELETE",
            });

            if (response.ok) {
                fetchReviews();
            } else if (response.status === 401) {
                removeToken();
                navigate('/login', { replace: true });
            } else {
                alert("삭제 실패: 권한이 없습니다.");
            }
        } catch (error) {
            console.error("리뷰 삭제 오류:", error);
            alert("삭제 실패: 서버에 연결할 수 없습니다.");
        }
    };

    // --- 페이지네이션 계산 로직 ---
    const indexOfLastItem = currentPage * itemsPerPage;
    const indexOfFirstItem = indexOfLastItem - itemsPerPage;
    const currentItems = reviewsData.slice(indexOfFirstItem, indexOfLastItem);
    const totalPages = Math.ceil(reviewsData.length / itemsPerPage) || 1;

    const handlePrevPage = () => {
        setCurrentPage((prev) => Math.max(prev - 1, 1));
    };

    const handleNextPage = () => {
        setCurrentPage((prev) => Math.min(prev + 1, totalPages));
    };

    return (
        <div className="flex h-screen w-full bg-[#101622] font-display text-white overflow-hidden antialiased">
            {/* 사이드바 (내비게이션) 유지 */}
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
                            className={`flex items-center gap-3 px-3 py-2 rounded-lg transition-colors ${
                                location.pathname === "/"
                                    ? "bg-primary/10 text-primary border-l-4 border-primary"
                                    : "text-slate-300 hover:bg-slate-800"
                            }`}
                        >
                            <span
                                className={`material-symbols-outlined ${location.pathname === "/" ? "icon-filled" : ""}`}
                                style={{ fontSize: "24px" }}
                            >
                                dashboard
                            </span>
                            <span className="text-sm font-medium">홈</span>
                        </Link>
                        <Link
                            to="/space"
                            className={`flex items-center gap-3 px-3 py-2 rounded-lg transition-colors ${
                                location.pathname === "/space"
                                    ? "bg-primary/10 text-primary border-l-4 border-primary"
                                    : "text-slate-300 hover:bg-slate-800"
                            }`}
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "24px" }}>
                                map
                            </span>
                            <span className="text-sm font-medium">공간 관리</span>
                        </Link>
                        <Link
                            to="/content"
                            className={`flex items-center gap-3 px-3 py-2 rounded-lg transition-colors ${
                                location.pathname === "/content"
                                    ? "bg-primary/10 text-primary border-l-4 border-primary"
                                    : "text-slate-300 hover:bg-slate-800"
                            }`}
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: "24px" }}>
                                inventory_2
                            </span>
                            <span className="text-sm font-medium">전시콘텐츠 관리</span>
                        </Link>
                        <Link
                            to="/user"
                            className={`flex items-center gap-3 px-3 py-2 rounded-lg transition-colors ${
                                location.pathname === "/user"
                                    ? "bg-primary/10 text-primary border-l-4 border-primary"
                                    : "text-slate-300 hover:bg-slate-800"
                            }`}
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

            {/* 메인 콘텐츠 영역 */}
            <main className="flex-1 flex flex-col h-full bg-[#101622] overflow-y-auto relative scrollbar-hide">
                <div className="p-6 md:p-8 max-w-[1600px] mx-auto w-full flex flex-col gap-6">
                    {/* 상단 타이틀 영역 */}
                    <div className="rounded-2xl overflow-hidden relative min-h-[180px] flex flex-col justify-end bg-[#111318] border border-slate-800 shadow-lg">
                        <div
                            className="absolute inset-0 bg-cover bg-center opacity-80"
                            style={{ backgroundImage: `url(${process.env.PUBLIC_URL}/images/review.png)` }}
                        ></div>
                        <div className="absolute inset-0 bg-gradient-to-t from-[#101622] via-[#101622]/70 to-transparent"></div>
                        <div className="relative z-10 p-6 md:p-8 flex justify-between items-end">
                            <div>
                                <h2 className="text-white text-3xl font-bold leading-tight tracking-tight mb-2">
                                    사용자 리뷰 관리
                                </h2>
                                <p className="text-gray-400 text-sm">관람객 리뷰 및 만족도 확인</p>
                            </div>
                        </div>
                    </div>

                    {/* 리뷰 목록 테이블 */}
                    <div className="bg-[#151a25] border border-slate-800 rounded-xl overflow-hidden mb-8 shadow-sm">
                        <div className="p-4 border-b border-slate-800 flex justify-between items-center">
                            <h3 className="text-white font-bold text-base">리뷰 목록</h3>
                            <span className="text-slate-500 text-xs">
                                총 {reviewsData.length}개 중 {reviewsData.length > 0 ? indexOfFirstItem + 1 : 0}-
                                {Math.min(indexOfLastItem, reviewsData.length)} 표시
                            </span>
                        </div>
                        <div className="overflow-x-auto">
                            <table className="w-full text-left border-collapse">
                                <thead>
                                    <tr className="bg-[#111318] text-slate-400 text-xs uppercase tracking-wider">
                                        {/* 전시품명 관련 헤더 삭제 */}
                                        <th className="p-4 font-medium border-b border-slate-800 w-1/5">
                                            작성자(별명)
                                        </th>
                                        <th className="p-4 font-medium border-b border-slate-800 w-1/2">리뷰 내용</th>
                                        <th className="p-4 font-medium border-b border-slate-800 w-24">별점</th>
                                        <th className="p-4 font-medium border-b border-slate-800 w-32">작성일</th>
                                        {/* 리뷰 삭제 버튼 열 헤더 */}
                                        <th className="p-4 font-medium border-b border-slate-800 w-20 text-center">관리</th>
                                    </tr>
                                </thead>
                                <tbody className="text-sm divide-y divide-slate-800">
                                    {currentItems.map((item) => (
                                        <TableRow
                                            key={item.id}
                                            id={item.id}
                                            nickname={item.nickname}
                                            content={item.content}
                                            star={item.rating}
                                            date={item.created_at}
                                            onDelete={handleDeleteReview}
                                        />
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        {/* 페이지네이션 컨트롤 */}
                        <div className="p-3 border-t border-slate-800 flex justify-center items-center gap-2 bg-[#151a25]">
                            <button
                                onClick={handlePrevPage}
                                disabled={currentPage === 1}
                                className="p-1 rounded hover:bg-[#1e2430] text-slate-400 disabled:opacity-30"
                            >
                                <span className="material-symbols-outlined">chevron_left</span>
                            </button>
                            <span className="text-xs text-slate-400 font-mono">
                                {currentPage} / {totalPages}
                            </span>
                            <button
                                onClick={handleNextPage}
                                disabled={currentPage === totalPages}
                                className="p-1 rounded hover:bg-[#1e2430] text-slate-400 disabled:opacity-30"
                            >
                                <span className="material-symbols-outlined">chevron_right</span>
                            </button>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    );
}

// --- 사이드바 시계 컴포넌트 ---
function SidebarClock() {
    const [currentTime, setCurrentTime] = useState(new Date());

    useEffect(() => {
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

// --- 테이블 행 렌더링 컴포넌트 ---
// id, onDelete prop 추가 — 삭제 버튼 클릭 시 handleDeleteReview 호출
function TableRow({ id, nickname, content, star, date, onDelete }) {
    // 날짜 포맷팅 로직 (YYYY-MM-DD)
    const formattedDate = date ? new Date(date).toLocaleDateString() : "-";

    // 별점 렌더링 로직 (예: ★★★☆☆)
    const renderStars = () => {
        const filledStars = "★".repeat(star);
        const emptyStars = "☆".repeat(5 - star);
        return (
            <span className="text-yellow-500 text-sm">
                {filledStars}
                <span className="text-slate-600">{emptyStars}</span>
            </span>
        );
    };

    return (
        <tr className="hover:bg-[#1d232e] transition-colors group cursor-pointer">
            {/* 전시품명 <td> 삭제 */}
            <td className="p-4 text-slate-400 text-sm font-medium">{nickname || "익명"}</td>
            <td className="p-4 text-white text-sm">{content}</td>
            <td className="p-4 font-mono">{renderStars()}</td>
            <td className="p-4 text-slate-400 text-xs font-mono">{formattedDate}</td>
            {/* 삭제 버튼 셀 — 휴지통 아이콘(Material Symbol) 사용 */}
            <td className="p-4 text-center">
                <button
                    onClick={() => onDelete(id)}
                    className="text-red-500 hover:text-red-400 text-xs flex items-center justify-center mx-auto"
                    title="리뷰 삭제"
                >
                    <span className="material-symbols-outlined" style={{ fontSize: "18px" }}>
                        delete
                    </span>
                </button>
            </td>
        </tr>
    );
}

export default Index;
