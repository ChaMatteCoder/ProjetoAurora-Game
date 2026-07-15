Shader "Aurora/TextMesh3DDepth"
{
    // Igual ao "GUI/Text Shader" do TextMesh legado, porém com ZTest LEqual:
    // o texto passa a ser OCLUIDO por paredes/blocos em vez de desenhar por cima.
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off
        Cull Off
        ZWrite Off
        ZTest LEqual
        Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Color [_Color]
            SetTexture [_MainTex]
            {
                combine primary, texture * primary
            }
        }
    }
}
