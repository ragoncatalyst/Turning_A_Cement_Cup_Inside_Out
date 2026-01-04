Shader "Custom/SimpleQuadSpritesheet"
{
    Properties
    {
        _MainTex ("Sprite Sheet", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _FrameCountX ("Frames X", Float) = 4
        _FrameCountY ("Frames Y", Float) = 4
        _CurrentFrame ("Current Frame", Float) = 0
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _FrameCountX;
            float _FrameCountY;
            float _CurrentFrame;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                
                // Spritesheet UV
                float framesX = max(1.0, _FrameCountX);
                float framesY = max(1.0, _FrameCountY);
                
                float frameIdx = fmod(_CurrentFrame, framesX * framesY);
                float col = fmod(frameIdx, framesX);
                float row = floor(frameIdx / framesX);
                
                float2 frameUV = v.uv;
                frameUV.x = frameUV.x / framesX + col / framesX;
                frameUV.y = 1.0 - (frameUV.y / framesY + (row + 1.0) / framesY);
                
                o.uv = frameUV;
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col *= _Color;
                
                // Simple directional light
                float3 lightDir = normalize(float3(0.3, 0.5, 0.8));
                float3 normal = normalize(i.normal);
                float diffuse = max(0, dot(normal, lightDir));
                
                col.rgb = col.rgb * (diffuse + 0.3);
                
                return col;
            }
            ENDCG
        }
    }
}
