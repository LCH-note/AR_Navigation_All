# [AR 기반 문화관광시설 내비게이션 시스템]

> Unity AR Foundation + Immersal VPS 기반  
> 실내 문화관광 시설 AR 내비게이션 서비스

![Unity](https://img.shields.io/badge/Unity-2022.3-black?logo=unity)
![React](https://img.shields.io/badge/React-61DAFB?logo=react&logoColor=black)
![Node.js](https://img.shields.io/badge/Node.js-339933?logo=node.js&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-4479A1?logo=mysql&logoColor=white)
![GitHub](https://img.shields.io/badge/GitHub-181717?logo=github&logoColor=white)
---

# 🧭 AR 기반 문화관광 시설 내비게이션 시스템

> Unity AR Foundation + Immersal VPS 기반  
> 문화관광 시설 전용 실내 AR 내비게이션 시스템

![Unity](https://img.shields.io/badge/Unity-2022.3-black?logo=unity)
![AR Foundation](https://img.shields.io/badge/AR%20Foundation-4.2-blue)
![React](https://img.shields.io/badge/React-61DAFB?logo=react&logoColor=black)
![Node.js](https://img.shields.io/badge/Node.js-339933?logo=node.js&logoColor=white)
![Supabase](https://img.shields.io/badge/Supabase-3FCF8E?logo=supabase&logoColor=white)
![GitHub](https://img.shields.io/badge/GitHub-181717?logo=github&logoColor=white)

---

# 🎬 시연 영상

## 📌 Part 1. 프로젝트 소개 및 주요 기능

(영상 링크 삽입)

---

## 📌 Part 2. AR 내비게이션 시연

(영상 링크 삽입)

---

# 📑 목차

- [📌 프로젝트 소개](#-프로젝트-소개)
- [🧩 기존 문제점](#-기존-문제점)
- [💡 해결 방법](#-해결-방법)
- [🎯 기대 효과](#-기대-효과)
- [🛠 기술 스택](#-기술-스택)
- [🧭 시스템 구조](#-시스템-구조)
- [📱 주요 기능](#-주요-기능)
- [🧭 AR 내비게이션](#-ar-내비게이션)
- [📸 실제 동작 화면](#-실제-동작-화면)
- [👥 팀원 소개](#-팀원-소개)
- [🔧 개선 사항](#-개선-사항)

---

# 📌 프로젝트 소개

본 프로젝트는 문화관광 시설 내부에서 사용자가 목적지를 쉽게 찾을 수 있도록 돕는 AR 기반 실내 내비게이션 시스템입니다.

Immersal SDK 기반 VPS(Visual Positioning System)를 활용하여 사용자의 실내 위치를 인식하고, Unity NavMesh를 통해 목적지까지의 최적 경로를 생성합니다.

또한 AR 화살표 오브젝트를 통해 사용자가 직관적으로 길을 찾을 수 있도록 지원하며, 웹 기반 관리 시스템을 통해 전시품 및 공간 데이터를 효율적으로 관리할 수 있습니다.

---

# 🧩 기존 문제점

기존 문화관광 시설 안내 시스템은 다음과 같은 문제점이 존재합니다.

- 실내 공간에서 GPS 기반 위치 인식의 정확도가 낮음
- 복잡한 시설 구조로 인해 사용자 길찾기 어려움
- 종이 지도 및 텍스트 기반 안내의 낮은 직관성
- 시설 정보 변경 시 즉각적인 반영 어려움
- 별도 비콘 및 LiDAR 장비 구축 비용 발생

---

# 💡 해결 방법

본 프로젝트는 Immersal SDK 기반 VPS 기술을 활용하여 실내 공간에서 사용자의 위치를 인식합니다.

Unity NavMesh를 이용하여 현재 위치에서 목적지까지의 최적 경로를 계산하고, AR 화살표 오브젝트를 통해 실시간 길안내 서비스를 제공합니다.

또한 웹 기반 관리자 페이지를 통해 전시품 위치 및 공간 데이터를 실시간으로 관리할 수 있도록 구현하였습니다.

---

# 🎯 기대 효과

- AR 기반 직관적 길안내 제공
- 문화관광 시설 관람 경험 향상
- 관리자 중심 실시간 데이터 관리 가능
- 유지보수 비용 절감
- 방문객 이동 데이터 기반 시설 운영 효율화
- 디지털 전시 경험(DX) 제공

---

# 🛠 기술 스택

| 분야 | 기술 |
|---|---|
| Front-End(Web) | React |
| Front-End(App) | Unity |
| Back-End | Node.js |
| AR | AR Foundation, Immersal SDK, NavMesh |
| Database | Supabase |
| Collaboration | GitHub, Figma |

---

# 🧭 시스템 구조

```text
사용자
 ↓
Immersal VPS 위치 인식
 ↓
Unity AR Foundation
 ↓
NavMesh 경로 탐색
 ↓
WayPoint 기반 경로 생성
 ↓
AR 화살표 렌더링
 ↓
실시간 길안내
