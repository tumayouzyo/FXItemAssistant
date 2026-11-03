Shader "Hidden/TY/ImageAssistant/Resolve"
{
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
    }
}
