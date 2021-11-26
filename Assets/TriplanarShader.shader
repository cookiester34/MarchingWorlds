// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TriplanarShader"
{
	Properties
	{
		_BottomTextureNormal("BottomTextureNormal", 2D) = "white" {}
		_TopTextureNormal("TopTextureNormal", 2D) = "white" {}
		_MiddleTextureNormal("MiddleTextureNormal", 2D) = "white" {}
		_TopTextureGrass("TopTextureGrass", 2D) = "white" {}
		_TopTextureSnow("TopTextureSnow", 2D) = "white" {}
		_MiddleTexture("MiddleTexture", 2D) = "white" {}
		_BottomTexture("BottomTexture", 2D) = "white" {}
		_WaterCutoffTexture("WaterCutoffTexture", 2D) = "white" {}
		_NormalCutoffTexture("NormalCutoffTexture", 2D) = "white" {}
		_Offset("Offset", Vector) = (1,1,0,0)
		_Falloff("Falloff", Int) = 5
		_ColourVariation("ColourVariation", 2D) = "white" {}
		_AntiTiling1("AntiTiling1", Float) = 0
		_AntiTiling2("AntiTiling2", Float) = 0
		_AntiTiling3("AntiTiling3", Float) = 0
		_GrassCutoff("GrassCutoff", Float) = 50
		_WaterCutoff("WaterCutoff", Float) = 30
		_NormalCutoff("NormalCutoff", Float) = 45
		_Alpha("Alpha", Float) = 0
		_Metallic("Metallic", Float) = 0
		_Smoothness("Smoothness", Float) = 0
		_AmbientOcclusion("AmbientOcclusion", Float) = 0
		_NormalStrength("NormalStrength", Float) = 2
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 5.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
			float2 uv_texcoord;
		};

		uniform sampler2D _TopTextureNormal;
		uniform sampler2D _MiddleTextureNormal;
		uniform sampler2D _BottomTextureNormal;
		uniform float2 _Offset;
		uniform int _Falloff;
		uniform float _NormalStrength;
		uniform sampler2D _ColourVariation;
		uniform float _AntiTiling1;
		uniform float _AntiTiling3;
		uniform float _AntiTiling2;
		uniform float _NormalCutoff;
		uniform sampler2D _NormalCutoffTexture;
		uniform float4 _NormalCutoffTexture_ST;
		uniform float _GrassCutoff;
		uniform sampler2D _TopTextureSnow;
		uniform sampler2D _MiddleTexture;
		uniform sampler2D _BottomTexture;
		uniform sampler2D _TopTextureGrass;
		uniform float _Alpha;
		uniform float _WaterCutoff;
		uniform sampler2D _WaterCutoffTexture;
		uniform float4 _WaterCutoffTexture_ST;
		uniform float _Metallic;
		uniform float _Smoothness;
		uniform float _AmbientOcclusion;


		inline float4 TriplanarSampling190( sampler2D topTexMap, sampler2D midTexMap, sampler2D botTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index )
		{
			float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
			projNormal /= ( projNormal.x + projNormal.y + projNormal.z ) + 0.00001;
			float3 nsign = sign( worldNormal );
			float negProjNormalY = max( 0, projNormal.y * -nsign.y );
			projNormal.y = max( 0, projNormal.y * nsign.y );
			half4 xNorm; half4 yNorm; half4 yNormN; half4 zNorm;
			xNorm  = tex2D( midTexMap, tiling * worldPos.zy * float2(  nsign.x, 1.0 ) );
			yNorm  = tex2D( topTexMap, tiling * worldPos.xz * float2(  nsign.y, 1.0 ) );
			yNormN = tex2D( botTexMap, tiling * worldPos.xz * float2(  nsign.y, 1.0 ) );
			zNorm  = tex2D( midTexMap, tiling * worldPos.xy * float2( -nsign.z, 1.0 ) );
			return xNorm * projNormal.x + yNorm * projNormal.y + yNormN * negProjNormalY + zNorm * projNormal.z;
		}


		inline float4 TriplanarSampling82( sampler2D topTexMap, sampler2D midTexMap, sampler2D botTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index )
		{
			float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
			projNormal /= ( projNormal.x + projNormal.y + projNormal.z ) + 0.00001;
			float3 nsign = sign( worldNormal );
			float negProjNormalY = max( 0, projNormal.y * -nsign.y );
			projNormal.y = max( 0, projNormal.y * nsign.y );
			half4 xNorm; half4 yNorm; half4 yNormN; half4 zNorm;
			xNorm  = tex2D( midTexMap, tiling * worldPos.zy * float2(  nsign.x, 1.0 ) );
			yNorm  = tex2D( topTexMap, tiling * worldPos.xz * float2(  nsign.y, 1.0 ) );
			yNormN = tex2D( botTexMap, tiling * worldPos.xz * float2(  nsign.y, 1.0 ) );
			zNorm  = tex2D( midTexMap, tiling * worldPos.xy * float2( -nsign.z, 1.0 ) );
			return xNorm * projNormal.x + yNorm * projNormal.y + yNormN * negProjNormalY + zNorm * projNormal.z;
		}


		inline float4 TriplanarSampling1( sampler2D topTexMap, sampler2D midTexMap, sampler2D botTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index )
		{
			float3 projNormal = ( pow( abs( worldNormal ), falloff ) );
			projNormal /= ( projNormal.x + projNormal.y + projNormal.z ) + 0.00001;
			float3 nsign = sign( worldNormal );
			float negProjNormalY = max( 0, projNormal.y * -nsign.y );
			projNormal.y = max( 0, projNormal.y * nsign.y );
			half4 xNorm; half4 yNorm; half4 yNormN; half4 zNorm;
			xNorm  = tex2D( midTexMap, tiling * worldPos.zy * float2(  nsign.x, 1.0 ) );
			yNorm  = tex2D( topTexMap, tiling * worldPos.xz * float2(  nsign.y, 1.0 ) );
			yNormN = tex2D( botTexMap, tiling * worldPos.xz * float2(  nsign.y, 1.0 ) );
			zNorm  = tex2D( midTexMap, tiling * worldPos.xy * float2( -nsign.z, 1.0 ) );
			return xNorm * projNormal.x + yNorm * projNormal.y + yNormN * negProjNormalY + zNorm * projNormal.z;
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float3 ase_objectScale = float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) );
			float2 Offset89 = _Offset;
			float2 uv_TexCoord182 = i.uv_texcoord * ase_objectScale.xy + Offset89;
			float cos186 = cos( (float)radians( 90 ) );
			float sin186 = sin( (float)radians( 90 ) );
			float2 rotator186 = mul( uv_TexCoord182 - float2( 0.5,0.5 ) , float2x2( cos186 , -sin186 , sin186 , cos186 )) + float2( 0.5,0.5 );
			int Falloff90 = _Falloff;
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float4 triplanar190 = TriplanarSampling190( _TopTextureNormal, _MiddleTextureNormal, _BottomTextureNormal, ase_worldPos, ase_worldNormal, (float)Falloff90, rotator186, float3( 1,1,1 ), float3(0,0,0) );
			float4 appendResult198 = (float4(1.0 , triplanar190.y , 0.0 , triplanar190.r));
			o.Normal = UnpackScaleNormal( appendResult198, _NormalStrength );
			float2 temp_cast_3 = (_AntiTiling1).xx;
			float2 uv_TexCoord4_g7 = i.uv_texcoord * temp_cast_3;
			float2 temp_cast_4 = (_AntiTiling3).xx;
			float2 uv_TexCoord6_g7 = i.uv_texcoord * temp_cast_4;
			float2 temp_cast_5 = (_AntiTiling2).xx;
			float2 uv_TexCoord5_g7 = i.uv_texcoord * temp_cast_5;
			float3 lerpResult15_g7 = lerp( float3(0.5,0.5,0.5) , float3( 1,1,1 ) , ( ( ( tex2D( _ColourVariation, uv_TexCoord4_g7 ).r + 0.5 ) * ( tex2D( _ColourVariation, uv_TexCoord6_g7 ).r + 0.5 ) ) * ( tex2D( _ColourVariation, uv_TexCoord5_g7 ).r + 0.5 ) ));
			float3 ase_vertexNormal = mul( unity_WorldToObject, float4( ase_worldNormal, 0 ) );
			float dotResult172 = dot( float3( 0,1,0 ) , ase_vertexNormal );
			float2 uv_NormalCutoffTexture = i.uv_texcoord * _NormalCutoffTexture_ST.xy + _NormalCutoffTexture_ST.zw;
			float4 tex2DNode124 = tex2D( _NormalCutoffTexture, uv_NormalCutoffTexture );
			float2 uv_TexCoord75 = i.uv_texcoord * ase_objectScale.xy + Offset89;
			float cos80 = cos( (float)radians( 90 ) );
			float sin80 = sin( (float)radians( 90 ) );
			float2 rotator80 = mul( uv_TexCoord75 - float2( 0.5,0.5 ) , float2x2( cos80 , -sin80 , sin80 , cos80 )) + float2( 0.5,0.5 );
			float4 triplanar82 = TriplanarSampling82( _TopTextureSnow, _MiddleTexture, _BottomTexture, ase_worldPos, ase_worldNormal, (float)Falloff90, rotator80, float3( 1,1,1 ), float3(0,0,0) );
			float4 TriplanarSnow83 = triplanar82;
			float2 uv_TexCoord17 = i.uv_texcoord * ase_objectScale.xy + Offset89;
			float cos31 = cos( (float)radians( 90 ) );
			float sin31 = sin( (float)radians( 90 ) );
			float2 rotator31 = mul( uv_TexCoord17 - float2( 0.5,0.5 ) , float2x2( cos31 , -sin31 , sin31 , cos31 )) + float2( 0.5,0.5 );
			float4 triplanar1 = TriplanarSampling1( _TopTextureGrass, _MiddleTexture, _BottomTexture, ase_worldPos, ase_worldNormal, (float)Falloff90, rotator31, float3( 1,1,1 ), float3(0,0,0) );
			float4 Triplanar62 = triplanar1;
			float GrassCutoff136 = _WaterCutoff;
			float clampResult142 = clamp( ( _Alpha / ( GrassCutoff136 - ase_worldPos.y ) ) , 0.0 , 1.0 );
			float4 lerpResult133 = lerp( TriplanarSnow83 , Triplanar62 , clampResult142);
			float2 uv_WaterCutoffTexture = i.uv_texcoord * _WaterCutoffTexture_ST.xy + _WaterCutoffTexture_ST.zw;
			float4 ifLocalVar85 = 0;
			if( ase_worldPos.y > GrassCutoff136 )
				ifLocalVar85 = Triplanar62;
			else if( ase_worldPos.y < GrassCutoff136 )
				ifLocalVar85 = tex2D( _WaterCutoffTexture, uv_WaterCutoffTexture );
			float4 ifLocalVar66 = 0;
			if( ase_worldPos.y > _GrassCutoff )
				ifLocalVar66 = lerpResult133;
			else if( ase_worldPos.y < _GrassCutoff )
				ifLocalVar66 = ifLocalVar85;
			float4 ifLocalVar122 = 0;
			if( saturate( ( dotResult172 / 0.6 ) ) >= _NormalCutoff )
				ifLocalVar122 = tex2DNode124;
			else
				ifLocalVar122 = ifLocalVar66;
			o.Albedo = ( float4( lerpResult15_g7 , 0.0 ) * ifLocalVar122 ).rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Occlusion = _AmbientOcclusion;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18912
