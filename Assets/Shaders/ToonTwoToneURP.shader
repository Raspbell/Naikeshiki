Shader "Malen/ToonTwoToneURP_Compat"
{
    Properties
    {
        _BaseMap        ("Albedo (RGB) A", 2D) = "white" {}
        _BaseColor      ("Albedo Color", Color) = (1,1,1,1)

        _BrightColor    ("Bright Tone Color", Color) = (1,1,1,1)
        _DarkColor      ("Dark   Tone Color", Color) = (0.2,0.2,0.2,1)
        _LightThreshold ("Light Threshold", Range(0,1)) = 0.5

        [Toggle(_ALBEDO_TINT)] _UseAlbedo ("Multiply Albedo", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.5

            // シャドウ・追加ライト
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            // 先に include すること（TEXTURE2D 等のマクロ定義が必要）
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // テクスチャ宣言（Core.hlsl 読み込み後に行う）
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // マテリアル定数
            float4 _BaseColor;
            float4 _BrightColor;
            float4 _DarkColor;
            float  _LightThreshold;
            float  _UseAlbedo;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionWS = posWS;
                o.positionHCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv;
                o.shadowCoord = TransformWorldToShadowCoord(posWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 nWS = normalize(i.normalWS);

                // メインライト（影減衰込み）
                Light mainLight = GetMainLight(i.shadowCoord);

                // N・L
                float ndotl = saturate(dot(nWS, normalize(mainLight.direction)));

                // 二階調判定
                float lite = step(_LightThreshold, ndotl);

                // 明暗色
                float3 tone = lerp(_DarkColor.rgb, _BrightColor.rgb, lite);

                // Albedo
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 baseCol = (_UseAlbedo > 0.5) ? albedo.rgb : float3(1.0, 1.0, 1.0);

                // シャドウとライト色
                float shadow = mainLight.shadowAttenuation;
                float3 litColor = tone * baseCol * mainLight.color * shadow;

                return float4(litColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
