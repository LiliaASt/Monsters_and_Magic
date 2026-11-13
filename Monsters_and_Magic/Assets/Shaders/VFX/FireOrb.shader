Shader "Monsters and Magic/VFX/Fire Orb"
{
    Properties
    {
        _HotColor ("Hot Core", Color) = (3.4, 2.0, 0.62, 1.0)
        _MidColor ("Mid Flame", Color) = (2.2, 0.78, 0.15, 1.0)
        _EdgeColor ("Cool Edge", Color) = (0.88, 0.12, 0.02, 1.0)
        _NoiseScale ("Noise Scale", Float) = 4.5
        _NoiseSpeed ("Noise Speed", Float) = 1.6
        _WarpStrength ("Warp Strength", Float) = 1.4
        _HeatBias ("Heat Bias", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            Name "FireOrbUnlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HotColor;
                half4 _MidColor;
                half4 _EdgeColor;
                float _NoiseScale;
                float _NoiseSpeed;
                float _WarpStrength;
                float _HeatBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = normalize(GetWorldSpaceViewDir(positionWS));
                return OUT;
            }

            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash22(i).x;
                float b = Hash22(i + float2(1.0, 0.0)).x;
                float c = Hash22(i + float2(0.0, 1.0)).x;
                float d = Hash22(i + float2(1.0, 1.0)).x;

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.55;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * ValueNoise(p);
                    p = p * 2.03 + float2(17.3, 9.1);
                    amplitude *= 0.5;
                }

                return value;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.normalWS);
                float facing = saturate(dot(normal, normalize(IN.viewDirWS)));

                // Spherical coordinates from the world-space normal so the
                // noise wraps around the sphere without seam-fighting UVs.
                float longitude = atan2(normal.z, normal.x);
                float latitude = asin(clamp(normal.y, -1.0, 1.0));
                float2 sphereUV = float2(longitude, latitude) * (1.0 / 3.14159265);

                float t = _Time.y * _NoiseSpeed;

                // Domain warp: first noise field drags the flame noise around,
                // which is what makes the fire crawl over the surface.
                float2 warp = float2(
                    Fbm(sphereUV * _NoiseScale * 0.6 + float2(t * 0.7, -t * 0.4)),
                    Fbm(sphereUV * _NoiseScale * 0.6 + float2(-t * 0.5, t * 0.8) + 31.7));

                // Flame licks drift upward over the sphere like real fire.
                float2 fireUV = sphereUV + float2(0.0, -t * 0.22);
                float flame = Fbm(fireUV * _NoiseScale + warp * _WarpStrength
                    + float2(t, -t * 0.6));

                // Heat rises to the top of the sphere (lava-lamp feel) and
                // flickers with the warped noise.
                float upward = saturate(normal.y * 0.5 + 0.5);
                float heat = saturate(
                    facing * 0.4 +
                    upward * 0.18 +
                    flame * 0.6 - 0.16 + _HeatBias);

                half3 color = lerp(_EdgeColor.rgb, _MidColor.rgb, smoothstep(0.12, 0.5, heat));
                color = lerp(color, _HotColor.rgb, smoothstep(0.58, 0.95, heat));

                // Soft silhouette: the rim fades out so the sphere has no
                // hard cut edge, the hottest core pushes into HDR for bloom,
                // and the whole orb flickers with the fire noise.
                float rim = smoothstep(0.0, 0.45, facing);
                float flicker = 0.88 + 0.12 * sin(_Time.y * 6.3 + flame * 5.0);
                float alpha = saturate(heat * 1.4) * lerp(0.35, 1.0, rim) * flicker;

                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