193;125;1344;579;1407.828;-760.1973;1;True;False
Node;AmplifyShaderEditor.Vector2Node;71;-3734.99,-379.6416;Inherit;False;Property;_Offset;Offset;9;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RegisterLocalVarNode;89;-3536.095,-377.5111;Inherit;False;Offset;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.IntNode;33;-2959.246,173.9039;Inherit;False;Constant;_Int0;Int 0;6;0;Create;True;0;0;0;False;0;False;90;0;False;0;1;INT;0
Node;AmplifyShaderEditor.ObjectScaleNode;36;-3123.567,-109.0147;Inherit;False;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;93;-3188.413,-874.0014;Inherit;False;89;Offset;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;86;-3730.651,-479.2919;Inherit;False;Property;_WaterCutoff;WaterCutoff;17;0;Create;True;0;0;0;False;0;False;30;18.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;95;-3124.853,38.47626;Inherit;False;89;Offset;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;13;-3732.326,-156.9944;Inherit;True;Property;_MiddleTexture;MiddleTexture;5;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TexturePropertyNode;14;-3731.021,28.33943;Inherit;True;Property;_BottomTexture;BottomTexture;6;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.IntNode;70;-3022.441,-737.6375;Inherit;False;Constant;_Int2;Int 2;6;0;Create;True;0;0;0;False;0;False;90;0;False;0;1;INT;0
Node;AmplifyShaderEditor.IntNode;76;-3733.661,-241.2477;Inherit;False;Property;_Falloff;Falloff;10;0;Create;True;0;0;0;False;0;False;5;7;False;0;1;INT;0
Node;AmplifyShaderEditor.ObjectScaleNode;72;-3186.762,-1020.555;Inherit;False;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector2Node;32;-2846.246,56.90365;Inherit;False;Constant;_Vector0;Vector 0;6;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;17;-2895.354,-61.21667;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RadiansOpNode;34;-2812.246,177.9039;Inherit;False;1;0;INT;0;False;1;INT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;91;-3466.695,-153.2064;Inherit;False;MiddleTexture;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.Vector2Node;73;-2909.441,-854.6374;Inherit;False;Constant;_Vector2;Vector 2;6;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RadiansOpNode;74;-2875.441,-733.6375;Inherit;False;1;0;INT;0;False;1;INT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;136;-3529.45,-476.9579;Inherit;False;GrassCutoff;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;92;-3459.348,29.26143;Inherit;False;BottomTexture;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;90;-3510.095,-242.511;Inherit;False;Falloff;-1;True;1;0;INT;0;False;1;INT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;75;-2958.549,-972.7576;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode;80;-2730.44,-873.6374;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;78;-2743.794,-1448.585;Inherit;True;Property;_TopTextureSnow;TopTextureSnow;4;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.GetLocalVarNode;102;-2673.111,-1176.782;Inherit;False;92;BottomTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode;101;-2673.939,-1255.62;Inherit;False;91;MiddleTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.WorldPosInputsNode;79;-2911.917,-1135.794;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;99;-2627.063,-338.5213;Inherit;False;91;MiddleTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode;94;-2715.734,-755.8455;Inherit;False;90;Falloff;1;0;OBJECT;;False;1;INT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;16;-2848.722,-224.2532;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RotatorNode;31;-2667.245,37.90354;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldPosInputsNode;134;-2139.788,151.7554;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;138;-2143.126,294.7665;Inherit;False;136;GrassCutoff;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;100;-2627.063,-265.5213;Inherit;False;92;BottomTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TexturePropertyNode;12;-2722.899,-529.1417;Inherit;True;Property;_TopTextureGrass;TopTextureGrass;3;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.GetLocalVarNode;96;-2645.653,160.7724;Inherit;False;90;Falloff;1;0;OBJECT;;False;1;INT;0
Node;AmplifyShaderEditor.RangedFloatNode;146;-1948.919,351.6027;Inherit;False;Property;_Alpha;Alpha;19;0;Create;True;0;0;0;False;0;False;0;-78.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TriplanarNode;82;-2366.25,-1213.147;Inherit;True;Cylindrical;World;False;Top Texture 4;_TopTexture4;white;-1;None;Mid Texture 2;_MidTexture2;white;-1;None;Bot Texture 2;_BotTexture2;white;-1;None;Triplanar Sampler;Tangent;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT3;1,1,1;False;3;FLOAT2;1,1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;144;-1957.919,172.6026;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TriplanarNode;1;-2303.055,-301.6066;Inherit;True;Cylindrical;World;False;Top Texture 0;_TopTexture0;white;-1;None;Mid Texture 0;_MidTexture0;white;-1;None;Bot Texture 0;_BotTexture0;white;-1;None;Triplanar Sampler;Tangent;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT3;1,1,1;False;3;FLOAT2;1,1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NormalVertexDataNode;119;-1513.802,-478.8179;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;145;-1798.919,238.6027;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;83;-1964.034,-1217.219;Inherit;False;TriplanarSnow;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;62;-1900.838,-305.6788;Inherit;False;Triplanar;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;178;-2079.967,1281.862;Inherit;False;89;Offset;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ObjectScaleNode;179;-2078.681,1134.371;Inherit;False;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.IntNode;180;-1914.361,1417.289;Inherit;False;Constant;_Int1;Int 1;6;0;Create;True;0;0;0;False;0;False;90;0;False;0;1;INT;0
Node;AmplifyShaderEditor.GetLocalVarNode;88;-1887.548,-77.96118;Inherit;False;83;TriplanarSnow;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.WorldPosInputsNode;87;-1506.14,94.94544;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureCoordinatesNode;182;-1850.468,1182.169;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RadiansOpNode;181;-1767.36,1421.289;Inherit;False;1;0;INT;0;False;1;INT;0
Node;AmplifyShaderEditor.DotProductOpNode;172;-1345.988,-504.0992;Inherit;False;2;0;FLOAT3;0,1,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;183;-1801.36,1300.289;Inherit;False;Constant;_Vector1;Vector 1;6;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ClampOpNode;142;-1795.826,117.2665;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;64;-1503.185,325.4644;Inherit;False;62;Triplanar;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SamplerNode;118;-1618.276,410.0666;Inherit;True;Property;_WaterCutoffTexture;WaterCutoffTexture;7;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;137;-1515.826,250.2665;Inherit;False;136;GrassCutoff;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;126;-1862.576,-5.460377;Inherit;False;62;Triplanar;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;69;-1300.228,24.0007;Inherit;False;Property;_GrassCutoff;GrassCutoff;16;0;Create;True;0;0;0;False;0;False;50;93.9;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;194;-1605.66,1036.824;Inherit;True;Property;_BottomTextureNormal;BottomTextureNormal;0;0;Create;True;0;0;0;False;0;False;None;None;True;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.ConditionalIfNode;85;-1302.051,229.6255;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT4;0,0,0,0;False;3;FLOAT;0;False;4;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;133;-1635.005,-40.6866;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.WorldPosInputsNode;184;-1803.836,1019.132;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldPosInputsNode;68;-1309.281,-127.9643;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;185;-1600.767,1404.158;Inherit;False;90;Falloff;1;0;OBJECT;;False;1;INT;0
Node;AmplifyShaderEditor.TexturePropertyNode;193;-1604.924,852.788;Inherit;True;Property;_MiddleTextureNormal;MiddleTextureNormal;2;0;Create;True;0;0;0;False;0;False;None;None;True;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleDivideOpNode;174;-1211.988,-504.0992;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.6;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;186;-1622.359,1281.289;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;187;-1606.013,659.2439;Inherit;True;Property;_TopTextureNormal;TopTextureNormal;1;0;Create;True;0;0;0;False;0;False;None;None;True;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SaturateNode;173;-1090.988,-504.0992;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;66;-1105.193,6.716045;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT4;0,0,0,0;False;3;FLOAT;0;False;4;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TriplanarNode;190;-1258.169,941.7788;Inherit;True;Cylindrical;World;False;Top Texture 1;_TopTexture1;white;-1;None;Mid Texture 1;_MidTexture1;white;-1;None;Bot Texture 1;_BotTexture1;white;-1;None;Triplanar Sampler;Tangent;10;0;SAMPLER2D;;False;5;FLOAT;1;False;1;SAMPLER2D;;False;6;FLOAT;0;False;2;SAMPLER2D;;False;7;FLOAT;0;False;9;FLOAT3;0,0,0;False;8;FLOAT3;1,1,1;False;3;FLOAT2;1,1;False;4;FLOAT;1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;121;-1093.138,-431.0331;Inherit;False;Property;_NormalCutoff;NormalCutoff;18;0;Create;True;0;0;0;False;0;False;45;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;43;-963.9196,-649.7499;Inherit;False;Property;_AntiTiling3;AntiTiling3;15;0;Create;True;0;0;0;False;0;False;0;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-963.9196,-799.75;Inherit;False;Property;_AntiTiling1;AntiTiling1;13;0;Create;True;0;0;0;False;0;False;0;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;124;-1264.35,-332.6672;Inherit;True;Property;_NormalCutoffTexture;NormalCutoffTexture;8;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;200;-1003.086,1135.416;Inherit;False;Constant;_Float0;Float 0;23;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;42;-963.9196,-726.7499;Inherit;False;Property;_AntiTiling2;AntiTiling2;14;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;198;-827.0894,965.9858;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;196;-836.7195,1136.385;Inherit;False;Property;_NormalStrength;NormalStrength;23;0;Create;True;0;0;0;False;0;False;2;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;171;-787.9883,-745.0975;Inherit;False;AntiTiling;11;;7;4d0fb13bc0196af499f2e1a9c93072f2;0;6;20;FLOAT2;0,0;False;21;FLOAT2;0,0;False;22;FLOAT2;0,0;False;17;FLOAT;0;False;18;FLOAT;0;False;19;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ConditionalIfNode;122;-805.7536,-471.3672;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT4;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;176;-553.528,-363.5634;Inherit;False;Property;_Smoothness;Smoothness;21;0;Create;True;0;0;0;False;0;False;0;-0.06;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;175;-528.528,-442.5634;Inherit;False;Property;_Metallic;Metallic;20;0;Create;True;0;0;0;False;0;False;0;0.18;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;-521.9342,-664.2288;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;177;-573.5345,-287.3405;Inherit;False;Property;_AmbientOcclusion;AmbientOcclusion;22;0;Create;True;0;0;0;False;0;False;0;1.46;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.UnpackScaleNormalNode;199;-623.2546,966.0139;Inherit;True;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-355.343,-660.3524;Float;False;True;-1;7;ASEMaterialInspector;0;0;Standard;TriplanarShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;89;0;71;0
WireConnection;17;0;36;0
WireConnection;17;1;95;0
WireConnection;34;0;33;0
WireConnection;91;0;13;0
WireConnection;74;0;70;0
WireConnection;136;0;86;0
WireConnection;92;0;14;0
WireConnection;90;0;76;0
WireConnection;75;0;72;0
WireConnection;75;1;93;0
WireConnection;80;0;75;0
WireConnection;80;1;73;0
WireConnection;80;2;74;0
WireConnection;31;0;17;0
WireConnection;31;1;32;0
WireConnection;31;2;34;0
WireConnection;82;0;78;0
WireConnection;82;1;101;0
WireConnection;82;2;102;0
WireConnection;82;9;79;0
WireConnection;82;3;80;0
WireConnection;82;4;94;0
WireConnection;144;0;138;0
WireConnection;144;1;134;2
WireConnection;1;0;12;0
WireConnection;1;1;99;0
WireConnection;1;2;100;0
WireConnection;1;9;16;0
WireConnection;1;3;31;0
WireConnection;1;4;96;0
WireConnection;145;0;146;0
WireConnection;145;1;144;0
WireConnection;83;0;82;0
WireConnection;62;0;1;0
WireConnection;182;0;179;0
WireConnection;182;1;178;0
WireConnection;181;0;180;0
WireConnection;172;1;119;0
WireConnection;142;0;145;0
WireConnection;85;0;87;2
WireConnection;85;1;137;0
WireConnection;85;2;64;0
WireConnection;85;4;118;0
WireConnection;133;0;88;0
WireConnection;133;1;126;0
WireConnection;133;2;142;0
WireConnection;174;0;172;0
WireConnection;186;0;182;0
WireConnection;186;1;183;0
WireConnection;186;2;181;0
WireConnection;173;0;174;0
WireConnection;66;0;68;2
WireConnection;66;1;69;0
WireConnection;66;2;133;0
WireConnection;66;4;85;0
WireConnection;190;0;187;0
WireConnection;190;1;193;0
WireConnection;190;2;194;0
WireConnection;190;9;184;0
WireConnection;190;3;186;0
WireConnection;190;4;185;0
WireConnection;198;0;200;0
WireConnection;198;1;190;2
WireConnection;198;3;190;1
WireConnection;171;17;41;0
WireConnection;171;18;42;0
WireConnection;171;19;43;0
WireConnection;122;0;173;0
WireConnection;122;1;121;0
WireConnection;122;2;124;0
WireConnection;122;3;124;0
WireConnection;122;4;66;0
WireConnection;39;0;171;0
WireConnection;39;1;122;0
WireConnection;199;0;198;0
WireConnection;199;1;196;0
WireConnection;0;0;39;0
WireConnection;0;1;199;0
WireConnection;0;3;175;0
WireConnection;0;4;176;0
WireConnection;0;5;177;0
ASEEND*/
//CHKSM=3F7AB702A2B940245C58972CB1D16C588961E0C2