// server.js
const express = require("express");
const cors = require("cors");
const multer = require("multer");
const path = require("path");
const db = require("./db"); // 기존 설정하신 DB 모듈
const app = express();
const PORT = 3000;

app.use(
    cors({
        origin: ["http://localhost:3000", "http://localhost:3001"],
        credentials: true,
        methods: ["GET", "POST", "DELETE", "OPTIONS"],
    }),
);

app.use(express.json());
app.use("/uploads", express.static("uploads"));

const storage = multer.diskStorage({
    destination: (req, file, cb) => {
        cb(null, "uploads/");
    },
    filename: (req, file, cb) => {
        const uniqueSuffix = Date.now() + "-" + Math.round(Math.random() * 1e9);
        cb(null, file.fieldname + "-" + uniqueSuffix + path.extname(file.originalname));
    },
});
const upload = multer({ storage: storage });

// ==========================================
// [API] 전시품(Display) 관련 라우터
// ==========================================

// [API] 전시품 등록
app.post("/api/display", upload.single("image"), async (req, res) => {
    try {
        const {
            title,
            feature,
            contents,
            ar_marker_id = "N/A",
            pos_x = 0,
            pos_z = 0,
            floor_info = "Museum 1F",
        } = req.body;

        const imagePath = req.file ? `/uploads/${req.file.filename}` : null;

        const sql = `INSERT INTO display (title, feature, contents, ar_marker_id, pos_x, pos_z, floor_info, image_path) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`;

        const [result] = await db.query(sql, [
            title,
            feature,
            contents,
            ar_marker_id,
            pos_x,
            pos_z,
            floor_info,
            imagePath,
        ]);

        res.status(201).json({
            message: "저장 성공!",
            data: { id: result.insertId, title },
        });
    } catch (error) {
        console.error("DB Error:", error.sqlMessage || error);
        res.status(500).json({ message: "서버 에러 발생", error: error.sqlMessage });
    }
});

// [API] 전시품 조회
app.get("/api/display", async (req, res) => {
    try {
        const [rows] = await db.query("SELECT * FROM display ORDER BY id DESC");
        res.json(rows);
    } catch (error) {
        res.status(500).json({ message: "조회 실패", error });
    }
});

// [API] 전시품 삭제
app.delete("/api/display/:id", async (req, res) => {
    try {
        const { id } = req.params;
        const sql = "DELETE FROM display WHERE id = ?";
        const [result] = await db.query(sql, [id]);

        if (result.affectedRows === 0) {
            return res.status(404).json({ message: "데이터를 찾을 수 없습니다." });
        }
        res.json({ message: "삭제 성공" });
    } catch (error) {
        console.error(error);
        res.status(500).json({ message: "삭제 실패", error });
    }
});

// ==========================================
// [API] 리뷰(Reviews) 관련 라우터
// ==========================================

// [API] 리뷰 목록 조회 (프론트엔드 관리자 페이지용)
app.get("/api/reviews", async (req, res) => {
    try {
        // 프론트엔드에서 '전시품명'이 제거되었으므로,
        // 불필요한 JOIN 구문을 빼고 리뷰 테이블만 단독으로 가볍게 조회하도록 수정했습니다.
        const sql = `
            SELECT 
                review_id, 
                nickname, 
                star, 
                content, 
                created_at
            FROM 
                reviews
            ORDER BY 
                created_at DESC;
        `;

        const [rows] = await db.query(sql);
        res.status(200).json(rows);
    } catch (error) {
        console.error("리뷰 조회 에러:", error.sqlMessage || error);
        res.status(500).json({ message: "리뷰 목록 조회 실패", error: error.sqlMessage });
    }
});

// [API] 새로운 리뷰 등록 (일반 관람객 사용 앱/웹용)
app.post("/api/reviews", async (req, res) => {
    try {
        const { display_id, nickname = "익명", star, content } = req.body;

        // 데이터 유효성 검사
        if (!display_id || !star || !content) {
            return res.status(400).json({ message: "필수 정보(display_id, star, content)가 누락되었습니다." });
        }

        const sql = `INSERT INTO reviews (display_id, nickname, star, content, created_at) VALUES (?, ?, ?, ?, NOW())`;
        const [result] = await db.query(sql, [display_id, nickname, star, content]);

        res.status(201).json({
            message: "리뷰 저장 성공!",
            data: { review_id: result.insertId },
        });
    } catch (error) {
        console.error("리뷰 등록 에러:", error.sqlMessage || error);
        res.status(500).json({ message: "리뷰 등록 실패", error: error.sqlMessage });
    }
});

// ==========================================
// 서버 실행
// ==========================================
app.listen(PORT, () => {
    console.log(`Server running on http://localhost:${PORT}`);
});
