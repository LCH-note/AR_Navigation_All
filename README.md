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

## 1.1 시스템 시연 영상 Part 1

https://github.com/user-attachments/assets/d49bee32-13b6-4d47-9839-29ae484d7aec

* 문화관광 시설 내부에서 길을 찾지 못하는 사용자를 위한 AR 실내 내비게이션 시스템 시연 영상
* VPS 기반 실내 위치 인식과 AR 화살표 안내 기능을 통해 사용자의 이동 방향에 맞는 실시간 경로 안내 제공

---

# 2. 목차

* [애플리케이션 화면 구성](#3-애플리케이션-화면-구성)
* [AR 내비게이션 시스템](#4-ar-내비게이션-시스템)
* [관리자 웹 시스템](#5-관리자-웹-시스템)
* [프로젝트 소개](#6-프로젝트-소개)
* [기존 문제점](#7-기존-문제점)
* [해결 방법](#8-해결-방법)
* [기대 효과](#9-기대-효과)
* [기술 스택](#10-기술-스택)
* [시스템 구조](#11-시스템-구조)
* [개선 사항](#12-개선-사항)

---

# 3. 애플리케이션 화면 구성

## 3.1 메인 화면

<p align="center">
<img height="390" alt="캡스톤 AR 내비게이션 사진 1" src="https://github.com/user-attachments/assets/d6ad4c08-b49a-4b3f-a8a8-d769b1beaffc" />
<img height="390" alt="캡스톤 AR 내비게이션 사진 2" src="https://github.com/user-attachments/assets/086d031a-e030-469f-812f-151a486ccf67" />
<img height="390" alt="캡스톤 AR 내비게이션 사진 3" src="https://github.com/user-attachments/assets/b3af64c0-0fd4-4cdf-b47d-1027fd16217a" />
<img height="390" alt="캡스톤 AR 내비게이션 사진 5" src="https://github.com/user-attachments/assets/2d4f4c74-5076-46f8-9251-7557443d0f69" />
</p>
<p align="center">
애플리케이션 메인 화면, 전체 지도 화면, 경로 선택 화면 및 전시품 선택 화면입니다.<br>
사용자는 전체 실내 지도를 확인하고 추천 관람 경로 또는 원하는 전시품을 선택하여 맞춤형 AR 내비게이션 서비스를 이용할 수 있습니다.
</p>

---

# 4. AR 내비게이션 시스템

## 4.1 VPS 공간 맵핑

<p align="center">
<img width="250" alt="캡스톤 AR 내비게이션 사진 7" src="https://github.com/user-attachments/assets/59bc411d-18af-42a4-8b06-ad15ae69f0b5" />
<img width="250" alt="캡스톤 AR 내비게이션 사진 8" src="https://github.com/user-attachments/assets/2a826674-52a9-4862-96c7-71b6771e0167" />
</p>

<p align="center">
Immersal SDK를 활용하여 실내 공간의 특징점을 수집한 화면입니다.<br>
수집된 Point Cloud 데이터는 VPS 기반 위치 인식에 사용됩니다.
</p>

---

# 4-2 AR 내비게이션 시스템

<p align="center">
<img height="520" alt="캡스톤 AR 내비게이션 사진 12" src="https://github.com/user-attachments/assets/2e1a8e90-5c5c-4876-82e5-e7d5afec4aee" />
<img height="520" alt="캡스톤 AR 내비게이션 사진 11" src="https://github.com/user-attachments/assets/7380309a-d91e-4d47-aab1-164d3e197af9" />

</p>

<p align="center">
VPS 공간 맵핑 기반 AR 화살표 길안내 및 실시간 경로 시각화 화면입니다.<br>
Immersal SDK 기반 위치 인식 기술과 NavMesh 경로 데이터를 활용하여<br>
사용자의 현재 위치를 인식하고 목적지까지 실시간 AR 내비게이션 서비스를 제공합니다.
</p>

---

# 5. 관리자 웹 시스템

## 5.1 관리자 웹 메인 페이지

<p align="center">
  <img width="1926" height="970" alt="스크린샷 2026-05-27 160519" src="https://github.com/user-attachments/assets/6942a12c-21e8-4668-9d65-4e7581d0aaa8" />
</p>

<p align="center">
관리자 웹 메인 페이지입니다.<br>
메인 페이지에서 총 관람객 수, 등록된 전시품 개수, 사용자 만족도 등의 정보를 확인할 수 있습니다.
</p>

---

## 5.2 관리자 웹 상세 관리 페이지

<p align="center">
  <img width="1913" height="977" alt="스크린샷 2026-05-27 160618" src="https://github.com/user-attachments/assets/21ebb46c-9dba-4ecc-908f-7e1235afa7df" />
</p>

<p align="center">
3D 맵 기반 공간 관리 화면입니다.<br>
관리자는 층별 세부 공간 구조와 방문 연령대 통계 데이터를 확인할 수 있습니다.
</p>

---

# 6. 프로젝트 소개

본 프로젝트는 문화관광 시설 내부에서 사용자가 목적지를 보다 직관적으로 탐색할 수 있도록 지원하는 AR 기반 실내 내비게이션 시스템입니다.

Immersal SDK 기반 VPS(Visual Positioning System)를 활용하여 사용자의 실내 위치를 인식하고, Unity NavMesh를 이용해 현재 위치에서 목적지까지의 최적 경로를 실시간으로 생성합니다.

또한 AR 화살표 오브젝트를 통해 사용자가 직관적으로 이동 경로를 확인할 수 있도록 구현하였으며, React 기반 관리자 페이지와 Supabase 데이터베이스를 연동하여 전시품 및 공간 데이터를 효율적으로 관리할 수 있도록 구성하였습니다.

---

# 7. 기존 문제점

기존 문화관광 시설 안내 시스템은 다음과 같은 한계가 존재합니다.

* 실내 환경에서 GPS 기반 위치 인식 정확도가 낮음
* 복잡한 실내 구조로 인해 사용자 길찾기 어려움 발생
* 종이 지도 및 텍스트 기반 안내 방식의 낮은 직관성
* 시설 정보 변경 시 실시간 반영의 어려움
* 비콘 및 LiDAR 기반 시스템 구축 시 높은 비용 발생

---

# 8. 해결 방법

본 프로젝트는 Immersal SDK 기반 VPS 기술을 활용하여 실내 공간에서 사용자의 위치를 인식합니다.

Unity NavMesh를 이용해 현재 위치에서 목적지까지의 최적 경로를 계산하며, AR 화살표 오브젝트를 통해 실시간 길안내 서비스를 제공합니다.

또한 React 기반 관리자 페이지와 Supabase 데이터베이스를 연동하여 전시품 위치 및 공간 데이터를 실시간으로 관리할 수 있도록 구현하였습니다.

---

# 9. 기대 효과

* AR 기반 실내 길안내를 통해 사용자가 목적지를 보다 직관적으로 탐색 가능
* 관리자 시스템을 활용하여 전시품 및 공간 데이터를 실시간 관리 가능
* 별도 비콘 장비 없이 VPS 기반 위치 인식 지원 가능
* 방문객 이동 동선 기반 데이터 분석을 통한 효율적인 공간 운영 가능
* 다양한 모바일 환경에서 안정적인 AR 서비스 제공 가능

---

# 10. 기술 스택

| 분야             | 기술                                 |
| -------------- | ---------------------------------- |
| Front-End(Web) | React                              |
| Front-End(App) | Unity, AR Foundation, Immersal SDK |
| Back-End       | NestJS                             |
| Database       | Supabase                           |
| Deployment     | Vercel, Render                     |
| Collaboration  | GitHub, Figma                      |

---

# 11. 시스템 구조

| 사용자 시스템            | 관리자 시스템           |
| ------------------ | ----------------- |
| 사용자                | 관리자 웹 페이지 (React) |
| ↓                  | ↓                 |
| Unity AR App       | NestJS API Server |
| ↓                  | ↓                 |
| Immersal SDK 위치 인식 | Supabase Database |
| ↓                  |                   |
| NavMesh 경로 탐색      |                   |
| ↓                  |                   |
| WayPoint 기반 경로 생성  |                   |
| ↓                  |                   |
| AR 화살표 렌더링         |                   |
| ↓                  |                   |
| 실시간 길안내            |                   |

---

# 12. 개선 사항

* **공간 데이터 맵핑 정밀도 향상**
  공간 데이터 크기 제한으로 인해 하나의 공간을 분할 맵핑한 뒤 공통 구조물을 기준으로 정렬(Calibration)하여 구현하였습니다.
  향후에는 고해상도 대용량 맵핑 프로세스를 적용하여 복합 공간 정합 과정에서 발생하는 오차를 최소화하고 공간 데이터 정밀도를 더욱 향상시킬 예정입니다.

* **클라우드 인프라 통합 및 최적화**
  현재는 운영 비용 절감을 위해 Frontend(Vercel)와 Backend(Render)를 분리하여 배포 및 운영하고 있습니다.
  향후에는 AWS(Amazon Web Services) 기반 단일 클라우드 아키텍처로 통합하여 인프라 관리 효율성과 시스템 안정성을 향상시킬 계획입니다.

* **AR 콘텐츠 확장**
  사용자 경험 향상과 서비스 활성화를 위해 실내 내비게이션 기능 외에도 공간 맞춤형 인터랙션 등 다양한 AR 콘텐츠를 추가적으로 확장할 예정입니다.
