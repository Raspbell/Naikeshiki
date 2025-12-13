Shader "Custom/SuppressingOverdrawURP"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Tint Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "RenderQueue"="Transparent"
            "UniversalMaterialType"="Unlit"        // URPでUnlit扱い
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            // 一度描画したピクセルには再度描画しない → ステンシルを利用
            Stencil
            {
                Ref 1       // 書き込む数値
                Comp Less   // 現在0なら 0 < 1 でパス (初期ステンシル値は0)
                Pass Replace // パスしたピクセルを1にする → 2回目以降は描画されない
            }

            //--- 透明ブレンドの設定 ---
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // ↑ URP (Unity 2021.3〜2023.x) などで使用可能。バージョンによりパスが多少異なる場合があります。

            // 1) テクスチャ／サンプラーをURPスタイルで定義
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float4 _BaseColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // 2) テクスチャをサンプル
                //    SAMPLE_TEXTURE2D(テクスチャ, サンプラー, UV)
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                // 3) Tint Color を乗算
                c *= _BaseColor;

                return c;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
