Shader "Sprites/DepthWrite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [ToggleOff] _AlphaTest ("Alpha Test", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma multi_compile_fog
            #pragma multi_compile DUMMY

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            fixed4 _Color;
            fixed4 _RendererColor;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _AlphaTest;
            float2 _Flip;

            v2f SpriteVert(appdata_t IN)
            {
                v2f OUT;

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color = IN.color * _Color * _RendererColor;
                UNITY_TRANSFER_FOG(OUT,OUT.vertex);
                return OUT;
            }

            fixed4 SpriteFrag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord);
                c.rgb *= IN.color.rgb;
                c.a *= IN.color.a;
                clip(c.a - (_AlphaTest * 0.001));
                UNITY_APPLY_FOG(IN.fogCoord, c);
                return c;
            }
        ENDCG
        }
    }

    Fallback "Sprites/Default"
}
