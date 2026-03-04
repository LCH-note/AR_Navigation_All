import { Injectable } from '@nestjs/common';

export type MapAsset = {
  id: number;
  facilityId: number;


  format: 'obj';
  fileUrl: string;

  
  scale: number;
  originX: number;
  originY: number;
  originZ: number;
  rotYawDeg: number;
};

@Injectable()
export class MapAssetsService {
  private assets: MapAsset[] = [];
  private nextId = 1;

  create(input: Omit<MapAsset, 'id'>) {
    const a: MapAsset = { id: this.nextId++, ...input };
    this.assets.push(a);
    return a;
  }

  findByFacility(facilityId: number) {
    return this.assets.filter((a) => a.facilityId === facilityId);
  }
}
