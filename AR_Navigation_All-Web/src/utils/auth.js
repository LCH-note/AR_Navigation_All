// JWT는 백엔드에서 httpOnly 쿠키로 설정됨 — JS에서 직접 접근 불가
// 로그인 상태 여부만 localStorage에 플래그로 저장
const LOGIN_FLAG_KEY = 'ar_admin_logged_in';

export const setLoginFlag = () => localStorage.setItem(LOGIN_FLAG_KEY, '1');
export const clearLoginFlag = () => localStorage.removeItem(LOGIN_FLAG_KEY);
export const isAuthenticated = () => localStorage.getItem(LOGIN_FLAG_KEY) === '1';

/**
 * 인증이 필요한 fetch 요청
 * credentials: 'include' 로 httpOnly 쿠키를 자동 전송
 * 401 수신 시 로그인 플래그 초기화 후 로그인 페이지로 이동
 */
export const authFetch = async (url, options = {}) => {
    const res = await fetch(url, {
        ...options,
        credentials: 'include',
        headers: {
            ...(options.headers || {}),
        },
    });

    if (res.status === 401) {
        clearLoginFlag();
        window.location.href = '/login';
    }

    return res;
};

/**
 * 로그아웃 — 백엔드 쿠키 삭제 후 플래그 초기화
 */
export const logout = async () => {
    await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' });
    clearLoginFlag();
    window.location.href = '/login';
};

// 하위 호환 유지용 (현재 미사용 — 삭제 가능)
export const getToken = () => null;
export const setToken = () => {};
export const removeToken = clearLoginFlag;
