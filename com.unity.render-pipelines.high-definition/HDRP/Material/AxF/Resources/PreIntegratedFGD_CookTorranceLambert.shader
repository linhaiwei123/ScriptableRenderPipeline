Shader "Hidden/HDRenderPipeline/PreIntegratedFGD_CookTorranceLambert"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #include "CoreRP/ShaderLibrary/Common.hlsl"
            #include "CoreRP/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "../../../ShaderVariables.hlsl"

            // ==============================================================================================
            // Pre-Integration Code
            //

            // ----------------------------------------------------------------------------
            // Importance sampling BSDF functions
            // ----------------------------------------------------------------------------
            // Formulas come from https://agraphicsguy.wordpress.com/2015/11/01/sampling-microfacet-brdf/ for the Beckmann normal distribution
            //
            void SampleCookTorranceDir( real2   u,
                                        real3   V,
                                        real3x3 localToWorld,
                                        real    roughness,
                                    out real3   L,
                                    out real    NdotL,
                                    out real    NdotH,
                                    out real    VdotH )
            {
                // Cook-Torrance NDF sampling
                real    cosTheta = rsqrt( 1 - Sq(roughness) * log( max( 1e-6, u.x ) ) );
                real    phi      = TWO_PI * u.y;

                real3   localH = SphericalToCartesian( phi, cosTheta );

                NdotH = cosTheta;

                real3   localV = mul( V, transpose(localToWorld) );
                VdotH  = saturate( dot( localV, localH ) );

                // Compute { localL = reflect(-localV, localH) }
                real3   localL = -localV + 2.0 * VdotH * localH;
                NdotL = localL.z;

                L = mul( localL, localToWorld );
            }

            float CT_G( float N_H, float N_V, float N_L, float H_V ) {
                return min( 1.0, 2.0 * N_H * min( N_V, N_L ) / H_V );
            }

            // weightOverPdf returns the weight (without the Fresnel term) over pdf. Fresnel term must be applied by the caller.
            void ImportanceSampleCookTorrance(  real2   u,
                                                real3   V,
                                                real3x3 localToWorld,
                                                real    roughness,
                                                real    NdotV,
                                            out real3   L,
                                            out real    VdotH,
                                            out real    NdotL,
                                            out real    weightOverPdf)
            {
                real    NdotH;
                SampleCookTorranceDir( u, V, localToWorld, roughness, L, NdotL, NdotH, VdotH );

                // Importance sampling weight for each sample
                // pdf = D(H) * (N.H) / (4 * (L.H))
                // fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
                // weight over pdf is:
                // weightOverPdf = fr * (N.V) / pdf
                // weightOverPdf = F(H) * G(V, L) * (L.H) / ((N.H) * (N.V))
                // F(H) is applied outside the function
                //
                weightOverPdf =  CT_G( NdotH, NdotV, NdotL, VdotH ) * VdotH / (NdotH * NdotV);
            }

            float4  IntegrateCookTorranceAndLambertDiffuseFGD( float3 V, float3 N, float roughness, uint sampleCount = 8192 ) {
                float   NdotV    = ClampNdotV( dot(N, V) );
                float4  acc      = float4(0.0, 0.0, 0.0, 0.0);
                float2  randNum  = InitRandom( V.xy * 0.5 + 0.5 );  // Add some jittering on Hammersley2d

                float3x3    localToWorld = GetLocalFrame( N );

                for ( uint i = 0; i < sampleCount; ++i ) {
                    float2  u = frac( randNum + Hammersley2d( i, sampleCount ) );

                    float   VdotH;
                    float   NdotL;
                    float   weightOverPdf;

                    float3  L; // Unused
                    ImportanceSampleCookTorrance(   u, V, localToWorld, roughness, NdotV,
                                                    L, VdotH, NdotL, weightOverPdf );

                    if ( NdotL > 0.0 ) {
                        // Integral{BSDF * <N,L> dw} =
                        // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
                        // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw}=
                        // (1 - F0) * x + F0 * y = lerp(x, y, F0)
                        acc.x += weightOverPdf * pow( 1 - VdotH, 5 );
                        acc.y += weightOverPdf;
                    }

                    // Regular Lambert
                    ImportanceSampleLambert( u, localToWorld, L, NdotL, weightOverPdf );

                    if ( NdotL > 0.0 ) {
                        acc.z += LambertNoPI() * weightOverPdf;
                    }
                }

                acc /= sampleCount;

                return acc;
            }

            // ==============================================================================================
            //
            struct Attributes {
                uint vertexID : SV_VertexID;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 texCoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes input) {
                Varyings output;

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texCoord   = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target {
                // These coordinate sampling must match the decoding in GetPreIntegratedDFG in lit.hlsl, i.e here we use perceptualRoughness, must be the same in shader
                float   NdotV               = input.texCoord.x;
                float   perceptualRoughness = input.texCoord.y;
                float3  V                   = float3(sqrt(1 - NdotV * NdotV), 0, NdotV);
                float3  N                   = float3(0.0, 0.0, 1.0);

                float4 preFGD = IntegrateCookTorranceAndLambertDiffuseFGD( V, N, PerceptualRoughnessToRoughness(perceptualRoughness) );

                return float4(preFGD.xyz, 1.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
