Shader "Malen/Crystal"
{
    Properties
    {
        [KeywordEnum(XY, YZ, ZX)] _PLANE ("Plane", Float) = 0

        _BaseColor      ("Base Color", Color) = (0.18, 0.55, 0.95, 1.0)

        _EdgeLength     ("Triangle Edge Length (World Units)", Range(0.02, 100.0)) = 0.6
        _RotationDeg    ("Pattern Rotation (deg)", Range(-180, 180)) = 0.0

        // --- 修正点: Toggle が有効化するキーワードを _UV_SCROLL に統一 ---
        [Toggle(_UV_SCROLL)] _EnableUVScroll ("Enable UV Scroll", Float) = 0
        _ScrollSpeed     ("UV Scroll Speed (u,v)", Vector) = (0.0, 0.0, 0.0, 0.0)

        _ShimmerSpeed   ("Shimmer Speed", Range(0.0, 8.0)) = 1.5
        _ShimmerAmp     ("Shimmer Amplitude (×)", Range(0.0, 1.0)) = 0.35
        _Seed           ("Random Seed", Float) = 1.0

        _SatFreqX       ("Saturation Freq X (cycles/unit)", Range(0.0, 5.0)) = 0.6
        _SatFreqY       ("Saturation Freq Y (cycles/unit)", Range(0.0, 5.0)) = 0.6
        _SatAmp         ("Saturation Amplitude (×)", Range(0.0, 1.0)) = 0.5
        _SatSpeed       ("Saturation Field Speed", Range(0.0, 4.0)) = 0.5

        _NoiseScale     ("Noise Scale (world units)", Range(0.05, 5.0)) = 1.5
        _NoiseAmp       ("Noise Amplitude (additive)", Range(0.0, 1.0)) = 0.35
        _NoiseSpeed     ("Noise Flow Speed", Range(0.0, 4.0)) = 0.8
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        LOD 100
        Cull Back
        ZWrite On
        ZTest LEqual
        Lighting Off
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // 平面切替
            #pragma multi_compile_local _PLANE_XY _PLANE_YZ _PLANE_ZX
            // UVスクロール切替（キーワード名を _UV_SCROLL に統一）
            #pragma shader_feature_local _UV_SCROLL

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _BaseColor;
            float  _EdgeLength;
            float  _RotationDeg;

            float4 _ScrollSpeed; // xy を使用

            float  _ShimmerSpeed;
            float  _ShimmerAmp;
            float  _Seed;

            float  _SatFreqX;
            float  _SatFreqY;
            float  _SatAmp;
            float  _SatSpeed;

            float  _NoiseScale;
            float  _NoiseAmp;
            float  _NoiseSpeed;

            // rgb <-> hsv
            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                float h = abs(q.z + (q.w - q.y) / (6.0 * d + e));
                float s = d / (q.x + e);
                float v = q.x;
                return float3(h, s, v);
            }
            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            // 0..1 hash
            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // 2D value noise 0..1
            float valueNoise2D(float2 x)
            {
                float2 i = floor(x);
                float2 f = frac(x);
                float a = hash12(i);
                float b = hash12(i + float2(1.0, 0.0));
                float c = hash12(i + float2(0.0, 1.0));
                float d = hash12(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                float ab = lerp(a, b, u.x);
                float cd = lerp(c, d, u.x);
                return lerp(ab, cd, u.y);
            }

            // p(x,y) -> triangular lattice coords (u,v)
            float2 ToTriCoords(float2 p)
            {
                const float invSqrt3    = 0.5773502691896257;   // 1/sqrt(3)
                const float twoInvSqrt3 = 1.1547005383792515;   // 2/sqrt(3)
                float u = p.x - invSqrt3 * p.y;
                float v = twoInvSqrt3 * p.y;
                return float2(u, v);
            }

            // (u,v) -> p(x,y) inverse
            float2 FromTriCoords(float2 uv)
            {
                const float half = 0.5;
                const float sqrt3_over_2 = 0.8660254037844386;
                return float2(uv.x + half * uv.y, sqrt3_over_2 * uv.y);
            }

            // world→選択平面2D
            float2 GetPlaneCoords(float3 wp)
            {
                #if defined(_PLANE_XY)
                    return wp.xy;
                #elif defined(_PLANE_YZ)
                    return wp.yz;
                #else
                    return float2(wp.z, wp.x);
                #endif
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 平面内ワールド座標
                float2 w2 = GetPlaneCoords(i.worldPos);

                // 回転（平面内）
                float ang = radians(_RotationDeg);
                float c = cos(ang);
                float s = sin(ang);
                float2 q = float2(c * w2.x - s * w2.y, s * w2.x + c * w2.y);

                // UVスクロール（回転後＝パターン座標系）
                #if defined(_UV_SCROLL)
                {
                    float2 scroll = _ScrollSpeed.xy * _Time.y;
                    q += scroll;
                }
                #endif

                // 辺長スケール
                float L = max(_EdgeLength, 1.0e-4);
                float2 p = q / L;

                // 三角格子
                float2 tri = ToTriCoords(p);
                float2 cell = floor(tri);
                float2 f = frac(tri);
                bool lower = (f.x + f.y) < 1.0;
                float triIndex = (lower ? 0.0 : 1.0);

                // 重心（tri空間→平面座標）
                float2 centerTri = cell + (lower ? float2(1.0/3.0, 1.0/3.0) : float2(2.0/3.0, 2.0/3.0));
                float2 centerP = FromTriCoords(centerTri) * L;

                // 三角形内一様の揺らぎ
                float rnd = hash12(cell + triIndex + _Seed);
                float shimmerPhase = _Time.y * _ShimmerSpeed + rnd * 6.2831853;
                float shimmer01 = 0.5 + 0.5 * sin(shimmerPhase);

                // 飽和度フィールド（重心でサンプル）
                float2 qc = centerP;
                float phase = _SatSpeed * _Time.y;
                float sx = sin(6.2831853 * _SatFreqX * qc.x + phase);
                float sy = sin(6.2831853 * _SatFreqY * qc.y + phase);
                float baseField = 0.5 + 0.25 * sx + 0.25 * sy;

                // ノイズ流
                float2 npos = qc / max(_NoiseScale, 1.0e-4) + _NoiseSpeed * _Time.y;
                float n = valueNoise2D(npos);
                float field01 = saturate(baseField + _NoiseAmp * (n - 0.5));

                // HSV合成
                float3 hsv = rgb2hsv(_BaseColor.rgb);
                float satScale = lerp(1.0 - _SatAmp, 1.0 + _SatAmp, field01);
                float S = saturate(hsv.y * satScale);
                float V = saturate(hsv.z * lerp(1.0 - _ShimmerAmp, 1.0 + _ShimmerAmp, shimmer01));

                float3 col = hsv2rgb(float3(hsv.x, S, V));
                return float4(col, _BaseColor.a);
            }
            ENDCG
        }
    }

    FallBack "Unlit/Color"
}
