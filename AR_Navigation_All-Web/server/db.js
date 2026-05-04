const mysql = require("mysql2");

const connection = mysql.createPool({
    host: "localhost",
    user: "root", // 또는 본인이 생성한 사용자 (예: dev_user)
    password: "", // MySQL 비밀번호 입력
    database: "museum", // 사용 중인 데이터베이스 이름
    waitForConnections: true,
    connectionLimit: 10,
    queueLimit: 0,
});

module.exports = connection.promise(); // 비동기 처리를 위해 promise() 사용
