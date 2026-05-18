import { Routes, Route, Navigate, useLocation } from "react-router-dom";
import Home from "./pages/Home";
import Content from "./pages/Content";
import Space from "./pages/Space";
import SpaceEditor from "./pages/SpaceEditor";
import SpaceMap from "./pages/SpaceMap";
import SpaceRoute from "./pages/SpaceRoute";
import User from "./pages/User";
import Login from "./pages/Login";
import { isAuthenticated } from "./utils/auth";

// 미인증 시 로그인 페이지로 리다이렉트, 복귀 경로 state에 보존
function PrivateRoute({ children }) {
    const location = useLocation();
    if (!isAuthenticated()) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }
    return children;
}

function App() {
    return (
        <div className="App">
            <Routes>
                {/* 로그인 페이지 (인증 불필요) */}
                <Route path="/login" element={<Login />} />

                {/* 메인 페이지 */}
                <Route
                    path="/"
                    element={
                        <PrivateRoute>
                            <Home />
                        </PrivateRoute>
                    }
                />

                {/* 공간 관리 페이지 — 하위 탭 라우트 포함 */}
                <Route
                    path="/space"
                    element={
                        <PrivateRoute>
                            <Space />
                        </PrivateRoute>
                    }
                >
                    <Route index element={<Navigate to="/space/editor" replace />} />
                    <Route path="editor" element={<SpaceEditor />} />
                    <Route path="map" element={<SpaceMap />} />
                    <Route path="route" element={<SpaceRoute />} />
                </Route>

                {/* 전시콘텐츠 관리 페이지 */}
                <Route
                    path="/content"
                    element={
                        <PrivateRoute>
                            <Content />
                        </PrivateRoute>
                    }
                />

                {/* 사용자 리뷰 관리 페이지 */}
                <Route
                    path="/user"
                    element={
                        <PrivateRoute>
                            <User />
                        </PrivateRoute>
                    }
                />
            </Routes>
        </div>
    );
}

export default App;
