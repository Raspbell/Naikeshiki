Shader "Custom/SpriteOutline"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        // スプライト用の設定
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Sprite"
        }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha  // 通常のアルファブレンド

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Sprite Shader でよく使われるマクロたち
            #include "UnityCG.cginc"
            #pragma multi_compile_sprite
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _Color;

            v2f vert (appdata_t IN)
            {
                v2f OUT;
                // スプライトの頂点変換
                OUT.vertex   = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                // 設定したカラーをベースにする
                fixed4 col = _Color;

                // スプライトのテクスチャから α(形状) を取り出し、合成する
                // RGB は無視し、形状だけ利用したいので、テクスチャのαだけ掛け算
                fixed4 sampleTex = tex2D(_MainTex, IN.texcoord);
                col.a *= sampleTex.a;

                // これによって「表示カラーは常に _Color 通り」＋「形状はスプライトのαどおり」
                return col;
            }
            ENDCG
        }
    }
}
