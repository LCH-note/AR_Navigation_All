</h1>

<p align="center">
문화관광 시설 전용 실내 AR 내비게이션 플랫폼
</p>

<br>

<p align="center">
  <img src="https://skillicons.dev/icons?i=unity,react,nodejs,github,figma"/>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/AR%20Foundation-4.2-blue">
  <img src="https://img.shields.io/badge/VPS-Immersal-orange">
  <img src="https://img.shields.io/badge/Database-Supabase-3FCF8E">
  <img src="https://img.shields.io/badge/Deploy-Vercel-black">
  <img src="https://img.shields.io/badge/Backend-Render-46E3B7">
</p>

<br>

<p align="center">
Unity AR Foundation와 Immersal SDK를 활용한<br>
실내 공간 기반 AR 내비게이션 시스템
</p>

---

# 1. 시연 영상

## 시스템 시연 영상 Part 1
https://github.com/user-attachments/assets/33aa637f-f5e8-4dd2-ba58-5706bf65100e

---

## 시스템 시연 영상 Part 2
https://github.com/user-attachments/assets/83a3385e-1df8-434d-bd19-296f33745eeb

---

<img width="1080" height="2340" alt="캡스톤 AR 내비게이션 사진 7" src="https://github.com/user-attachments/assets/f511d637-68be-4548-8348-2f77db70ad03" />
<img width="1080" height="2340" alt="캡스톤 AR 내비게이션 사진 6" src="https://github.com/user-attachments/assets/765c0f94-4e69-4f66-9f9e-9803ff980168" />
<img width="481" height="866" alt="캡스톤 AR 내비게이션 사진 5" src="https://github.com/user-attachments/assets/2d4f4c74-5076-46f8-9251-7557443d0f69" />
<img width="476" height="865" alt="캡스톤 AR 내비게이션 사진 4" src="https://github.com/user-attachments/assets/88a2fec8-8954-4b15-81fa-324ffcc607a8" />
<img width="480" height="864" alt="캡스톤 AR 내비게이션 사진 3" src="https://github.com/user-attachments/assets/b3af64c0-0fd4-4cdf-b47d-1027fd16217a" />
<img width="483" height="874" alt="캡스톤 AR 내비게이션 사진 2" src="https://github.com/user-attachments/assets/086d031a-e030-469f-812f-151a486ccf67" />
<img width="477" height="876" alt="캡스톤 AR 내비게이션 사진 1" src="https://github.com/user-attachments/assets/d6ad4c08-b49a-4b3f-a8a8-d769b1beaffc" />
<img width="1080" height="2340" alt="캡스톤 AR 내비게이션 사진 8" src="https://github.com/user-attachments/assets/2a1da042-594e-4c82-ae98-7bde2adcd255" />
<img width="2115" height="940" alt="캡스톤 AR 내비게이션 사진 9" src="https://github.com/user-attachments/assets/cb004ec7-191e-4213-a0bf-b2a7546c1273" /><h1 align="center">
AR Indoor Navigation System
<img width="481" height="866" alt="캡스톤 AR 내비게이션 사진 10" src="https://github.com/user-attachments/assets/65b9f64e-1a47-4473-b602-17d4c77746e9" />
<img width="1080" height="2340" alt="캡스톤 AR 내비게이션 사진 12" src="https://github.com/user-attachments/assets/2e1a8e90-5c5c-4876-82e5-e7d5afec4aee" />
<img width="1080" height="2340" alt="캡스톤 AR 내비게이션 사진 11" src="https://github.com/user-attachments/assets/ebc212fb-85df-44d2-9edb-6ad92f3c16b0" />






# 2. 목차

