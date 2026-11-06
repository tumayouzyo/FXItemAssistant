Shader "Hidden/TY/ImageAssistant/Resolve"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _ShapeMask ("Shape mask", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _BlackTex;
            sampler2D _WhiteTex;
            float4 _MainTex_TexelSize;
            int _Factor;
            int _Transparent;
            int _Linear;
            int _Checker;
            int _PreviewLinear;
            float4 _OutputSize;

            float4 frag(v2f_img input) : SV_Target
            {
                float3 premultiplied = 0;
                float coverage = 0;
                for (int y = 0; y < _Factor; y++)
                for (int x = 0; x < _Factor; x++)
                {
                    float2 uv = input.uv + (float2(x, y) + 0.5 - _Factor * 0.5) * abs(_MainTex_TexelSize.xy);
                    float3 dark = tex2D(_BlackTex, uv).rgb;
                    float alpha = 1;
                    if (_Transparent != 0)
                    {
                        float3 difference = tex2D(_WhiteTex, uv).rgb - dark;
                        alpha = saturate(1 - max(difference.r, max(difference.g, difference.b)));
                    }
                    premultiplied += dark;
                    coverage += alpha;
                }
                float alpha = coverage / (_Factor * _Factor);
                float3 color = coverage < 0.00001 ? 0 : saturate(premultiplied / coverage);
                if (_Linear != 0) color = LinearToGammaSpace(color);
                if (_Checker != 0)
                {
                    float2 square = floor(input.uv * _OutputSize.xy / 12);
                    float gray = fmod(square.x + square.y, 2) < 1 ? 0.22 : 0.29;
                    if (_PreviewLinear != 0) gray = GammaToLinearSpace(float3(gray, gray, gray)).r;
                    return float4(lerp(float3(gray, gray, gray), color, alpha), 1);
                }
                return float4(color, alpha);
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            sampler2D _ShapeMask;
            float4 _ShapeMask_TexelSize;
            float _ShadowStrength;
            float4 _OutputSize;
            int _Shape, _Shadow, _Grid, _SafeArea, _Checker, _Linear, _PreviewLinear;

            float ShapeCoverage(float2 uv, float2 size, float shorterSide)
            {
                float coverage = 1;
                if (_Shape == 3)
                    {
                    float2 edge = abs(_ShapeMask_TexelSize.xy) * 0.5;
                    coverage = saturate(dot(tex2Dlod(_ShapeMask, float4(clamp(uv, edge, 1 - edge), 0, 0)).rgb, float3(0.2126, 0.7152, 0.0722)));
                }
                float2 position = abs((uv - 0.5) * size);
                if (_Shape == 1)
                    coverage = saturate(shorterSide * 0.5 - length(position) + 0.5);
                if (_Shape == 2)
                {
                    float radius = shorterSide * 0.12;
                    float2 corner = position - size * 0.5 + radius;
                    float distance = length(max(corner, 0)) + min(max(corner.x, corner.y), 0) - radius;
                    coverage = saturate(0.5 - distance);
                }
                return coverage;
            }

            float4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                float2 size = _OutputSize.xy;
                float shorterSide = min(size.x, size.y);
                float4 item = tex2D(_MainTex, uv);
                if (_Shadow != 0)
                {
                    float shadow = 0;
                    float2 center = uv - float2(1, -1) * (0.018 * shorterSide / size);
                    [unroll] for (int y = -2; y <= 2; y++)
                    [unroll] for (int x = -2; x <= 2; x++)
                    {
                        float2 sampleUV = center + float2(x, y) * (0.009 * shorterSide / size);
                        if (all(sampleUV >= 0) && all(sampleUV <= 1))
                            shadow += tex2D(_MainTex, sampleUV).a * (3 - abs(x)) * (3 - abs(y));
                    }
                    float alpha = item.a + shadow * (0.42 * _ShadowStrength / 81.0) * (1 - item.a);
                    item.rgb *= item.a / max(alpha, 0.00001);
                    item.a = alpha;
                }
                item.a *= ShapeCoverage(uv, size, shorterSide);
                if (_Linear != 0) item.rgb = LinearToGammaSpace(item.rgb);
                if (_Checker != 0)
                {
                    float2 square = floor(uv * size / 12);
                    float gray = fmod(square.x + square.y, 2) < 1 ? 0.22 : 0.29;
                    if (_PreviewLinear != 0) gray = GammaToLinearSpace(float3(gray, gray, gray)).r;
                    item = float4(lerp(gray.xxx, item.rgb, item.a), 1);
                }
                float lineDistance = 10000;
                if (_Grid == 1 || _Grid == 2 || _Grid == 3)
                {
                    float splitA = _Grid == 1 ? 1.0 / 3.0 : (_Grid == 2 ? 0.25 : 0.381966);
                    float splitB = 1 - splitA;
                    float2 distance = min(abs(uv - splitA), abs(uv - splitB)) * size;
                    lineDistance = min(distance.x, distance.y);
                }
                if (_Grid == 4)
                {
                    float2 p = (uv - 0.5) * size;
                    lineDistance = min(abs(p.x - p.y), abs(p.x + p.y)) * 0.7071;
                }
                if (_Grid == 5)
                {
                    float2 p = (uv - 0.5) * size;
                    lineDistance = min(abs(p.x), abs(p.y));
                }
                float gridAlpha = saturate(1.5 - lineDistance) * 0.75;
                item.rgb = lerp(item.rgb, 1, gridAlpha);
                if (_SafeArea != 0)
                {
                    float2 texel = 1.0 / size;
                    float coverage = ShapeCoverage(uv, size, shorterSide);
                    float edgeCoverage = max(max(ShapeCoverage(uv + float2(texel.x, 0), size, shorterSide),
                        ShapeCoverage(uv - float2(texel.x, 0), size, shorterSide)),
                        max(ShapeCoverage(uv + float2(0, texel.y), size, shorterSide),
                        ShapeCoverage(uv - float2(0, texel.y), size, shorterSide)));
                    float safeLine = saturate(edgeCoverage - coverage) * 0.9;
                    item.rgb = lerp(item.rgb, 1, safeLine);
                }
                return item;
            }
            ENDCG
        }
    }
}
