import { Injectable } from '@nestjs/common';
import { SupabaseService } from '../supabase/supabase.service';

const MAP_TYPES = {
  IMMERSAL: 'immersal_map',
  IMMERSAL_B: 'immersal_map_b',  // 두 번째 맵 (145962)
  FLOOR_PLAN: 'floor_plan',
  THREE_D_MODEL: '3d_model',     // 3D 전체도 (.glb)
} as const;
type MapType = (typeof MAP_TYPES)[keyof typeof MAP_TYPES];

@Injectable()
export class AssetsService {
  constructor(private readonly supabase: SupabaseService) {}

  async getAsset(mapType: MapType) {
    const { data } = await this.supabase.db
      .from('maps')
      .select('file_url, updated_at')
      .eq('map_type', mapType)
      .not('file_url', 'is', null)
      .order('updated_at', { ascending: false })
      .limit(1)
      .single();

    // 등록된 맵 없으면 빈 응답 반환 (앱에서 로컬 캐시로 폴백)
    if (!data?.file_url) {
      return { fileUrl: '', version: '' };
    }

    return {
      fileUrl: data.file_url,
      version: data.updated_at,
    };
  }

  getMap() {
    return this.getAsset(MAP_TYPES.IMMERSAL);
  }

  // 두 번째 맵 (145962) — map_type = 'immersal_map_b'
  getMapB() {
    return this.getAsset(MAP_TYPES.IMMERSAL_B);
  }

  getFloorPlan() {
    return this.getAsset(MAP_TYPES.FLOOR_PLAN);
  }

  // 플로어별 3D 전체도 목록 — floor 컬럼이 지정된 최신 항목만, 플로어당 1개
  async get3dModels() {
    const { data } = await this.supabase.db
      .from('maps')
      .select('floor, file_url, updated_at')
      .eq('map_type', MAP_TYPES.THREE_D_MODEL)
      .not('file_url', 'is', null)
      .order('updated_at', { ascending: false });

    const seen = new Set<string>();
    return (data || [])
      .filter((d) => {
        if (!d.floor || seen.has(d.floor)) return false;
        seen.add(d.floor);
        return true;
      })
      .map((d) => ({
        floor: d.floor,
        fileUrl: d.file_url,
        version: d.updated_at,
      }));
  }

  // 플로어별 2D 평면도 목록 — floor 컬럼이 지정된 최신 항목만, 플로어당 1개
  async getFloorPlans() {
    const { data } = await this.supabase.db
      .from('maps')
      .select('floor, file_url, updated_at')
      .eq('map_type', MAP_TYPES.FLOOR_PLAN)
      .not('file_url', 'is', null)
      .not('floor', 'is', null)
      .order('updated_at', { ascending: false });

    // 플로어당 가장 최근 항목 1개만 유지
    const seen = new Set<string>();
    return (data || [])
      .filter((d) => {
        if (seen.has(d.floor)) return false;
        seen.add(d.floor);
        return true;
      })
      .map((d) => ({
        floor: d.floor,
        fileUrl: d.file_url,
        version: d.updated_at,
      }));
  }
}
