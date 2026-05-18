import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { removeToken } from '../utils/auth';

function Index() {
    const navigate = useNavigate();
    const [currentFloor, setCurrentFloor] = useState(1);
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 5;

    const [artifactsData, setArtifactsData] = useState([]);
    const [userCount, setUserCount] = useState(null);
    const [avgRating, setAvgRating] = useState(null);
    const [ageGroups, setAgeGroups] = useState([]);

    const handleLogout = () => {
        removeToken();
        navigate('/login', { replace: true });
    };

    const floorImages = {
        1: '/images/floor_1.jpg',
        2: '/images/floor_2.jpg',
        3: '/images/floor_3.jpg',
        4: '/images/floor_B1.jpg',
    };

    const fetchArtifacts = async () => {
        try {
            const response = await fetch('/api/artworks');
            if (response.ok) {
                // NestJS ResponseInterceptor가 { success, data } 형태로 래핑
                const result = await response.json();
                setArtifactsData(result.data);
            }
        } catch (error) {
            console.error('전시품 데이터 로드 실패:', error);
        }
    };

    const fetchAnalytics = async () => {
        try {
            const [countRes, statsRes, ageRes] = await Promise.all([
                fetch('/api/analytics/user-count'),
                fetch('/api/analytics/artwork-stats'),
                fetch('/api/analytics/age-groups'),
            ]);

            if (countRes.ok) {
                const r = await countRes.json();
                setUserCount(r.data);
            }

            if (statsRes.ok) {
                const r = await statsRes.json();
                const stats = r.data;
                if (stats && stats.length > 0) {
                    const totalReviews = stats.reduce((s, x) => s + x.review_count, 0);
                    const weightedSum = stats.reduce((s, x) => s + x.average_rating * x.review_count, 0);
                    setAvgRating(totalReviews > 0 ? (weightedSum / totalReviews).toFixed(1) : null);
                }
            }

            if (ageRes.ok) {
                const r = await ageRes.json();
                const raw = r.data;
                // visitors 테이블의 age_group은 앱에서 한국어로 직접 저장됨
                const total = Object.values(raw).reduce((s, v) => s + v, 0);
                const sorted = Object.entries(raw)
                    .map(([key, count]) => ({
                        label: key,
                        count,
                        percent: total > 0 ? Math.round((count / total) * 100) : 0,
                    }))
                    .sort((a, b) => b.count - a.count)
                    .slice(0, 4);
                setAgeGroups(sorted);
            }
        } catch (error) {
            console.error('분석 데이터 로드 실패:', error);
        }
    };

    useEffect(() => {
        fetchArtifacts();
        fetchAnalytics();
    }, []);

    const indexOfLastItem = currentPage * itemsPerPage;
    const indexOfFirstItem = indexOfLastItem - itemsPerPage;

    const currentItems = artifactsData.slice(indexOfFirstItem, indexOfLastItem);
    const totalPages = Math.ceil(artifactsData.length / itemsPerPage) || 1;

    const handlePrevPage = () => {
        setCurrentPage((prev) => Math.max(prev - 1, 1));
    };

    const handleNextPage = () => {
        setCurrentPage((prev) => Math.min(prev + 1, totalPages));
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
                            className="flex items-center gap-3 px-3 py-2 rounded-lg bg-primary/10 text-primary border-l-4 border-primary transition-colors"
                        >
                            <span className="material-symbols-outlined icon-filled" style={{ fontSize: '24px' }}>
                                dashboard
                            </span>
                            <span className="text-sm font-medium">홈</span>
                        </Link>
                        <Link
                            to="/space"
                            className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-300 hover:bg-slate-800 transition-colors"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: '24px' }}>
                                map
                            </span>
                            <span className="text-sm font-medium">공간 관리</span>
                        </Link>
                        <Link
                            to="/content"
                            className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-300 hover:bg-slate-800 transition-colors"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: '24px' }}>
                                inventory_2
                            </span>
                            <span className="text-sm font-medium">전시콘텐츠 관리</span>
                        </Link>
                        <Link
                            to="/user"
                            className="flex items-center gap-3 px-3 py-2 rounded-lg text-slate-300 hover:bg-slate-800 transition-colors"
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: '24px' }}>
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

            <main className="flex-1 flex flex-col h-full bg-[#101622] overflow-y-auto relative scrollbar-hide">
                <div className="p-6 md:p-8 max-w-[1600px] mx-auto w-full flex flex-col gap-6">
                    <div className="rounded-2xl overflow-hidden relative min-h-[180px] flex flex-col justify-end bg-[#111318] border border-slate-800 shadow-lg">
                        <div
                            className="absolute inset-0 bg-cover bg-center opacity-80"
                            style={{ backgroundImage: `url(${process.env.PUBLIC_URL}/images/arPhoto.png)` }}
                        ></div>
                        <div className="absolute inset-0 bg-gradient-to-t from-[#101622] via-[#101622]/70 to-transparent"></div>
                        <div className="relative z-10 p-6 md:p-8 flex justify-between items-end">
                            <div>
                                <h2 className="text-white text-3xl font-bold leading-tight tracking-tight mb-2">
                                    관리자 모드
                                </h2>
                                <p className="text-gray-400 text-sm">AR 내비게이션 관리</p>
                            </div>
                        </div>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                        <KpiCard
                            icon="groups"
                            title="총 관람객 수"
                            value={userCount !== null ? userCount.toLocaleString() : '—'}
                        />
                        <KpiCard icon="category" title="등록된 전시품 갯수" value={`${artifactsData.length}개`} />
                        <KpiCard icon="view_in_ar" title="AR 활성화 횟수" value="—" />
                        <KpiCard
                            icon="star"
                            title="사용자 만족도"
                            value={avgRating !== null ? `${avgRating}/5.0` : '—'}
                        />
                    </div>

                    <div>
                        <h3 className="text-white font-bold text-xl mb-4 flex items-center gap-2">
                            <span className="material-symbols-outlined text-yellow-500">crown</span>
                            대표 소장품
                        </h3>
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                            <KpiCard
                                image={`${process.env.PUBLIC_URL}/images/mu1.png`}
                                title="쌍룡운문경"
                                value="고려시대에 이르면 각종 기명(器皿) 이외에도 여러가지의 금속공예품이 일용품으로 전해지는데 청동거울도 이 가운데의 하나로서 많은 종류와 숫자가 알려져 있다."
                            />
                            <KpiCard
                                image={`${process.env.PUBLIC_URL}/images/mu2.png`}
                                title="청자주전자"
                                value="길쭉하게 둥근 원형의 몸통 양면에 상감수법으로 석류무늬를 만들어 넣고 그 밑부분에는 양각으로 연꽃잎의 문양을 돌려 넣은 주전자이다."
                            />
                            <KpiCard
                                image={`${process.env.PUBLIC_URL}/images/mu3.png`}
                                title="청동향로"
                                value="향완(香琓)이라고도 불리우며 청동반자(靑銅飯子), 청동정병(靑銅淨甁) 등과 함께 불구(佛具)로 사용되었다."
                            />
                            <KpiCard
                                image={`${process.env.PUBLIC_URL}/images/mu4.png`}
                                title="평양성10폭병풍"
                                value="평양성10폭병풍은 지도류의 일종으로 넓은 의미에서 기록화로서 당시 평양의 모습을 한눈에 파악할 수 있는 자료로서 병풍에 그 당시 지명이 상세히 적혀져 있다."
                            />
                        </div>
                    </div>

                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                        <div className="lg:col-span-2 bg-[#151a25] border border-slate-800 rounded-xl flex flex-col overflow-hidden shadow-sm">
                            <div className="p-4 border-b border-slate-800 flex justify-between items-center bg-[#151a25]">
                                <h3 className="text-white font-bold flex items-center gap-2">
                                    <span className="material-symbols-outlined text-primary">map</span>
                                    Museum Map ({currentFloor === 4 ? '지하 1층' : `${currentFloor}층`})
                                </h3>
                                <div className="flex gap-2">
                                    {[1, 2, 3, 4].map((floor) => (
                                        <button
                                            key={floor}
                                            onClick={() => setCurrentFloor(floor)}
                                            className={`px-3 py-1.5 rounded text-xs font-medium transition-colors border ${currentFloor === floor ? 'bg-primary text-white border-primary shadow-sm' : 'bg-[#1e2430] text-slate-400 border-slate-700 hover:text-white hover:bg-[#252c3b]'}`}
                                        >
                                            {floor === 4 ? '지하 1층' : `${floor}층`}
                                        </button>
                                    ))}
                                </div>
                            </div>

                            <div className="relative flex-1 bg-[#0f1218] min-h-[400px] flex items-center justify-center overflow-hidden">
                                <img
                                    src={floorImages[currentFloor]}
                                    alt={`${currentFloor}층 지도`}
                                    className="w-full h-full object-contain p-4 animate-fade-in z-0"
                                />
                            </div>
                        </div>

                        <div className="flex flex-col gap-6">
                            <div className="bg-[#151a25] border border-slate-800 rounded-xl p-5 shadow-sm h-full">
                                <h3 className="text-white font-bold mb-4 flex items-center gap-2">
                                    <span className="material-symbols-outlined text-yellow-500">timeline</span>
                                    층별 세부사항
                                </h3>
                                <div className="flex flex-col gap-3 relative">
                                    {/* \n을 사용하여 줄바꿈 데이터 전달 */}
                                    <SequenceStep
                                        title="Floor B1"
                                        value={'A : 4수장고\nB : 5수장고(보이는 수장고)'}
                                        color="border-l-blue-500 text-blue-400"
                                    />
                                    <SequenceStep
                                        title="Floor F1"
                                        value={
                                            'A : 출입문\nB : 학교사 전시실\nC : 1수장고(보이는 수장고)\nD : 2수장고(보이는 수장고)\nE : 3수장고\n* 여성화정실, 여성장애인화장실'
                                        }
                                        color="border-l-blue-500 text-blue-400"
                                    />
                                    <SequenceStep
                                        title="Floor F2"
                                        value={
                                            'A : 기증자 전시실\nB : 김조자 기획전시실\nC : 체엄 실습실\nD : 명예의전당\nE : 강의실\n* 남성화장실, 남성장애인화장실'
                                        }
                                        color="border-l-blue-500 text-blue-400"
                                    />
                                    <SequenceStep
                                        title="Floor F3"
                                        value={'A : 상설 전시실'}
                                        color="border-l-blue-500 text-blue-400"
                                    />
                                </div>
                            </div>

                            <div className="bg-[#151a25] border border-slate-800 rounded-xl p-5 shadow-sm">
                                <h3 className="text-white font-bold mb-4">방문 연령대</h3>
                                <div className="flex flex-col gap-4">
                                    {ageGroups.length > 0 ? (
                                        ageGroups.map((g, i) => (
                                            <RankingBar
                                                key={g.label}
                                                rank={String(i + 1)}
                                                title={g.label}
                                                time={`${g.percent}%`}
                                                percent={`${g.percent}%`}
                                                barColor={
                                                    ['bg-rose-500', 'bg-orange-500', 'bg-amber-500', 'bg-blue-500'][i]
                                                }
                                            />
                                        ))
                                    ) : (
                                        <p className="text-slate-500 text-sm text-center py-4">설문 데이터 없음</p>
                                    )}
                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="bg-[#151a25] border border-slate-800 rounded-xl overflow-hidden mb-8 shadow-sm">
                        <div className="p-4 border-b border-slate-800 flex justify-between items-center">
                            <h3 className="text-white font-bold text-base">전시품 목록</h3>
                            <span className="text-slate-500 text-xs">
                                총 {artifactsData.length}개 중 {artifactsData.length > 0 ? indexOfFirstItem + 1 : 0}-
                                {Math.min(indexOfLastItem, artifactsData.length)} 표시
                            </span>
                        </div>
                        <div className="overflow-x-auto">
                            <table className="w-full text-left border-collapse">
                                <thead>
                                    <tr className="bg-[#111318] text-slate-400 text-xs uppercase tracking-wider">
                                        <th className="p-4 font-medium border-b border-slate-800">전시물 명 (title)</th>
                                        <th className="p-4 font-medium border-b border-slate-800">위치 (Floor)</th>
                                    </tr>
                                </thead>
                                <tbody className="text-sm divide-y divide-slate-800">
                                    {currentItems.map((item) => (
                                        <TableRow key={item.id} name={item.title} zone={item.floor_info} />
                                    ))}
                                </tbody>
                            </table>
                        </div>

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

