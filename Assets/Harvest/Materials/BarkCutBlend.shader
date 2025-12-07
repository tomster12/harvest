Shader "Custom/BarkCutBlend"
{
    Properties
    {
        _BaseMap ("Bark Texture", 2D) = "white" {}
        _WoodColor ("Wood Color", Color) = (0.55, 0.40, 0.25, 1)
        _BarkColor ("Bark Color", Color) = (0.55, 0.40, 0.25, 1)
        _BarkStep ("Bark Step", Range(0, 1)) = 0.2
        _CutSharpness ("Cut Sharpness", Range(0.5, 8)) = 2
        _CavityDarken ("Cavity Darken", Range(0, 1)) = 0.3
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "UniversalMaterialType"="Lit" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float2 uv          : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _WoodColor;
                float4 _BarkColor;
                float _BarkStep;
                float _CutSharpness;
                float _CavityDarken;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 0 = bark, 0 = internal
                float internalPct = saturate(IN.uv.x);
                float internalStep = step(_BarkStep, pow(internalPct, _CutSharpness));
                
                float3 col = lerp(_BarkColor.rgb, _WoodColor.rgb, internalStep);
                
                float cavityDarken = internalPct * _CavityDarken;
                col *= (1.0 - cavityDarken);

                return float4(col, 1);
            }
            ENDHLSL
        }
    }
}
