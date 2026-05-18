const TOKEN_KEY = 'ar_admin_token';

export const getToken = () => localStorage.getItem(TOKEN_KEY);
export const setToken = (token) => localStorage.setItem(TOKEN_KEY, token);
export const removeToken = () => localStorage.removeItem(TOKEN_KEY);
export const isAuthenticated = () => !!localStorage.getItem(TOKEN_KEY);

// 인증이 필요한 fetch 요청에 사용
export const authFetch = (url, options = {}) => {
    const token = getToken();
    return fetch(url, {
        ...options,
        headers: {
            ...(options.headers || {}),
            Authorization: `Bearer ${token}`,
        },
    });
};
