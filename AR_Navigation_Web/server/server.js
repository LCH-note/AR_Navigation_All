// server.js
const express = require("express");
const cors = require("cors");
const multer = require("multer");
const path = require("path");
const db = require("./db");
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

// [API] 전시품 등록
app.post("/api/display", upload.single("image"), async (req, res) => {
    try {
        // 데이터가 넘어오지 않을 경우를 대비해 기본값 설정 (ER_BAD_NULL_ERROR 방지)
        const {
            title,
            feature,
            contents,
            ar_marker_id = "N/A",
            pos_x = 0,
            pos_y = 0,
            floor_info = "Museum 1F",
        } = req.body;

        const imagePath = req.file ? `/uploads/${req.file.filename}` : null;

        const sql = `INSERT INTO display (title, feature, contents, ar_marker_id, pos_x, pos_y, floor_info, image_path) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`;

        // 쿼리 실행
        const [result] = await db.query(sql, [
            title,
            feature,
            contents,
            ar_marker_id,
            pos_x,
            pos_y,
            floor_info,
            imagePath,
        ]);

        res.status(201).json({
            message: "저장 성공!",
            data: { id: result.insertId, title },
        });
    } catch (error) {
        // 서버 터미널에서 구체적인 SQL 에러를 확인할 수 있도록 출력
        console.error("DB Error:", error.sqlMessage || error);
        res.status(500).json({ message: "서버 에러 발생", error: error.sqlMessage });
    }
});

app.get("/api/display", async (req, res) => {
    try {
        const [rows] = await db.query("SELECT * FROM display ORDER BY id DESC");
        res.json(rows);
    } catch (error) {
        res.status(500).json({ message: "조회 실패", error });
    }
});

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

app.listen(PORT, () => {
    console.log(`Server running on http://localhost:${PORT}`);
});
