Shader "Custom/BarkCutBlend"
{
    Properties
    {
        _BaseMap ("Bark Texture", 2D) = "white" {}
        _WoodColor ("Wood Color", Color) = (0.55, 0.40, 0.25, 1)
        _BarkColor ("Bark Color", Color) = (0.55, 0.40, 0.25, 1)
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
                // 1 = full bark, 0 = cut
                float barkPct = saturate(IN.uv.x);
                float barkT = pow(barkPct, _CutSharpness);

                float3 woodCol = _WoodColor.rgb;
                float3 barkCol = _BarkColor.rgb;
                float3 baseCol = lerp(woodCol, barkCol, barkT);

                float cavity = (1.0 - barkPct) * _CavityDarken;
                baseCol *= (1.0 - cavity);

                return float4(baseCol, 1);
            }
            ENDHLSL
        }
    }
}
