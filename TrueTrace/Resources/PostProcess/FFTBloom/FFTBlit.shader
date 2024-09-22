//https://github.com/AKGWSB/FFTConvolutionBloom/blob/main/Assets/Scripts/ConvolutionBloom.cs
Shader "ConvolutionBloom/FFTBlit"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Blend One One
            CGPROGRAM
                #pragma vertex CommonVert
                #pragma fragment FinalBlitFrag
                #define FINALBLIT
                #include "FFTCommon.cginc"
            ENDCG
        }
        Pass
        {
            CGPROGRAM
                #pragma vertex CommonVert
                #pragma fragment SourceGenFrag
                #define FINALBLIT
                #include "FFTCommon.cginc"
            ENDCG
        }
        Pass
        {
            CGPROGRAM
                #pragma vertex CommonVert
                #pragma fragment KernGenFrag
                #define FINALBLIT
                #include "FFTCommon.cginc"
            ENDCG
        }
    }
}