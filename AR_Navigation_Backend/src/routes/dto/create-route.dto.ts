import { Type } from 'class-transformer';
import {
  IsArray,
  IsNumber,
  IsOptional,
  IsString,
  ValidateNested,
} from 'class-validator';


export class WaypointDto {
  @IsNumber()
  x: number;

  @IsNumber()
  y: number;

  @IsNumber()
  z: number;

  @IsString()
  displayName: string;

  @IsString()
  instruction: string;

  // 소속 맵 인덱스: 0 = 맵 A (145962), 1 = 맵 B (145963)
  @IsNumber()
  @IsOptional()
  mapIndex?: number;
}

export class CreateRouteDto {
  @IsString()
  route_id: string;

  @IsString()
  route_name: string;

  @IsOptional()
  @IsString()
  destination?: string;

  @IsOptional()
  @IsString()
  description?: string;

  @IsOptional()
  @IsString()
  estimated_distance?: string;

  @IsOptional()
  @IsString()
  estimated_time?: string;

  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => WaypointDto)
  waypoints: WaypointDto[];
}
