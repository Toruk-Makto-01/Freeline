Shader "UI/NoiseCutoff"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture (Siyah Beyaz)", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float _Progress;
            float4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Asıl resmi ve gürültü haritasını oku
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                fixed noise = tex2D(_NoiseTex, i.uv).r;

                // İlerleme yüzdesi, gürültü haritasındaki grilik değerini aşmadıysa o pikseli sil (görünmez yap)
                clip(_Progress - noise);

                return col;
            }
            ENDCG
        }
    }
}