shader_type spatial;
//render_mode specular_disabled;
render_mode unshaded, specular_disabled, depth_prepass_alpha, depth_draw_opaque, shadows_disabled, cull_disabled;


global uniform float time;
uniform sampler2D main_tex : source_color, hint_default_transparent;
uniform sampler2D secondary_tex : source_color, hint_default_transparent;
uniform float frequency;
uniform float strength;
uniform float offset;
uniform float secondary_offset;

uniform float displacementFlowSpeed : hint_range(0, 2);
uniform float displacementScale : hint_range(0, 2) = 1.0;
uniform sampler2D displacement : hint_default_black;
uniform vec3 colorfake : source_color;
uniform sampler2D screen_texture : hint_screen_texture;


void fragment()
{
	//float alpha = abs(sin(time * frequency));
	//vec2 uv = UV2 + texture(displacement, UV2 * displacementScale + vec2(time * -displacementFlowSpeed)).ra;
	//vec2 uv = UV;
	//uv.y = clamp(uv.y + offset + strength * alpha, 0.01, 1.0);
	//vec4 col = texture(main_tex, uv) * COLOR;
	vec4 col = vec4(1.0,1.0,1.0,1.0) ;
	
	//float secondary_alpha = abs(sin((time - secondary_offset) * frequency));
	//vec2 uv2 = UV;
	//uv2.y = uv2.y + offset + strength * (time * frequency);
	//vec4 secondary_col = texture(secondary_tex, uv2);
	//secondary_col.a *= secondary_col.a;
	//col.rgb *= secondary_col.r;
	//col.a *= secondary_col.r;
	
	
	col = clamp(col * COLOR, 0, 1);
	//if (col.a < 0.5){
		ALBEDO = col.rgb;
	//}
	//col.a -= 0.1;
	vec4 col2 = vec4(1.0,1.0,1.0,1.0) ;
	col2 = clamp(col * COLOR, 0, 1);
	ALPHA = col2.a * col.a * col.a * col.a;
	
	
	}
