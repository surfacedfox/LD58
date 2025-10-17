Shader "CharacterStackedToon"
{
	Properties
	{
		_ASEOutlineColor( "Outline Color", Color ) = (0,0,0,0)
		_ASEOutlineWidth( "Outline Width", Float ) = 0.0004
		_TextureStack("TextureStack", 2D) = "black" {}
		_BaseTexture("BaseTexture", 2D) = "white" {}
		_BaseColor("BaseColor", Color) = (1,1,1,0)
		_EarColor("EarColor", Color) = (0,0,0,0)
		_AccentColor("AccentColor", Color) = (0,0,0,0)
		_Yarny_AO("Yarny_AO", 2D) = "white" {}
		_Yarny_Albedo("Yarny_Albedo", 2D) = "white" {}
		_Yarnify("Yarnify", Range( 0 , 0.7)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ }
		Cull Front
		CGPROGRAM
		#pragma target 3.0
		#pragma surface outlineSurf Outline nofog  keepalpha noshadow noambient novertexlights nolightmap nodynlightmap nodirlightmap nometa noforwardadd vertex:outlineVertexDataFunc 		
		
		struct Input {
			half filler;
		};
		float4 _ASEOutlineColor;
		float _ASEOutlineWidth;
		void outlineVertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			v.vertex.xyz += ( v.normal * _ASEOutlineWidth );
		}
		inline half4 LightingOutline( SurfaceOutput s, half3 lightDir, half atten ) { return half4 ( 0,0,0, s.Alpha); }
		void outlineSurf( Input i, inout SurfaceOutput o )
		{
			o.Emission = _ASEOutlineColor.rgb;
			o.Alpha = 1;
		}
		ENDCG
		

		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows exclude_path:deferred 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _Yarny_Albedo;
		uniform float4 _Yarny_Albedo_ST;
		uniform float _Yarnify;
		uniform float4 _BaseColor;
		uniform sampler2D _BaseTexture;
		uniform float4 _BaseTexture_ST;
		uniform sampler2D _TextureStack;
		uniform float4 _TextureStack_ST;
		uniform float4 _EarColor;
		uniform float4 _AccentColor;
		uniform sampler2D _Yarny_AO;
		uniform float4 _Yarny_AO_ST;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float4 temp_cast_0 = (1.0).xxxx;
			float2 uv_Yarny_Albedo = i.uv_texcoord * _Yarny_Albedo_ST.xy + _Yarny_Albedo_ST.zw;
			float4 lerpResult20 = lerp( temp_cast_0 , tex2D( _Yarny_Albedo, uv_Yarny_Albedo ) , _Yarnify);
			float2 uv_BaseTexture = i.uv_texcoord * _BaseTexture_ST.xy + _BaseTexture_ST.zw;
			float2 uv_TextureStack = i.uv_texcoord * _TextureStack_ST.xy + _TextureStack_ST.zw;
			float4 tex2DNode2 = tex2D( _TextureStack, uv_TextureStack );
			float4 lerpResult4 = lerp( _BaseColor , tex2D( _BaseTexture, uv_BaseTexture ) , tex2DNode2.b);
			float4 lerpResult9 = lerp( lerpResult4 , _EarColor , tex2DNode2.r);
			float4 lerpResult11 = lerp( lerpResult9 , _AccentColor , tex2DNode2.g);
			o.Albedo = ( lerpResult20 * lerpResult11 ).rgb;
			float4 temp_cast_2 = (1.0).xxxx;
			float2 uv_Yarny_AO = i.uv_texcoord * _Yarny_AO_ST.xy + _Yarny_AO_ST.zw;
			float4 lerpResult21 = lerp( temp_cast_2 , tex2D( _Yarny_AO, uv_Yarny_AO ) , _Yarnify);
			o.Occlusion = lerpResult21.r;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}