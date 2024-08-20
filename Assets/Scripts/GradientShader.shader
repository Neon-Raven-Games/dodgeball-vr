Shader "Custom/GradientShader"
{
    Properties
    {
        _BottomColor ("Bottom Color", Color) = (1, 0, 0, 1)
        _TopColor ("Top Color", Color) = (0, 0, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha // Enable alpha blending
        ZWrite Off // Disable depth writing for transparent objects
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
                float4 color : COLOR;
            };

            fixed4 _BottomColor;
            fixed4 _TopColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float gradientFactor = v.vertex.y; // Assuming Y is the vertical axis
                o.color = lerp(_BottomColor, _TopColor, gradientFactor);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color; // Output the color with alpha
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}