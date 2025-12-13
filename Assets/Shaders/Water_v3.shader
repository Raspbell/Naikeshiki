Shader "Malen_Water_v3"
{
    Properties
    {
        // ベースと水没（エッジ）側の色
        _BaseColor      ("Base Color", Color) = (0.0, 0.5, 0.9, 1.0)
        _EdgeColor      ("UnderWater/Edge Color", Color) = (0.0, 0.2, 0.6, 1.0)

        // フェード幅（視点空間メートル相当）。交差からどの厚みで水没色へ遷移するか
        _FadeWidth      ("Fade Width (Eye-Space Units)", Float) = 0.2

        // 泡の色・強度・帯の厚み・ノイズしきい・スケール・速度
        _FoamColor      ("Foam Color (RGBA)", Color) = (1,1,1,1)
        _FoamIntensity  ("Foam Intensity", Float) = 1.0
        _FoamWidth      ("Foam Band Width (Eye-Space Units)", Float) = 0.06
        _FoamCutoff     ("Foam Noise Cutoff (0-1)", Float) = 0.5
        _FoamScale      ("Foam UV Scale", Float) = 2.0
        _FoamSpeed      ("Foam Scroll Speed (UV/sec)", Float) = 0.2

        // 泡ノイズ（未設定でも動作。設定した方が自然）
        _FoamNoiseTex   ("Foam Noise (R)", 2D) = "gray" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // SRP Batcher 対応
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            // 必要ライブラリ
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // プロパティ（SRP Batcher向け: CBUFFER 内）
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EdgeColor;

                float _FadeWidth;

                half4 _FoamColor;
                float _FoamIntensity;
                float _FoamWidth;
                float _FoamCutoff;
                float _FoamScale;
                float _FoamSpeed;
            CBUFFER_END

            // 泡ノイズ
            TEXTURE2D(_FoamNoiseTex);
            SAMPLER(sampler_FoamNoiseTex);

            // カメラ深度
            TEXTURE2D_X_FLOAT(_CameraDepthTexture);

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionVS : TEXCOORD0;   // 視点空間座標
                float4 screenPos  : TEXCOORD1;   // 画面UV用
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                // OS→WS→CS
                float3 positionWS = TransformObjectToWorld(IN.positionOS);
                OUT.positionCS    = TransformWorldToHClip(positionWS);

                // 視点空間座標（Zは負）
                OUT.positionVS    = TransformWorldToView(positionWS);

                // 画面座標（透視補正用の w を含む）
                OUT.screenPos     = ComputeScreenPos(OUT.positionCS);

                return OUT;
            }

            // 画面UV（0..1）
            float2 GetScreenUV(float4 screenPos)
            {
                float2 uv = screenPos.xy / screenPos.w;
                return uv;
            }

            // シーン深度を視点空間距離へ
            float SampleSceneEyeDepth(float2 uv)
            {
                // Raw depth -> linear eye depth
                #if UNITY_REVERSED_Z
                    float raw = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, uv).r;
                #else
                    float raw = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, uv).r;
                #endif
                float eye = LinearEyeDepth(raw, _ZBufferParams);
                return eye;
            }

            // ノイズマスク（泡用）
            float FoamNoise(float2 screenUV)
            {
                // スクロールするUV
                float2 uv = screenUV * _FoamScale + float2(_Time.y * _FoamSpeed, 0.0);

                // 2D ノイズテクスチャ参照
                float n = SAMPLE_TEXTURE2D(_FoamNoiseTex, sampler_FoamNoiseTex, uv).r;

                // ソフト閾値で二値化
                float w = 0.1; // エッジの柔らかさ
                float m = smoothstep(_FoamCutoff - w, _FoamCutoff + w, n);

                return m;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // 画面UV
                float2 screenUV = GetScreenUV(IN.screenPos);

                // シーン深度（視点空間距離）
                float sceneEye = SampleSceneEyeDepth(screenUV);

                // 現在ピクセルの視点空間距離（Zは負のため符号反転）
                float fragEye = -IN.positionVS.z;

                // 深度差（交差で 0 付近）
                float depthDiff = max(sceneEye - fragEye, 0.0);

                // 水没フェード係数 t（0..1）
                float t = saturate(_FadeWidth > 1e-5 ? depthDiff / _FadeWidth : 1.0);

                // ベース色（上: Base → 下: Edge）
                float3 baseCol = lerp(_BaseColor.rgb, _EdgeColor.rgb, t);

                // 透明度（上:1 → 下:0 の例）
                float alphaBase = 1.0 - t;

                // ---- 泡：境界バンド抽出 ----
                // s1: [0, w] で立ち上がり, s2: [w, 2w] で立ち下がり
                float w = max(_FoamWidth, 1e-6);
                float s1 = smoothstep(0.0, w, depthDiff);
                float s2 = smoothstep(w, 2.0 * w, depthDiff);
                float band = saturate(s1 - s2);

                // ノイズで切り欠き
                float nmask = FoamNoise(screenUV);

                // 泡マスク
                float foamMask = saturate(band * nmask) * _FoamIntensity;

                // 泡合成（色は加法ではなく Lerp。Emission に足す案も可）
                float3 colorFoam  = _FoamColor.rgb;
                float3 colorMixed = lerp(baseCol, colorFoam, foamMask);

                // 泡でアルファをわずかに下限持ち上げ（見栄え）
                float alpha = max(alphaBase, foamMask * _FoamColor.a);

                return half4(colorMixed, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
