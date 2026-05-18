import { BadRequestException } from '@nestjs/common';

/** 이미지 업로드 허용 MIME Type */
export const ALLOWED_IMAGE_MIME_TYPES = ['image/jpeg', 'image/png', 'image/svg+xml'];

/** 맵 파일 업로드 허용 MIME Type (Immersal 바이너리 + 이미지) */
export const ALLOWED_MAP_MIME_TYPES = [
  'image/jpeg',
  'image/png',
  'image/svg+xml',
  'application/octet-stream', // Immersal .bytes 맵 파일
];

/** 최대 파일 크기: 5 MB */
export const MAX_FILE_SIZE = 5 * 1024 * 1024;

/**
 * 이미지 파일 필터 — 허용되지 않는 MIME Type이면 BadRequestException
 */
export const imageFileFilter = (
  _req: any,
  file: Express.Multer.File,
  callback: (error: Error | null, acceptFile: boolean) => void,
) => {
  if (!ALLOWED_IMAGE_MIME_TYPES.includes(file.mimetype)) {
    return callback(
      new BadRequestException(
        `지원하지 않는 이미지 형식입니다. 허용: ${ALLOWED_IMAGE_MIME_TYPES.join(', ')}`,
      ),
      false,
    );
  }
  callback(null, true);
};

/**
 * 맵 파일 필터 — 이미지 및 바이너리 맵 데이터 허용
 */
export const mapFileFilter = (
  _req: any,
  file: Express.Multer.File,
  callback: (error: Error | null, acceptFile: boolean) => void,
) => {
  if (!ALLOWED_MAP_MIME_TYPES.includes(file.mimetype)) {
    return callback(
      new BadRequestException(
        `지원하지 않는 파일 형식입니다. 허용: ${ALLOWED_MAP_MIME_TYPES.join(', ')}`,
      ),
      false,
    );
  }
  callback(null, true);
};

/** 이미지 업로드용 multer 옵션 */
export const multerImageOptions = {
  limits: { fileSize: MAX_FILE_SIZE },
  fileFilter: imageFileFilter,
};

/** 맵 파일 업로드용 multer 옵션 */
export const multerMapFileOptions = {
  limits: { fileSize: MAX_FILE_SIZE },
  fileFilter: mapFileFilter,
};