- [프로젝트 소개](#3-프로젝트-소개)
- [기존 문제점](#4-기존-문제점)
- [해결 방법](#5-해결-방법)
- [기대 효과](#6-기대-효과)
- [기술 스택](#7-기술-스택)
- [시스템 구조](#8-시스템-구조)
- [개선 사항](#9-개선-사항)

---

# 3. 프로젝트 소개

본 프로젝트는 문화관광 시설 내부에서 사용자가 목적지를 보다 직관적으로 탐색할 수 있도록 지원하는 AR 기반 실내 내비게이션 시스템입니다.

Immersal SDK 기반 VPS(Visual Positioning System)를 활용하여 사용자의 실내 위치를 인식하고, Unity NavMesh를 이용해 현재 위치에서 목적지까지의 최적 경로를 실시간으로 생성합니다.

또한 AR 화살표 오브젝트를 통해 사용자가 직관적으로 이동 경로를 확인할 수 있도록 구현하였으며, React 기반 관리자 페이지와 Supabase 데이터베이스를 연동하여 전시품 및 공간 데이터를 효율적으로 관리할 수 있도록 구성하였습니다.

---

# 4. 기존 문제점

기존 문화관광 시설 안내 시스템은 다음과 같은 한계가 존재합니다.

- 실내 환경에서 GPS 기반 위치 인식 정확도가 낮음
- 복잡한 실내 구조로 인해 사용자 길찾기 어려움 발생
- 종이 지도 및 텍스트 기반 안내 방식의 낮은 직관성
- 시설 정보 변경 시 실시간 반영의 어려움
- 비콘 및 LiDAR 기반 시스템 구축 시 높은 비용 발생

---

# 5. 해결 방법

본 프로젝트는 Immersal SDK 기반 VPS 기술을 활용하여 실내 공간에서 사용자의 위치를 인식합니다.

Unity NavMesh를 이용해 현재 위치에서 목적지까지의 최적 경로를 계산하며, AR 화살표 오브젝트를 통해 실시간 길안내 서비스를 제공합니다.

또한 React 기반 관리자 페이지와 Supabase 데이터베이스를 연동하여 전시품 위치 및 공간 데이터를 실시간으로 관리할 수 있도록 구현하였습니다.

---

# 6. 기대 효과

- AR 기반 실내 길안내와 디지털 전시 경험 제공을 통해 문화관광 시설 관람 편의성 및 사용자 경험 향상
- 웹 관리자 시스템을 활용하여 실내 공간 데이터 및 전시 정보를 실시간으로 효율적으로 관리 가능
- 비콘, AP 등 별도 물리적 하드웨어 설치 의존도를 줄여 초기 구축 비용 및 장기적인 유지보수 비용 절감
- 방문객 이동 동선 및 주요 관심 구역 데이터를 기반으로 효율적인 관람 동선 설계 및 시설 운영 최적화 가능
- 특수 장비 없이 다양한 스마트폰 및 모바일 환경에서 안정적인 AR 서비스 제공 가능

# 7. 기술 스택

| 분야 | 기술 |
|---|---|
| Front-End(Web) | React |
| Front-End(App) | Unity, AR Foundation, Immersal SDK |
| Back-End | NestJS |
| Database | Supabase |
| Deployment | Vercel, Render |
| Collaboration | GitHub, Figma |

---

# 8. 시스템 구조

| 사용자 시스템 | 관리자 시스템 |
|---|---|
| 사용자 | 관리자 웹 페이지 (React) |
| ↓ | ↓ |
| Unity AR App | NestJS API Server |
| ↓ | ↓ |
| Immersal SDK 위치 인식 | Supabase Database |
| ↓ |  |
| NavMesh 경로 탐색 |  |
| ↓ |  |
| WayPoint 기반 경로 생성 |  |
| ↓ |  |
| AR 화살표 렌더링 |  |
| ↓ |  |
| 실시간 길안내 |  |

---

# 9. 개선 사항

- **공간 데이터 맵핑 정밀도 향상**  
  공간 데이터 크기 제한으로 인해 하나의 공간을 분할 맵핑한 뒤 공통 구조물을 기준으로 정렬(Calibration)하여 구현하였습니다.  
  향후에는 고해상도 대용량 맵핑 프로세스를 적용하여 복합 공간 정합 과정에서 발생하는 오차를 최소화하고, 공간 데이터 정밀도를 더욱 향상시킬 예정입니다.

- **클라우드 인프라 통합 및 최적화**  
  현재는 운영 비용 절감을 위해 Frontend(Vercel)와 Backend(Render)를 분리하여 배포 및 운영하고 있습니다.  
  향후에는 AWS(Amazon Web Services) 기반 단일 클라우드 아키텍처로 통합하여 인프라 관리 효율성과 시스템 안정성을 향상시킬 계획입니다.

- **AR 콘텐츠 확장**  
  사용자 경험(UX) 향상과 서비스 활성화를 위해 실내 내비게이션 기능 외에도 공간 맞춤형 인터랙션 등 다양한 AR 콘텐츠를 추가적으로 확장할 예정입니다.
