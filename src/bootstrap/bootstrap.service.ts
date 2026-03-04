import { Injectable, NotFoundException } from '@nestjs/common';
import { FacilitiesService } from '../facilities/facilities.service';
import { MapAssetsService } from '../map-assets/map-assets.service';
import { AnchorsService } from '../anchors/anchors.service';
import { PlacesService } from '../places/places.service';
import { GraphService } from '../graph/graph.service';

@Injectable()
export class BootstrapService {
  constructor(
    private readonly facilitiesService: FacilitiesService,
    private readonly mapAssetsService: MapAssetsService,
    private readonly anchorsService: AnchorsService,
    private readonly placesService: PlacesService,
    private readonly graphService: GraphService,
  ) {}

  async getFacilityBootstrap(facilityId: number) {
    const [facility, mapAssets, anchors, places, graphNodes, graphEdges] = await Promise.all([
      this.facilitiesService.findOne(facilityId),
      this.mapAssetsService.findByFacility(facilityId),
      this.anchorsService.findByFacility(facilityId),
      this.placesService.findByFacility(facilityId),
      this.graphService.listNodes(facilityId),
      this.graphService.listEdges(facilityId),
    ]);

    // 시설이 없으면 404
    if (!facility) {
      throw new NotFoundException(`facility ${facilityId} not found`);
    }

    // 앱에는 보통 "활성/최신 맵 1개"만 주는 게 편함
    // (mapAssetsService에서 정렬/필터링이 이미 되면 여기선 그냥 첫 번째 사용)
    const mapAsset = Array.isArray(mapAssets) ? mapAssets[0] ?? null : mapAssets ?? null;

    return {
      facility,
      mapAsset,
      anchors,
      places,
      graph: {
        nodes: graphNodes,
        edges: graphEdges,
      },
    };
  }
}