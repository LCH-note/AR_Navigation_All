import { SetMetadata } from '@nestjs/common';

// Unity 앱처럼 { success, data } 래핑 없이 응답을 그대로 반환해야 하는 엔드포인트에 사용
export const RAW_RESPONSE_KEY = 'rawResponse';
export const RawResponse = () => SetMetadata(RAW_RESPONSE_KEY, true);
