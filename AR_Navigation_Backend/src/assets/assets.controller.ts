import { Controller, Get } from '@nestjs/common';
import { RawResponse } from '../common/decorators/raw-response.decorator';
import { AssetsService } from './assets.service';

@Controller('assets')
export class AssetsController {
  constructor(private readonly assetsService: AssetsService) {}

  // Unity 앱 GetAsync<AssetUrlResponse> 호환 — { fileUrl, version } 직접 반환
  @Get('map')
  @RawResponse()
  getMap() {
    return this.assetsService.getMap();
  }

  // 두 번째 맵 (145962) — Supabase maps 테이블 map_type = 'immersal_map_b'
  @Get('map-b')
  @RawResponse()
  getMapB() {
    return this.assetsService.getMapB();
  }

  @Get('floor-plan')
  @RawResponse()
  getFloorPlan() {
    return this.assetsService.getFloorPlan();
  }

  // Unity 앱용 — 플로어별 평면도 목록 ({ floor, fileUrl, version }[])
  // @RawResponse: GetArrayAsync 가 순수 배열을 {"items":[...]} 로 래핑해 파싱하므로 래핑 생략
  @Get('floor-plans')
  @RawResponse()
  getFloorPlans() {
    return this.assetsService.getFloorPlans();
  }

  // Unity 앱 + 웹용 — 플로어별 3D 전체도 목록 ({ floor, fileUrl, version }[])
  @Get('3d-models')
  @RawResponse()
  get3dModels() {
    return this.assetsService.get3dModels();
  }
}
