using UnityEngine.XR.Interaction.Toolkit.Utilities.Tweenables.Primitives;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets
{
    /// <summary>
    /// ARFeatheredPlane 메시에 대한 추가 시각 처리를 수행합니다. (알파 페이드 인/아웃 애니메이션 등)
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ARFeatheredPlaneMeshVisualizerCompanion : MonoBehaviour
    {
        [Tooltip("ARFeatheredPlane 프리팹의 Renderer 컴포넌트. 페이드 인/아웃에 사용할 머티리얼을 가져옵니다.")]
        [SerializeField]
        Renderer m_PlaneRenderer;

        /// <summary>
        /// ARFeatheredPlane 프리팹의 Renderer 컴포넌트. 페이드 인/아웃에 사용할 머티리얼을 가져옵니다.
        /// </summary>
        public Renderer planeRenderer
        {
            get => m_PlaneRenderer;
            set => m_PlaneRenderer = value;
        }

        [Tooltip("알파 트위닝 중 적용되는 페이드 인/아웃 속도 배수. 값이 낮을수록 느리게 동작하며, 1이면 최대 속도(1초)입니다.")]
        [Range(0.1f, 1.0f)]
        [SerializeField]
        float m_FadeSpeed = 1f;

        /// <summary>
        /// 알파 트위닝 중 적용되는 페이드 인/아웃 속도 배수.
        /// 값이 낮을수록 느리게 동작하며, 1이면 최대 속도(1초)입니다.
        /// </summary>
        public float fadeSpeed
        {
            get => m_FadeSpeed;
            set => m_FadeSpeed = value;
        }

        int m_ShaderAlphaPropertyID;        // 셰이더의 _PlaneAlpha 프로퍼티 ID
        float m_SurfaceVisualAlpha = 1f;    // 현재 평면 표면의 알파값
        float m_TweenProgress;              // 트위닝 진행도 (0~1)
        Material m_PlaneMaterial;           // 페이드 처리에 사용할 머티리얼 인스턴스

#pragma warning disable CS0618 // 타입 또는 멤버가 더 이상 사용되지 않음 -- 어포던스 시스템은 향후 XRI 버전에서 교체 예정
        readonly FloatTweenableVariable m_AlphaTweenableVariable = new FloatTweenableVariable();
#pragma warning restore CS0618

        /// <summary>
        /// MonoBehaviour.Awake: 셰이더 프로퍼티 ID 및 머티리얼 초기화
        /// </summary>
        void Awake()
        {
            m_ShaderAlphaPropertyID = Shader.PropertyToID("_PlaneAlpha");
            m_PlaneMaterial = m_PlaneRenderer.material;
            visualizeSurfaces = true;
        }

        /// <summary>
        /// MonoBehaviour.OnDestroy: 트위닝 변수 리소스 해제
        /// </summary>
        void OnDestroy()
        {
            m_AlphaTweenableVariable.Dispose();
        }

        /// <summary>
        /// MonoBehaviour.Update: 매 프레임 알파 트위닝을 진행하고 셰이더에 반영
        /// </summary>
        void Update()
        {
            m_AlphaTweenableVariable.HandleTween(m_TweenProgress);
            m_TweenProgress += Time.unscaledDeltaTime * m_FadeSpeed;  // 실제 경과 시간 기반으로 진행도 증가
            m_SurfaceVisualAlpha = m_AlphaTweenableVariable.Value;
            m_PlaneMaterial.SetFloat(m_ShaderAlphaPropertyID, m_SurfaceVisualAlpha);  // 셰이더 알파값 갱신
        }

        /// <summary>
        /// true이면 평면 표면을 표시하고, false이면 숨깁니다.
        /// </summary>
        public bool visualizeSurfaces
        {
            set
            {
                m_TweenProgress = 0f;                                       // 트위닝 진행도 초기화
                m_AlphaTweenableVariable.target = value ? 1f : 0f;         // 목표 알파값 설정 (표시: 1, 숨김: 0)
                m_AlphaTweenableVariable.HandleTween(0f);                   // 트위닝 즉시 시작
            }
        }
    }
}
