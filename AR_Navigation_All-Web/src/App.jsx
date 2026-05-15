import { Routes, Route, Navigate } from "react-router-dom";
import Home from "./pages/Home";
import Content from "./pages/Content";
import Space from "./pages/Space";
import SpaceEditor from "./pages/SpaceEditor";
import SpaceMap from "./pages/SpaceMap";
import SpaceRoute from "./pages/SpaceRoute";
import User from "./pages/User";

function App() {
    return (
        <div className="App">
            <Routes>
                {/* 메인 페이지 */}
                <Route path="/" element={<Home />} />

                {/* 공간 관리 페이지 — 하위 탭 라우트 포함 */}
                <Route path="/space" element={<Space />}>
                    {/* 기본 진입 시 지도 에디터로 리다이렉트 */}
                    <Route index element={<Navigate to="/space/editor" replace />} />
                    <Route path="editor" element={<SpaceEditor />} />
                    <Route path="map" element={<SpaceMap />} />
                    <Route path="route" element={<SpaceRoute />} />
                </Route>

                {/* 콘텐츠 관리 페이지 */}
                <Route path="/content" element={<Content />} />

                {/* 리뷰 관리 페이지 */}
                <Route path="/user" element={<User />} />
            </Routes>
        </div>
    );
}

export default App;
