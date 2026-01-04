Shader "Custom/QuadSpritesheetLit"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Animation parameters
        _FrameCountX ("Frames Horizontal", Float) = 4
        _FrameCountY ("Frames Vertical", Float) = 4
        _CurrentFrame ("Current Frame", Float) = 0
        
        // PBR
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 300
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma prefer_hlsl3
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };
            
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _FrameCountX;
                float _FrameCountY;
                float _CurrentFrame;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float framesX = max(1.0, _FrameCountX);
                float framesY = max(1.0, _FrameCountY);
                float totalFrames = framesX * framesY;
                
                // Clamp and wrap frame
                float frameIndex = fmod(max(0.0, _CurrentFrame), totalFrames);
                
                // Calculate grid position
                float col = fmod(frameIndex, framesX);
                float row = floor(frameIndex / framesX);
                
                // Convert input UV [0,1] to frame UV
                float2 frameUV = input.texcoord;
                
                // Scale UV to frame size
                frameUV.x = frameUV.x / framesX;
                frameUV.y = frameUV.y / framesY;
                
                // Offset to correct frame
                frameUV.x += col / framesX;
                frameUV.y += (framesY - row - 1.0) / framesY;
                
                output.uv = frameUV;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionHCS = TransformWorldToHClip(positionWS);
                
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
                output.viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_TARGET
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 baseColor = texColor * _Color;
                
                // Alpha test
                clip(baseColor.a - 0.001);
                
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                // Main Light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = normalize(mainLight.direction);
                
                // Diffuse
                float NdotL = max(dot(normal, lightDir), 0.0);
                float3 diffuse = baseColor.rgb * mainLight.color * NdotL * mainLight.shadowAttenuation;
                
                // Specular (Blinn-Phong)
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(max(dot(normal, halfDir), 0.0), 32.0);
                float3 specular = spec * mainLight.color * mainLight.shadowAttenuation * 0.5;
                
                // Ambient (very minimal)
                float3 ambient = baseColor.rgb * 0.05;
                
                // Additional lights
                uint lightCount = GetAdditionalLightsCount();
                for(uint i = 0; i < lightCount; i++)
                {
                    Light light = GetAdditionalLight(i, input.positionWS);
                    float addNdotL = max(dot(normal, light.direction), 0.0);
                    diffuse += baseColor.rgb * light.color * addNdotL * light.shadowAttenuation;
                }
                
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
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
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
                output.positionHCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
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
}
                float _Smoothness;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Calculate UV offset for spritesheet animation
                float framesX = _FrameCount.x;
                float framesY = _FrameCount.y;
                float totalFrames = framesX * framesY;
                
                // Clamp frame index to valid range
                float frameIndex = clamp(_CurrentFrame, 0, totalFrames - 1);
                
                // Calculate row and column
                float col = fmod(frameIndex, framesX);
                float row = floor(frameIndex / framesX);
                
                // Calculate UV offset for spritesheet
                // UV 范围在 [0,1] 内，需要缩放到对应帧的范围
                float2 frameUV = input.texcoord;  // [0,1]
                
                // 计算该帧的 UV 范围起点
                float frameU = col / framesX;
                float frameV = row / framesY;
                
                // 将 UV 缩放到帧大小，然后加上偏移
                frameUV.x = (frameUV.x / framesX) + frameU;
                frameUV.y = 1.0 - ((frameUV.y / framesY) + frameV + (1.0 / framesY));  // 反转 Y 因为纹理坐标系统
                
                output.uv = frameUV;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionHCS = TransformWorldToHClip(positionWS);
                
                // Normal and tangent
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
                output.viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_TARGET
            {
                // Sample sprite texture with animation
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 baseColor = texColor * _Color;
                
                // Discard fully transparent pixels
                clip(baseColor.a - 0.001);
                
                // Calculate lighting
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                // Main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = normalize(mainLight.direction);
                
                // Diffuse
                float NdotL = max(dot(normal, lightDir), 0);
                float3 diffuse = baseColor.rgb * mainLight.color * NdotL * mainLight.shadowAttenuation;
                
                // Simple specular (Blinn-Phong)
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(max(dot(normal, halfDir), 0), 32);
                float3 specular = spec * mainLight.color * mainLight.shadowAttenuation * (1 - baseColor.a * 0.5);
                
                // Ambient (minimal)
                float3 ambient = baseColor.rgb * 0.1;
                
                // Additional lights
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0; lightIndex < pixelLightCount; lightIndex++)
                {
                    Light addLight = GetAdditionalLight(lightIndex, input.positionWS);
                    float addNdotL = max(dot(normal, addLight.direction), 0);
                    diffuse += baseColor.rgb * addLight.color * addNdotL * addLight.shadowAttenuation;
                }
                
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
                float4 _FrameCount;
                float _CurrentFrame;
            CBUFFER_END
            
            float3 _LightDirection;
            float3 _LightPositionWS;
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Calculate UV for spritesheet
                float framesX = _FrameCount.x;
                float framesY = _FrameCount.y;
                float totalFrames = framesX * framesY;
                float frameIndex = clamp(_CurrentFrame, 0, totalFrames - 1);
                float col = fmod(frameIndex, framesX);
                float row = floor(frameIndex / framesX);
                
                output.uv.x = input.texcoord.x / framesX + col / framesX;
                output.uv.y = input.texcoord.y / framesY + (1.0 - row - 1.0) / framesY;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                output.positionHCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
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
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
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
                float4 _FrameCount;
                float _CurrentFrame;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float framesX = _FrameCount.x;
                float framesY = _FrameCount.y;
                float totalFrames = framesX * framesY;
                float frameIndex = clamp(_CurrentFrame, 0, totalFrames - 1);
                float col = fmod(frameIndex, framesX);
                float row = floor(frameIndex / framesX);
                
                output.uv.x = input.texcoord.x / framesX + col / framesX;
                output.uv.y = input.texcoord.y / framesY + (1.0 - row - 1.0) / framesY;
                
                output.positionHCS = TransformWorldToHClip(TransformObjectToWorld(input.positionOS.xyz));
                
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
}
