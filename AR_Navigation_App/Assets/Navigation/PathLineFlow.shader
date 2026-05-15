// 파일명: Assets/Navigation/PathLineFlow.shader
// 역할: 경로 유도선에 흐르는 대시 애니메이션을 적용하는 URP 커스텀 셰이더
//   - LineRenderer 의 colorGradient 버텍스 컬러를 그대로 반영
//   - _Time.y 기반 UV 스크롤로 대시가 선 시작(카메라)→끝(목적지) 방향으로 흐름
//   - ZTest Always: AR 바닥 평면 메시에 가려지지 않도록 항상 렌더링
Shader "Custom/PathLineFlow"
{
    Properties
    {
        // C# CreatePathLineMaterial() 에서 SetFloat 로 값을 주입한다
        _FlowSpeed     ("흐름 속도",   Float)      = 1.5
        _DashFrequency ("대시 빈도",   Float)      = 2.5
        _DashRatio     ("대시 비율",   Range(0,1)) = 0.65
        _GlowIntensity ("발광 강도",   Float)      = 1.3
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            // Transparent+10: 반투명 오브젝트보다 늦게 그려 Z-sort 간섭 최소화
            "Queue"          = "Transparent+10"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PathLineFlow"

            // 반투명 합성 (표준 알파 블렌딩)
            Blend SrcAlpha OneMinusSrcAlpha
            // 깊이 버퍼에 쓰지 않음 — 반투명 선이 후속 오브젝트를 가리는 현상 방지
            ZWrite Off
            // AR 바닥 평면 메시가 깊이를 기록해도 항상 렌더링
            ZTest Always
            // LineRenderer 는 얇은 양면 쿼드이므로 양면 렌더링
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            // OpenGL ES 3.0 이상 대상 (Android AR 기기 호환)
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 머티리얼 프로퍼티 상수 버퍼 (SRP Batcher 호환)
            CBUFFER_START(UnityPerMaterial)
                float _FlowSpeed;
                float _DashFrequency;
                float _DashRatio;
                float _GlowIntensity;
            CBUFFER_END

            // ── 버텍스 입력 ───────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                // LineRenderer 가 textureMode = Tile 시 길이 방향(x) + 너비 방향(y) UV 제공
                float2 uv         : TEXCOORD0;
                // LineRenderer 의 colorGradient 를 버텍스 컬러로 수신
                float4 color      : COLOR;
            };

            // ── 프래그먼트 입력 ───────────────────────────────────────
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            // ── 버텍스 셰이더 ─────────────────────────────────────────
            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            // ── 프래그먼트 셰이더 ─────────────────────────────────────
            half4 Frag(Varyings IN) : SV_Target
            {
                // ── 1. 흐름 UV 계산 ──────────────────────────────────
                // uv.x: 선 시작(카메라=0) → 끝(목적지=총거리) 방향, Tile 모드에서 m 단위
                // flowUV 를 frac() 하면 0~1 을 반복하는 대시 패턴 좌표가 됨
                // 시간이 지날수록 같은 대시가 더 큰 uv.x 위치에 나타남 → 시작→끝 방향으로 흐름
                float flowUV = IN.uv.x * _DashFrequency - _Time.y * _FlowSpeed;
                float t      = frac(flowUV); // 0 = 대시 머리, _DashRatio = 대시 꼬리

                // ── 2. 대시 알파 계산 ────────────────────────────────
                // [0, _DashRatio]: 대시 구간 — 머리(t=0)에서 밝고 꼬리로 갈수록 투명
                // (_DashRatio, 1 ]: 간격 구간 — 완전 투명 (0.05 구간으로 부드럽게 전환)
                float inDash    = 1.0 - smoothstep(_DashRatio - 0.05, _DashRatio + 0.02, t);
                float normalized = saturate(t / max(_DashRatio, 0.001)); // 0=머리, 1=꼬리
                float dashAlpha = inDash * pow(1.0 - normalized, 1.5);   // 꼬리로 갈수록 지수적으로 감소

                // ── 3. 선 너비 방향 가장자리 페이드 ─────────────────
                // uv.y 가 0 또는 1 인 가장자리는 완전 투명, 중앙(0.5)은 불투명
                // → 경계가 부드러운 발광선처럼 보이는 효과
                float edge     = abs(IN.uv.y * 2.0 - 1.0); // 0 = 중앙, 1 = 가장자리
                float edgeFade = smoothstep(1.0, 0.15, edge);

                // ── 4. 대시 머리 발광 하이라이트 ─────────────────────
                // normalized 의 앞 30% 구간에서 지수적으로 강한 빛을 추가해
                // 대시가 움직이는 느낌을 강조한다
                float headGlow = pow(saturate(1.0 - normalized * 3.3), 2.5);

                // ── 5. 최종 색상 조합 ────────────────────────────────
                // RGB: LineRenderer colorGradient 버텍스 컬러 × 발광 강도
                // A  : 대시 패턴 × 가장자리 페이드 (gradient alpha 는 셰이더에서 직접 제어)
                half4 col  = IN.color;
                col.rgb   *= _GlowIntensity * (1.0 + headGlow * 0.8);
                col.a      = dashAlpha * edgeFade;

                return col;
            }
            ENDHLSL
        }
    }

    // URP 미지원 환경 폴백 (ARFoundation 이 지원하지 않는 플랫폼용)
    FallBack "Universal Render Pipeline/Unlit"
}
