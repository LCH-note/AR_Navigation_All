import { Routes, Route } from "react-router-dom";
import Home from "./pages/Home";
import Content from "./pages/Content";
import Space from "./pages/Space";
import User from "./pages/User";

function App() {
    return (
        <div className="App">
            <Routes>
                {/* 메인 페이지 */}
                <Route path="/" element={<Home />} />

                {/* 공간 관리 페이지 */}
                <Route path="/space" element={<Space />} />

                {/* 콘텐츠 관리 페이지 */}
                <Route path="/content" element={<Content />} />

                {/* 리뷰 관리 페이지 */}
                <Route path="/user" element={<User />} />
            </Routes>
        </div>
    );
}

export default App;
