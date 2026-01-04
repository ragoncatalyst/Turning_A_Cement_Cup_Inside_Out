Shader "Custom/QuadSpritesheetLit"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FrameCountX ("Frames Horizontal", Float) = 4
        _FrameCountY ("Frames Vertical", Float) = 4
        _CurrentFrame ("Current Frame", Float) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _FrameCountX;
                float _FrameCountY;
                float _CurrentFrame;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float framesX = max(1.0, _FrameCountX);
                float framesY = max(1.0, _FrameCountY);
                float totalFrames = framesX * framesY;
                
                float frameIndex = fmod(max(0.0, _CurrentFrame), totalFrames);
                float col = fmod(frameIndex, framesX);
                float row = floor(frameIndex / framesX);
                
                float2 frameUV = input.texcoord;
                frameUV.x = frameUV.x / framesX + col / framesX;
                frameUV.y = frameUV.y / framesY + (framesY - row - 1.0) / framesY;
                
                output.uv = frameUV;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_TARGET
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 baseColor = texColor * _Color;
                
                clip(baseColor.a - 0.001);
                
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = normalize(mainLight.direction);
                
                float NdotL = max(dot(normal, lightDir), 0.0);
                float3 diffuse = baseColor.rgb * mainLight.color * NdotL * mainLight.shadowAttenuation;
                
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(max(dot(normal, halfDir), 0.0), 32.0);
                float3 specular = spec * mainLight.color * mainLight.shadowAttenuation * 0.5;
                
                float3 ambient = baseColor.rgb * 0.05;
                float3 finalColor = ambient + diffuse + specular;
                return float4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _FrameCountX;
                float _FrameCountY;
                float _CurrentFrame;
            CBUFFER_END
            
            float3 _LightDirection;
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float framesX = max(1.0, _FrameCountX);
                float framesY = max(1.0, _FrameCountY);
                float frameIndex = fmod(max(0.0, _CurrentFrame), framesX * framesY);
                float col = fmod(frameIndex, framesX);
                float row = floor(frameIndex / framesX);
                
                float2 frameUV = input.texcoord;
                frameUV.x = frameUV.x / framesX + col / framesX;
                frameUV.y = frameUV.y / framesY + (framesY - row - 1.0) / framesY;
                output.uv = frameUV;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                float4 positionCS = TransformWorldToHClip(positionWS);
                positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                output.positionHCS = positionCS;
                
                return output;
            }
            
            float frag(Varyings input) : SV_TARGET
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(texColor.a * _Color.a - 0.5);
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}
