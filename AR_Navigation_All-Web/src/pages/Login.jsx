import { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { setLoginFlag } from '../utils/auth';

export default function Login() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    const navigate = useNavigate();
    const location = useLocation();
    // 로그인 전에 접근하려 했던 경로로 복귀, 없으면 홈으로
    const from = location.state?.from?.pathname || '/';

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!username || !password) {
            setError('아이디와 비밀번호를 입력해주세요.');
            return;
        }

        setLoading(true);
        setError('');

        try {
            const res = await fetch('/api/auth/login', {
                method: 'POST',
                credentials: 'include', // httpOnly 쿠키 수신을 위해 필수
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password }),
            });

            if (res.ok) {
                const data = await res.json();
                // JWT는 백엔드에서 httpOnly 쿠키로 설정됨 — 로그인 플래그만 저장
                if (data.success) {
                    setLoginFlag();
                    navigate(from, { replace: true });
                } else {
                    setError('로그인에 실패했습니다. 다시 시도해주세요.');
                }
            } else if (res.status === 401) {
                setError('아이디 또는 비밀번호가 올바르지 않습니다.');
            } else {
                setError('서버 오류가 발생했습니다. 잠시 후 다시 시도해주세요.');
            }
        } catch {
            setError('서버에 연결할 수 없습니다. 네트워크 상태를 확인해주세요.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="flex h-screen w-full items-center justify-center bg-[#101622] font-display antialiased">
            {/* 배경 그라디언트 장식 */}
            <div className="pointer-events-none absolute inset-0 overflow-hidden">
                <div className="absolute -top-40 -left-40 h-96 w-96 rounded-full bg-primary/10 blur-3xl" />
                <div className="absolute -bottom-40 -right-40 h-96 w-96 rounded-full bg-primary/5 blur-3xl" />
            </div>

            <div className="relative w-full max-w-sm px-6">
                {/* 로고 및 제목 */}
                <div className="mb-8 flex flex-col items-center gap-3">
                    <img
                        src="/images/arlogo.jpg"
                        alt="AR Navigation 로고"
                        className="h-16 w-16 rounded-2xl object-cover shadow-lg shadow-primary/20"
                    />
                    <div className="text-center">
                        <h1 className="text-2xl font-bold text-white">AR Navigation</h1>
                        <p className="mt-1 text-sm text-slate-400">관리자 대시보드</p>
                    </div>
                </div>

                {/* 로그인 카드 */}
                <div className="rounded-2xl border border-slate-800 bg-[#1e2430] p-8 shadow-2xl">
                    <h2 className="mb-6 text-lg font-semibold text-white">로그인</h2>

                    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
                        {/* 아이디 입력 */}
                        <div className="flex flex-col gap-1.5">
                            <label className="text-sm font-medium text-slate-300" htmlFor="username">
                                아이디
                            </label>
                            <div className="relative">
                                <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 select-none"
                                    style={{ fontSize: '18px' }}>
                                    person
                                </span>
                                <input
                                    id="username"
                                    type="text"
                                    value={username}
                                    onChange={(e) => setUsername(e.target.value)}
                                    placeholder="관리자 아이디"
                                    autoComplete="username"
                                    autoFocus
                                    className="w-full rounded-lg border border-slate-700 bg-[#111318] py-2.5 pl-10 pr-4 text-sm text-white placeholder-slate-500 outline-none transition-colors focus:border-primary focus:ring-1 focus:ring-primary"
                                />
                            </div>
                        </div>

                        {/* 비밀번호 입력 */}
                        <div className="flex flex-col gap-1.5">
                            <label className="text-sm font-medium text-slate-300" htmlFor="password">
                                비밀번호
                            </label>
                            <div className="relative">
                                <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 select-none"
                                    style={{ fontSize: '18px' }}>
                                    lock
                                </span>
                                <input
                                    id="password"
                                    type="password"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    placeholder="비밀번호"
                                    autoComplete="current-password"
                                    className="w-full rounded-lg border border-slate-700 bg-[#111318] py-2.5 pl-10 pr-4 text-sm text-white placeholder-slate-500 outline-none transition-colors focus:border-primary focus:ring-1 focus:ring-primary"
                                />
                            </div>
                        </div>

                        {/* 에러 메시지 */}
                        {error && (
                            <div className="flex items-center gap-2 rounded-lg border border-red-800/50 bg-red-900/20 px-3 py-2.5">
                                <span className="material-symbols-outlined flex-shrink-0 text-red-400"
                                    style={{ fontSize: '16px' }}>
                                    error
                                </span>
                                <p className="text-xs text-red-400">{error}</p>
                            </div>
                        )}

                        {/* 로그인 버튼 */}
                        <button
                            type="submit"
                            disabled={loading}
                            className="mt-2 flex w-full items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-semibold text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
                        >
                            {loading ? (
                                <>
                                    <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                                        <circle className="opacity-25" cx="12" cy="12" r="10"
                                            stroke="currentColor" strokeWidth="4" />
                                        <path className="opacity-75" fill="currentColor"
                                            d="M4 12a8 8 0 018-8v8H4z" />
                                    </svg>
                                    로그인 중...
                                </>
                            ) : (
                                <>
                                    <span className="material-symbols-outlined" style={{ fontSize: '18px' }}>
                                        login
                                    </span>
                                    로그인
                                </>
                            )}
                        </button>
                    </form>
                </div>

                <p className="mt-6 text-center text-xs text-slate-600">
                    AR Navigation Admin &copy; {new Date().getFullYear()}
                </p>
            </div>
        </div>
    );
}
