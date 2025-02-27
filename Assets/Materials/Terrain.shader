Shader "Universal Render Pipeline/Terrain/CustomURP"
{
    Properties
    {
        testTexture("Texture", 2D) = "white"
        testScale("Scale", float) = 1
    }
    SubShader
    {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            Name "ForwardLit"
            
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.worldPos = TransformObjectToWorld(v.vertex);
                o.normal = v.normal;
                return o;
            }

            const static int maxColorCount = 8;
            const static float epsilon = 1e-4;
            
            int layerCount;
            
            float3 baseColors[maxColorCount];
            float baseStartHeights[maxColorCount];
            float baseBlends[maxColorCount];
            float baseColorStrengths[maxColorCount];
            float baseTextureScales[maxColorCount];

            float minHeight;
            float maxHeight;
            
            sampler2D testTexture;
            float testScale;
            TEXTURE2D_ARRAY(baseTextures);
            SAMPLER(sampler_baseTextures);

            float inverseLerp(float a, float b, float value) {

                return saturate((value - a) / (b - a));

            }

            // float3 Lambert(float3 lightColor, float3 lightDir, float3 normal)
            // {
            //     float NdotL = saturate(dot(normal, lightDir));
            //     return lightColor * NdotL;
            // }
            
            float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {

                float3 scaledWorldPos = worldPos / scale;
                float3 xProjection = SAMPLE_TEXTURE2D_ARRAY(baseTextures, sampler_baseTextures, scaledWorldPos.yz, textureIndex) * blendAxes.x;
                float3 yProjection = SAMPLE_TEXTURE2D_ARRAY(baseTextures, sampler_baseTextures, scaledWorldPos.xz, textureIndex) * blendAxes.y;
                float3 zProjection = SAMPLE_TEXTURE2D_ARRAY(baseTextures, sampler_baseTextures, scaledWorldPos.xy, textureIndex) * blendAxes.z;
                
                return xProjection + yProjection + zProjection;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float heightPercent = inverseLerp(minHeight, maxHeight, i.worldPos.y);
                float3 lightPos = _MainLightPosition.xyz;
                // float3 lightCol = Lambert(_MainLightColor * unity_LightData.z, lightPos, i.normal);
                float3 worldPos = i.worldPos;
                float3 blendAxes = abs(i.normal);
                blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
                uint lightsCount = GetAdditionalLightsCount();
                
                // for (int j = 0; j < lightsCount; j++)
                // {
                //     Light light = GetAdditionalLight(j, i.worldPos);
                //     lightCol += Lambert(light.color * (light.distanceAttenuation * light.shadowAttenuation), light.direction, i.normal);
                // }
                
                float3 color = float3(heightPercent, heightPercent, heightPercent);
                
                for (int i = 0; i < layerCount; i++) {
                    float drawStrength = inverseLerp(-baseBlends[i] /2 - epsilon, baseBlends[i] / 2, heightPercent - baseStartHeights[i]);
                    float3 baseColor = baseColors[i] * baseColorStrengths[i];
                    float3 textureColor = triplanar(worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColorStrengths[i]);
                    color = color * (1 - drawStrength) + drawStrength * (baseColor + textureColor);
                }
               // color = xProjection + yProjection + zProjection;
                
                // color.rgb += lightCol;
                
                return float4(color,1);
            }
            ENDHLSL
        }
    }
}