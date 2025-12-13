Shader "Custom/TexturedGlass"
{
    Properties
    {
        _Color ("色", Color) = (0.85, 0.95, 1.0, 1.0)
        _Alpha ("色の透明度", Range(0,1)) = 0.15

        // _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 1

        _NormalTex ("ノーマルマップ", 2D) = "bump" {}
        _NormalScale ("ノーマル強さ", Range(0,5)) = 1.2

        _RefractiveIndex ("屈折率", Range(1.0,1.2)) = 1.0

        _FresnelPower ("フレネル反射強度", Range(0.1,20)) = 6.0
        _EdgeTint ("フレネル反射色", Color) = (0.6,0.8,1.0,1.0)

        _Dispersion ("色収差", Range(0,0.5)) = 0.08  // R<G<B の順で屈折率が大きくなるため、そのズレを表現するための値
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Alpha;

                // float _Metallic;
                float _Smoothness;

                float _NormalScale;
                float4 _NormalTex_ST;

                float _RefractiveIndex;

                float _FresnelPower;
                float4 _EdgeTint;

                float _Dispersion;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // GPUインスタンスを使って処理を最適化できるらしい
            };

            struct v2f
            {
                float4 vertex : SV_Position;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ノーマルマップの値をワールド空間に変換
            float3x3 TangentToWorldMatrix(float3 nWS, float4 tangentWS)
            {
                float3 t = normalize(tangentWS.xyz);
                float3 b = cross(nWS, t) * tangentWS.w;
                return float3x3(t, b, nWS);
            }

            // スクリーンUVを計算
            float2 CalcScreenUV(float4 screenPos)
            {
                float2 uvScene = screenPos.xy / max(screenPos.w, 1e-6);
                return saturate(uvScene);
            }

            // 環境マップからの反射色の取得
            float3 SampleEnvReflection(float3 R)
            {
                float4 encoded = SAMPLE_TEXTURECUBE(unity_SpecCube0, samplerunity_SpecCube0, R);
                return DecodeHDREnvironment(encoded, unity_SpecCube0_HDR);
            }

            // フレネル反射強度の計算
            float CalcFresnel(float cosTheta, float power)
            {
                cosTheta = saturate(cosTheta);
                return pow(1.0 - cosTheta, power);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // オブジェクト空間座標の頂点をいろいろ変換して最終的にスクリーンUV空間に変換する (詳しくはよくわからん)
                // オブジェクト空間→ワールド空間→クリップ空間→スクリーンUV空間
                float4 posWS = mul(unity_ObjectToWorld, v.vertex);
                o.vertex = TransformWorldToHClip(posWS.xyz);
                o.positionWS = posWS.xyz;

                o.normalWS = normalize(mul(v.normalOS,  (float3x3)unity_ObjectToWorld));
                o.tangentWS = float4(normalize(mul(v.tangentOS.xyz, (float3x3)unity_ObjectToWorld)), v.tangentOS.w);

                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.vertex);
                o.viewDirWS = normalize(GetWorldSpaceViewDir(o.positionWS));

                return o;
            }

            half4 frag(v2f inputData) : SV_Target
            {
                // 接線空間からワールド空間への変換行列
                float3x3 tangentToWorld = TangentToWorldMatrix(normalize(inputData.normalWS), inputData.tangentWS);

                // ノーマルマップから法線ベクトルを取得
                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, TRANSFORM_TEX(inputData.uv, _NormalTex)),_NormalScale);

                // ワールド空間に変換した法線ベクトル
                float3 normalWS = normalize(mul(normalTS, tangentToWorld));

                // 背景からUVを計算
                float2 sceneUV = CalcScreenUV(inputData.screenPos);

                // カメラからピクセルへの視線方向
                float3 viewDirectionWS = normalize(inputData.viewDirWS);

                // 法線と視線の角度
                float surfaceFacing = saturate(dot(normalWS, viewDirectionWS));

                // 視線角度による屈折スケール
                float viewAngleFactor = (1.0 - surfaceFacing);

                // 屈折率に基づく補正係数
                float refractiveIndexScale = max(_RefractiveIndex - 1.0, 0.0) * 10.0;

                // 屈折による背景UVのずれ量
                float2 refractionOffset = normalWS.xy * viewAngleFactor * refractiveIndexScale;

                // 屈折によるRGBずれ（色収差）
                float2 uvGreen = saturate(sceneUV + refractionOffset);
                float2 uvRed = saturate(sceneUV + refractionOffset * (1.0 + _Dispersion));
                float2 uvBlue = saturate(sceneUV + refractionOffset * (1.0 - _Dispersion));

                // 背景色の取得
                float3 colorGreen = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvGreen).rgb;
                float3 colorRed = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvRed).rgb;
                float3 colorBlue = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uvBlue).rgb;

                // RGBずれを合成して屈折色を作成
                float3 refractionColor = float3(colorRed.r, colorGreen.g, colorBlue.b) * _Color.rgb;

                // フレネル反射係数
                float fresnelFactor = CalcFresnel(surfaceFacing, _FresnelPower);
                //float fresnelFactor = 0;

                // 反射ベクトルを計算
                float3 reflectionVectorWS = reflect(-viewDirectionWS, normalWS);

                // 環境マップから反射色を取得
                float3 environmentReflectionColor = SampleEnvReflection(reflectionVectorWS);

                // エッジ付近の色（屈折色とエッジ色をブレンド）
                float3 fresnelEdgeColor = lerp(refractionColor, _EdgeTint.rgb, saturate(fresnelFactor * 0.5));

                // フレネル効果で反射色をブレンド
                float3 combinedReflectionColor = lerp(fresnelEdgeColor, environmentReflectionColor, fresnelFactor);

                // ハイライト補正
                float highlightBoost = saturate(_Smoothness * 0.25);
                float3 finalColor = lerp(refractionColor, combinedReflectionColor, highlightBoost);

                return half4(finalColor, saturate(_Alpha));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
