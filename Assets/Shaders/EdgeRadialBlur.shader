Shader "Hidden/EdgeRadialBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurStrength ("Blur Strength", Float) = 0.5
        _Radius ("Blur Radius", Float) = 0.6
        _Samples ("Samples", Int) = 6
        _DynamicOffset ("Dynamic Offset", Float) = 0.0
        _Vignette ("Vignette Intensity", Float) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurStrength;
            float _Radius;
            int _Samples;
            float _DynamicOffset;
            float _Vignette;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 SampleRadialBlur(float2 uv)
            {
                float2 center = float2(0.5, 0.5);
                float2 dir = uv - center;
                float dist = length(dir) / _Radius; // 0..1-ish
                dist = saturate(dist);
                float strength = _BlurStrength * dist;
                // dynamic offset adds a little animated jitter
                float2 baseOffset = dir * (_DynamicOffset * 0.02);

                fixed4 col = tex2D(_MainTex, uv);
                // sample linearly outward a few steps
                int samples = max(1, _Samples);
                for (int i = 1; i <= samples; i++)
                {
                    float t = (i / (float)samples) * strength;
                    float2 sampleUV = uv - dir * t + baseOffset;
                    col += tex2D(_MainTex, sampleUV);
                }
                col /= (samples + 1);
                return col;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 blurred = SampleRadialBlur(uv);

                // vignette factor: stronger at edges
                float2 toCenter = uv - float2(0.5, 0.5);
                float vign = smoothstep(0.0, 1.0, length(toCenter) / _Radius);
                vign = pow(vign, 1.2) * _Vignette;

                // lerp between original and blurred by vignette
                fixed4 orig = tex2D(_MainTex, uv);
                fixed4 outc = lerp(orig, blurred, vign);
                return outc;
            }
            ENDCG
        }
    }
    FallBack Off
}
