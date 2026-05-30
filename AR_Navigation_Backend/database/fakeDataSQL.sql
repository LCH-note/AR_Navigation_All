  -- ============================================================
  -- AR Navigation 테스트 데이터 삽입
  -- Supabase SQL 편집기에서 ①②③ 순서대로 실행하세요
  -- 초기화가 필요하면 아래 주석 해제 후 먼저 실행:
  -- DELETE FROM reviews; DELETE FROM visitors; DELETE FROM artworks;
  -- ============================================================


  -- ① 전시품 450개
  INSERT INTO artworks (title, artist, feature, contents, ar_marker_id, pos_x, pos_z, floor_info, map_index)
  SELECT
      장르 || ' 작품 ' || i,
      작가,
      장르 || ' 계열 · ' || 특징,
      '작가 ' || 작가 || '의 대표작으로, ' || 설명 || '. 관람객들에게 깊은 예술적 감동을 주는 작품입니다.',
      'ART_' || LPAD(i::text, 4, '0'),
      ((random() * 8 - 4)::numeric(6,3))::text,
      ((random() * 8 - 4)::numeric(6,3))::text,
      층,
      (i - 1) % 2
  FROM (
      SELECT
          i,
          (ARRAY['회화', '조각', '사진', '설치미술', '미디어아트', '공예', '판화', '드로잉'])[(i % 8) + 1] AS 장르,
          (ARRAY[
              '김민준', '이서연', '박지원', '최수현', '정민호',
              '강예은', '조현우', '윤지아', '임태양', '한소희',
              '신동현', '오지수', '문채원', '배성민', '류하은',
              '안재현', '황미래', '전지후', '손예지', '노승현'
          ])[(i % 20) + 1] AS 작가,
          (ARRAY[
              '자연의 아름다움', '도시의 일상', '인간의 감정', '시대의 흐름',
              '문화의 다양성', '생명의 순환', '공간의 변형', '빛과 그림자'
          ])[(i % 8) + 1] AS 특징,
          (ARRAY[
              '독창적인 색감과 구도가 돋보임',
              '섬세한 표현 기법으로 완성된 걸작',
              '강렬한 메시지와 상징성이 담긴 작품',
              '관람객의 상상력을 자극하는 독특한 작품'
          ])[(i % 4) + 1] AS 설명,
          -- 1F 비중을 가장 높게 설정
          (ARRAY[
              'Museum 1F', 'Museum 1F', 'Museum 1F',
              'Museum 2F', 'Museum 2F',
              'Museum 3F',
              'Museum B1'
          ])[(i % 7) + 1] AS 층
      FROM generate_series(1, 450) AS i
  ) sub;


  -- ② 방문객 6200명
  -- 연령대 비중: 20대(29%) > 30대(24%) > 40대(18%) > 10대·50대(12%) > 60대 이상(6%)
  INSERT INTO visitors (device_id, visited_at, age_group)
  SELECT
      'DEV' || LPAD(i::text, 7, '0'),
      NOW() - (floor(random() * 180)::int || ' days')::interval
            - (floor(random() * 24)::int  || ' hours')::interval,
      (ARRAY[
          '10대', '10대',
          '20대', '20대', '20대', '20대', '20대',
          '30대', '30대', '30대', '30대',
          '40대', '40대', '40대',
          '50대', '50대',
          '60대 이상'
      ])[(floor(random() * 17) + 1)::int]
  FROM generate_series(1, 6200) AS i;


  -- ③ 리뷰 4960개 (방문객의 80% · 평균 ≈ 4.8점)
  -- 별점 분포: 5점 85% + 4점 10% + 3점 5% → 기댓값 4.80
  WITH artwork_ids AS (
      SELECT id, (row_number() OVER ())::int AS rn FROM artworks
  ),
  artwork_cnt AS (
      SELECT COUNT(*)::int AS n FROM artworks
  ),
  review_input AS (
      SELECT
          i,
          ((i - 1) % (SELECT n FROM artwork_cnt)) + 1 AS artwork_rn,
          random() AS r1,   -- 5점 vs 나머지 판정
          random() AS r2,   -- 4점 vs 3점 판정 (r1 < 0.85 아닐 때만 사용)
          (floor(random() * 20) + 1)::int AS comment_idx,
          (floor(random() * 10) + 1)::int AS nick_idx
      FROM generate_series(1, 4960) AS i
  )
  INSERT INTO reviews (artwork_id, rating, content, nickname)
  SELECT
      a.id,
      CASE
          WHEN ri.r1 < 0.85  THEN 5   -- 85%
          WHEN ri.r2 < 0.667 THEN 4   -- 10%  (0.667 × 나머지 15%)
          ELSE 3                       -- 5%
      END,
      (ARRAY[
          '정말 인상적인 작품이었습니다!',
          '아름다운 전시였어요. 강력 추천합니다.',
          '작가의 의도가 잘 느껴지는 훌륭한 작품입니다.',
          '다시 방문하고 싶은 특별한 전시입니다.',
          '감동적인 작품이었습니다. 오래 기억에 남을 것 같아요.',
          '색감과 구도가 매우 훌륭했습니다.',
          '예술적 영감을 받을 수 있었어요.',
          '가족과 함께 관람하기 정말 좋았습니다.',
          '작품 설명이 매우 유익하고 이해하기 쉬웠어요.',
          '이런 전시를 더 자주 열어주세요!',
          '현대 예술의 새로운 면을 발견할 수 있었습니다.',
          'AR 기술과의 결합이 정말 신선하고 좋았어요.',
          '작품 하나하나에 작가의 정성이 느껴졌습니다.',
          '전시 공간 구성도 작품만큼 훌륭했습니다.',
          '기억에 남는 특별한 문화 경험이었습니다.',
          '예상보다 훨씬 좋았어요. 꼭 한번 더 오고 싶습니다.',
          '작가의 독특한 세계관에 매료되었습니다.',
          '아이들과 함께 왔는데 교육적으로도 훌륭했어요.',
          '사진 찍기 정말 좋은 전시였습니다.',
          '이 작품 덕분에 하루가 행복했습니다.'
      ])[ri.comment_idx],
      (ARRAY[
          '관람객', '예술애호가', '학생', '직장인', '가족방문객',
          '미술전공자', '일반관람객', '익명', '단체관람', '외국인관람객'
      ])[ri.nick_idx]
  FROM review_input ri
  JOIN artwork_ids a ON a.rn = ri.artwork_rn;


  -- ✅ 검증 쿼리 (삽입 완료 후 확인용)
  SELECT
      (SELECT COUNT(*) FROM artworks) AS 전시품수,
      (SELECT COUNT(*) FROM visitors) AS 방문객수,
      (SELECT COUNT(*) FROM reviews)  AS 리뷰수,
      (SELECT ROUND(AVG(rating)::numeric, 2) FROM reviews) AS 평균별점;