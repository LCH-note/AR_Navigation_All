// 파일명: Assets/Navigation/PathLineFlow.shader
// 역할: 경로 유도선에 부메랑/쉐브론 화살표 흐름 애니메이션을 적용하는 URP 커스텀 셰이더
//   - 반투명 배경 라인 위로 > 형태 쉐브론 마커들이 목적지 방향으로 흘러감
//   - LineRenderer colorGradient 버텍스 컬러 그대로 반영
//   - _Time.y 기반 UV 스크롤 — CPU 업데이트 없이 GPU에서 자동 애니메이션
//   - ZTest Always: AR 바닥 평면 메시에 가려지지 않도록 항상 렌더링
Shader "Custom/PathLineFlow"
{
    Properties
    {
        // C# CreatePathLineMaterial() 에서 SetFloat 로 값을 주입한다
        _FlowSpeed      ("흐름 속도",      Float)      = 1.5
        _ArrowFrequency ("화살표 빈도",    Float)      = 2.0
        _TipPosition    ("팁 위치 (0~1)", Range(0,1)) = 0.80
        _ChevronSpread  ("날개 폭 (0~1)", Range(0,1)) = 0.42
        _ArrowThickness ("화살표 두께",   Range(0,1)) = 0.07
        _GlowIntensity  ("발광 강도",     Float)      = 1.4
        _BgOpacity      ("배경 투명도",   Range(0,1)) = 0.22
        // 4 = LessEqual(기본, 벽에 가려짐) / 8 = Always(벽 무시, Inspector에서 변경 가능)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            // Transparent+10: 다른 반투명 오브젝트보다 늦게 그려 Z-sort 간섭 최소화
            "Queue"          = "Transparent+10"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PathLineChevron"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _FlowSpeed;
                float _ArrowFrequency;
                float _TipPosition;
                float _ChevronSpread;
                float _ArrowThickness;
                float _GlowIntensity;
                float _BgOpacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                // textureMode = Tile: uv.x = 길이 방향 누적 거리, uv.y = 너비 방향 0→1
                float2 uv         : TEXCOORD0;
                // LineRenderer colorGradient 버텍스 컬러
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // ── 1. 너비 방향 좌표 ─────────────────────────────────
                // v: 0 = 선 중앙, 1 = 선 가장자리
                float v = abs(IN.uv.y * 2.0 - 1.0);

                // 가장자리로 갈수록 부드럽게 사라지는 페이드
                float edgeFade = smoothstep(1.0, 0.25, v);

                // ── 2. 흐름 위상 계산 ─────────────────────────────────
                // uv.x 가 증가하는 방향 = 목적지 방향
                // t = 0(카메라쪽) → 1(목적지쪽) 으로 반복, 시간이 지나면서 전체 패턴이 전진
                float t = frac(IN.uv.x * _ArrowFrequency - _Time.y * _FlowSpeed);

                // ── 3. 쉐브론(부메랑) 형태 계산 ─────────────────────
                // 팁(v=0) 이 앞쪽(t = _TipPosition), 날개(v=1) 가 뒤쪽으로 벌어짐
                // chevronT: 이 v 위치에서 쉐브론 선이 위치해야 하는 t 값
                float chevronT = _TipPosition - v * _ChevronSpread;

                // chevronT < 0 인 영역은 날개가 타일 밖으로 넘어간 것 → 표시 안 함
                float chevronValid = step(0.0, chevronT);

                // 현재 픽셀의 t 와 쉐브론 선의 거리 기반 알파
                float dist       = abs(t - chevronT);
                float arrowAlpha = smoothstep(_ArrowThickness, 0.0, dist) * chevronValid;

                // ── 4. 팁 발광 ────────────────────────────────────────
                // v=0(중앙)이고 t ≈ _TipPosition 일 때만 밝게 — 화살표 꼭짓점 강조
                float tipGlow = exp(-v * v * 7.0)
                              * smoothstep(0.15, 0.0, abs(t - _TipPosition));

                // ── 5. 배경 반투명 라인 ───────────────────────────────
                // 항상 표시되는 베이스 라인 (가장자리 페이드 적용)
                float bgAlpha = _BgOpacity * edgeFade;

                // ── 6. 최종 색상 ─────────────────────────────────────
                half4 col       = IN.color;
                float glowBoost = 1.0 + tipGlow * 0.6;
                float arrowFinal = arrowAlpha * edgeFade * _GlowIntensity * glowBoost;

                col.a   = max(bgAlpha, arrowFinal);
                col.rgb *= glowBoost; // 팁 꼭짓점 RGB 약간 밝게

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
