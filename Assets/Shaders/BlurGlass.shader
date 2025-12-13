Shader "Custom/BlurGlass"
{
    Properties
    {
        // Image側が設定するスプライト。9スライス境界もここに含まれる。
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}

        _TintColor("Tint Color", Color) = (0.85, 0.95, 1.0, 0.5)
        _BlurRadius("Blur Radius", Range(0.0, 32.0)) = 2.0
        [IntRange] _KernelSize ("Blur Kernel Size", Range(1, 10)) = 3
        _GlassStrength("Glass Strength", Range(0.0, 2.0)) = 1.0
        _AddColorStrength("Add Color Strength", Range(0.0, 2.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        // 加算寄り＋アルファ。必要に応じて調整。
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "FrostedGlassUI"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            // UIでよく使うマルチコンパイル（必要最低限）
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // スプライト（マスク兼用）
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // 背景キャプチャ（URP: Opaque Texture を有効にすること）
            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float4 _TintColor;
            float _BlurRadius;
            float _KernelSize;
            float _GlassStrength;
            float _AddColorStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                float2 uvScreen    : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(positionWS);

                // ここが重要：
                // Imageコンポーネントが9スライス用に組んだUVをそのまま使う。
                OUT.uv = IN.uv;
                OUT.color = IN.color;

                // スクリーンUV
                float2 ndc = OUT.positionHCS.xy / OUT.positionHCS.w;
                OUT.uvScreen = ndc * 0.5f + 0.5f;

                return OUT;
            }

            // 9tapボックスブラー
            float3 SampleBlurredOpaque(float2 uvScreen)
            {
                float2 texelSize = 1.0 / _ScreenParams.xy;
                float2 offset = texelSize * _BlurRadius;

                float3 c = 0.0f;

                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvScreen + float2(-offset.x, -offset.y)).rgb;
                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvScreen + float2( 0.0f     , -offset.y)).rgb;
                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvScreen + float2( offset.x, -offset.y)).rgb;

                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvScreen + float2(-offset.x,  0.0f     )).rgb;
                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvScreen).rgb;
                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvScreen + float2( offset.x,  0.0f     )).rgb;

                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvScreen + float2(-offset.x,  offset.y)).rgb;
                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvScreen + float2( 0.0f     ,  offset.y)).rgb;
                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvScreen + float2( offset.x,  offset.y)).rgb;

                c /= 9.0f;

                return c;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // スプライトマスク（9スライス込み）
                float4 mainSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // 完全透明部分は破棄
                if(mainSample.a <= 0.001f)
                {
                    discard;
                }

                // 背景ぼかし
                float3 blurred = SampleBlurredOpaque(IN.uvScreen);

                // すりガラス感
                float3 glass = blurred * _GlassStrength;

                // 色を加算寄りで乗せる
                float3 addedColor = glass + _TintColor.rgb * _AddColorStrength;

                // アルファは：Tint × スプライトα × 頂点カラーα
                float alpha = saturate(_TintColor.a * mainSample.a * IN.color.a);

                return float4(addedColor * alpha, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
