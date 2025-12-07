Shader "Custom/BarkCutBlendShadow"
{
    Properties
    {
        _BarkTex ("Bark Texture", 2D) = "white" {}
        _WoodColor ("Wood Color", Color) = (0.72, 0.62, 0.50, 1)
        _BarkColor ("Bark Color", Color) = (0.42, 0.37, 0.31, 1)
        _BarkEdgeColor ("Bark Edge Color", Color) = (0.49, 0.42, 0.35, 1) 
        _BarkThreshold ("Bark Threshold", Range(0, 1)) = 0.02
        _BarkEdgeWidth ("Bark Edge Width", Range(0, 0.2)) = 0.04
        _DarkenAmount ("Darken Amount", Range(0, 1)) = 0.35
        _DarkenThreshold ("Darken Threshold", Range(0, 1)) = 0.35
        _ShadowSoftness ("Shadow Softness", Range(0, 1)) = 0.64
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "UniversalMaterialType" = "Lit" }


        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 shadowCoords : TEXCOORD2;
            };


            CBUFFER_START(UnityPerMaterial)
            float4 _BarkTex_ST;
            float4 _WoodColor;
            float4 _BarkColor;
            float4 _BarkEdgeColor;
            float _BarkThreshold;
            float _BarkEdgeWidth;
            float _DarkenAmount;
            float _DarkenThreshold;
            float _ShadowSoftness;
            CBUFFER_END

            TEXTURE2D(_BarkTex);
            SAMPLER(sampler_BarkTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                VertexPositionInputs vpos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.shadowCoords = GetShadowCoord(vpos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BarkTex);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float depthPct = IN.uv.x;
                float fullBarkThreshold = _BarkThreshold + _BarkEdgeWidth;

                // == Bark to wood colour with darkening ==
                float3 baseColor;
                if (depthPct < _BarkThreshold)
                {
                    baseColor = _BarkColor.rgb;
                }
                else if (depthPct < fullBarkThreshold)
                {
                    baseColor = _BarkEdgeColor.rgb;
                }
                else
                {
                    float darkenPct = saturate((depthPct - fullBarkThreshold) / (_DarkenThreshold - fullBarkThreshold));
                    float depthDarken = 1.0 - _DarkenAmount * darkenPct;
                    baseColor = _WoodColor.rgb * depthDarken;
                }

                // == Lighting & Shadow ==
                Light mainLight = GetMainLight(IN.shadowCoords);
                float NdotL = saturate(dot(IN.normalWS, mainLight.direction));
                float shadowDarken = mainLight.distanceAttenuation * mainLight.shadowAttenuation * NdotL;
                shadowDarken = lerp(1.0, shadowDarken, _ShadowSoftness);

                float3 finalCol = baseColor * mainLight.color * shadowDarken;
                return float4(finalCol, 1);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_shadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                return OUT;
            }

            half4 frag_shadow(Varyings IN) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }
}
