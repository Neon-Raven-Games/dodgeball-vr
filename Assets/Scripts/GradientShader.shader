Shader "Custom/GradientShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BottomColor ("Bottom Color", Color) = (1, 0, 0, 1)
        _TopColor ("Top Color", Color) = (0, 0, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha // Enable alpha blending
        ZWrite On // Disable depth writing for transparent objects
        Cull Off // Enable double-sided rendering
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            fixed4 _BottomColor;
            fixed4 _TopColor;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float gradientFactor = v.vertex.y; // Assuming Y is the vertical axis
                o.color = lerp(_BottomColor, _TopColor, gradientFactor);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                fixed4 finalColor = texColor * i.color; // Multiply texture color with gradient color
                clip(finalColor.a - _Cutoff); // Apply alpha cutoff
                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}