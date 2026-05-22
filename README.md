# AR 기반 문화관광 시설 내비게이션 시스템

> Unity AR Foundation + Immersal VPS 기반  
> 문화관광 시설 전용 실내 AR 내비게이션 시스템

![Unity](https://img.shields.io/badge/Unity-2022.3-black?logo=unity)
![AR Foundation](https://img.shields.io/badge/AR%20Foundation-4.2-blue)
![React](https://img.shields.io/badge/React-61DAFB?logo=react&logoColor=black)
![NestJS](https://img.shields.io/badge/NestJS-E0234E?logo=nestjs&logoColor=white)
![Supabase](https://img.shields.io/badge/Supabase-3FCF8E?logo=supabase&logoColor=white)
![GitHub](https://img.shields.io/badge/GitHub-181717?logo=github&logoColor=white)
![VPS](https://img.shields.io/badge/VPS-Immersal-orange)
![AR Navigation](https://img.shields.io/badge/AR-Navigation-blueviolet)

---

# 🎬 시연 영상

## 내비게이터 시스템 영상 Part 1

(영상 링크 넣을것1)

---

## 내비게이터 시스템 영상 Part 2

(영상 링크 넣을것2)

---

## AR 내비게이션 사진

(사진들 기입)

---

# 목차

- [1. 프로젝트 소개](#프로젝트-소개)
- [2. 기존 문제점](#기존-문제점)
- [3. 해결 방법](#해결-방법)
- [4. 기대 효과](#기대-효과)
- [5. 기술 스택](#기술-스택)
- [6. 시스템 구조](#시스템-구조)
- [7. 주요 기능](#주요-기능)
- [8. 내비게이터 시스템 영상 Part 1](#내비게이터-시스템-영상-Part-1)
- [9. 내비게이터 시스템 영상 Part 2](#내비게이터-시스템-영상-Part-2)
- [10. 개선 사항](#개선-사항)
- [11. 프로젝트 회고](#프로젝트-회고)

---

# 프로젝트 소개

본 프로젝트는 문화관광 시설 내부에서 사용자가 목적지를 쉽게 찾을 수 있도록 돕는 AR 기반 실내 내비게이션 시스템입니다.

Immersal SDK 기반 VPS(Visual Positioning System)를 활용하여 사용자의 실내 위치를 인식하고, Unity NavMesh를 통해 사용자 위치에서 목적지까지의 최적 경로를 실시간으로 생성합니다.

또한 AR 화살표 오브젝트를 통해 사용자가 직관적으로 길을 찾을 수 있도록 지원하며, React 기반 관리자 페이지와 Supabase 데이터베이스를 연동하여 전시품 및 공간 데이터를 효율적으로 관리할 수 있도록 구현하였습니다.

---

# 기존 문제점

기존 문화관광 시설 안내 시스템은 다음과 같은 문제점이 존재합니다.

- 실내 공간에서 GPS 기반 위치 인식의 정확도가 낮음
- 복잡한 시설 구조로 인해 사용자 길찾기 어려움
- 종이 지도 및 텍스트 기반 안내의 낮은 직관성
- 시설 정보 변경 시 즉각적인 반영 어려움
- 비콘, LiDAR 등 별도 장비 기반 시스템 구축 시 높은 비용 발생

---

# 해결 방법

본 프로젝트는 Immersal SDK 기반 VPS 기술을 활용하여 실내 공간에서 사용자의 위치를 인식합니다.

Unity NavMesh를 이용하여 현재 위치에서 목적지까지의 최적 경로를 계산하고, 실시간 길안내 서비스를 제공합니다.

또한 React 기반 관리자 페이지와 Supabase 데이터베이스를 연동하여 전시품 위치 및 공간 데이터를 실시간으로 관리할 수 있도록 구현하였습니다.

---

# 기대 효과

- AR 기반 직관적 길안내 제공
- 문화관광 시설 관람 경험 향상 및 디지털 전시 경험(DX) 제공
- 관리자 중심 실시간 데이터 관리 가능
- 별도 비콘 및 LiDAR 장비 의존도를 줄여 구축 및 유지보수 비용 절감
- 방문객 이동 데이터 기반 시설 운영 효율화
- 카메라 성능에 관계 없이 다양한 모바일 환경에서 사용 가능

---

# 기술 스택

| 분야 | 기술 |
|---|---|
| Front-End(Web) | React |
| Front-End(App) | Unity |
| Back-End | NestJS |
| AR | AR Foundation, Immersal SDK, VPS, NavMesh |
| Database | Supabase |
| Deployment | Vercel, Render |
| Environment | Android |
| Collaboration | GitHub, Figma |

---

# 시스템 구조

```text
사용자
 ↓
Unity AR App
 ↓
Immersal VPS 위치 인식
 ↓
NavMesh 경로 탐색
 ↓
WayPoint 기반 경로 생성
 ↓
AR 화살표 렌더링
 ↓
실시간 길안내

──────────────────

관리자 웹 페이지 (React)
 ↓
NestJS API Server
 ↓
Supabase Database
