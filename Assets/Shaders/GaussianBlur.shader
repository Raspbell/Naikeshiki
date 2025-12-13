Shader "Custom/GaussianBlurClean"
{
    Properties
    {
        _KernelSize ("Blur Kernel Size", Range(1, 128)) = 3
        _BlurRadius ("Blur Radius", Range(0.0, 128.0)) = 2.0
        _BaseColor ("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
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
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float4 _CameraOpaqueTexture_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float _BlurRadius;
                float _KernelSize;
                float4 _BaseColor;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_Position;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float gauss(float x, float y, float sigma)
            {
                return 1.0f / (2.0f * PI * sigma * sigma) * exp(-(x * x + y * y) / (2.0f * sigma * sigma));
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 mainSample = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv);
                
                if(mainSample.a <= 0.001f)
                {
                    discard;
                }
                
                float2 screenUV = i.screenPos.xy / max(i.screenPos.w, 1e-6);
                float4 output = 0;
                float sum_weight = 0;
                

                [loop]
                for(int x = -_KernelSize / 2; x <= _KernelSize / 2; x++)
                {
                    for(int y = -_KernelSize / 2; y <= _KernelSize / 2; y++)
                    {
                        float2 offset = float2(x, y) * _CameraOpaqueTexture_TexelSize.xy;
                        float2 sampleUV = screenUV + offset;

                        float weight = gauss(x, y, _BlurRadius);
                        
                        output += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, sampleUV) * weight;
                        sum_weight += weight;
                    }
                }

                output /= sum_weight;
                output *= _BaseColor;

                return output;
            }

            ENDHLSL
        }
    }
    FallBack Off
}