// 실시간 시간 및 요일을 렌더링하는 시계 컴포넌트
function SidebarClock() {
    const [currentTime, setCurrentTime] = useState(new Date());

    useEffect(() => {
        // 1초마다 현재 시간 업데이트
        const timer = setInterval(() => setCurrentTime(new Date()), 1000);
        return () => clearInterval(timer);
    }, []);

    const year = currentTime.getFullYear();
    const month = String(currentTime.getMonth() + 1).padStart(2, '0');
    const day = String(currentTime.getDate()).padStart(2, '0');

    // getDay()를 이용해 0(일요일)부터 6(토요일)까지의 값을 요일 문자열로 변환
    const dayOfWeek = ['일', '월', '화', '수', '목', '금', '토'][currentTime.getDay()];

    const hours = String(currentTime.getHours()).padStart(2, '0');
    const minutes = String(currentTime.getMinutes()).padStart(2, '0');

    return (
        <div className="mt-auto pt-6 border-t border-slate-800">
            <div className="flex flex-col gap-1">
                {/* 렌더링 영역에 요일({dayOfWeek}) 추가 */}
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

function KpiCard({ icon, title, value, image }) {
    if (icon) {
        return (
            <div className="bg-[#151a25] p-5 rounded-xl border border-slate-800 hover:border-slate-600 transition-all duration-300 shadow-sm flex items-center gap-4">
                <div className="w-12 h-12 rounded-lg bg-[#1e2430] text-slate-300 flex items-center justify-center border border-slate-700 flex-shrink-0">
                    <span className="material-symbols-outlined text-[24px]">{icon}</span>
                </div>
                <div className="flex flex-col">
                    <h4 className="text-slate-400 text-xs font-medium uppercase tracking-wider mb-0.5">{title}</h4>
                    <p className="text-white text-xl font-bold tracking-tight leading-none">{value}</p>
                </div>
            </div>
        );
    }
    return (
        <div className="bg-[#151a25] p-5 rounded-xl border border-slate-800 hover:border-slate-600 transition-all duration-300 shadow-sm flex flex-col gap-3 h-full">
            {image && (
                <div className="w-full h-40 rounded-lg overflow-hidden bg-[#1e2430] mb-1">
                    <img
                        src={image}
                        alt={title}
                        className="w-full h-full object-cover hover:scale-105 transition-transform duration-500"
                    />
                </div>
            )}
            <h4 className="text-white text-lg font-bold tracking-tight">{title}</h4>
            <p className="text-slate-400 text-sm leading-relaxed break-keep">{value}</p>
        </div>
    );
}

function RankingBar({ rank, title, time, percent, barColor }) {
    return (
        <div className="flex flex-col gap-1 w-full">
            <div className="flex justify-between text-xs text-slate-300">
                <span className="font-medium">
                    <span className="text-slate-500 mr-2">{rank}</span> {title}
                </span>
                <span className="font-bold text-white">{time}</span>
            </div>
            <div className="h-1.5 w-full bg-slate-800 rounded-full overflow-hidden">
                <div
                    className={`h-full ${barColor} rounded-full transition-all duration-500`}
                    style={{ width: percent }}
                ></div>
            </div>
        </div>
    );
}

function SequenceStep({ title, value, color }) {
    return (
        <div className="flex flex-col items-center gap-2 w-full z-10">
            <div
                className={`w-full bg-[#1e2430] p-3 rounded-lg text-center border-l-4 shadow-sm border border-slate-800 hover:bg-[#252c3b] transition-colors ${color}`}
            >
                <p className="text-slate-200 text-xs font-bold mb-0.5">{title}</p>
                {/* whiteSpace: "pre-wrap"을 추가하여 \n 줄바꿈이 적용되게 함 */}
                <p className="text-[11px] opacity-80 font-mono leading-relaxed" style={{ whiteSpace: 'pre-wrap' }}>
                    {value}
                </p>
            </div>
        </div>
    );
}

function TableRow({ name, zone }) {
    return (
        <tr className="hover:bg-[#1d232e] transition-colors group cursor-pointer">
            <td className="p-4 text-white font-medium w-2/3">{name}</td>
            <td className="p-4 text-slate-400 text-sm font-mono w-1/3">{zone}</td>
        </tr>
    );
}

export default Index;
