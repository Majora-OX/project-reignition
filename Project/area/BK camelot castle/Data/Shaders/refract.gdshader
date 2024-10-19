shader_type spatial;

uniform vec3 colorfake : source_color;
uniform sampler2D displacement;
//uniform float refraction_strength : hint_range(0.0, 8.0, 0.001) = 0.5;
uniform sampler2D screen_texture : hint_screen_texture;

uniform bool is_depth_based = false;

vec2 refract_uv(vec2 uv, float strength2, vec3 normal){
	float strength1 = strength2 * (is_depth_based ? pow(uv.y, 4.0) * 5.0 : 1.0);
	uv += strength1 * length(normal) - strength1 * 1.2;
	return uv;
}

void fragment() {
	vec3 nmap = texture(displacement, UV).rgb;
	ALBEDO = mix(colorfake, texture(screen_texture, refract_uv(SCREEN_UV, 0.148, nmap)).rgb, 1.0);
}