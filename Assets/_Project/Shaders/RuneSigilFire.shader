Shader "FantasyShapez/Rune Sigil Fire"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sigil", 2D) = "white" {}
        _GradientTex ("Fire Gradient", 2D) = "white" {}
        _UseGradient ("Use Fire Gradient", Float) = 0
        _OutlineWidth ("Outline Width (Pixels)", Range(0, 5)) = 2
        _OutlineColor ("Outline Color", Color) = (0.12, 0.07, 0.2, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_GradientTex);
            SAMPLER(sampler_GradientTex);
            float4 _MainTex_TexelSize;
            half _UseGradient;
            half _OutlineWidth;
            half4 _OutlineColor;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;
                half nearby = 0;
                nearby = max(nearby, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(offset.x, 0)).a);
                nearby = max(nearby, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(offset.x, 0)).a);
                nearby = max(nearby, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, offset.y)).a);
                nearby = max(nearby, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(0, offset.y)).a);
                nearby = max(nearby, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset).a);
                nearby = max(nearby, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - offset).a);
                nearby = max(nearby, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(offset.x, -offset.y)).a);
                nearby = max(nearby, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-offset.x, offset.y)).a);

                half outline = saturate(nearby - mask) * _OutlineColor.a;
                half alpha = saturate(mask + outline) * input.color.a;
                half3 fire = SAMPLE_TEXTURE2D(_GradientTex, sampler_GradientTex, input.uv).rgb;
                half3 fill = lerp(input.color.rgb, fire * input.color.rgb, _UseGradient);
                half3 rgb = (fill * mask + _OutlineColor.rgb * outline) / max(mask + outline, 0.0001h);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
