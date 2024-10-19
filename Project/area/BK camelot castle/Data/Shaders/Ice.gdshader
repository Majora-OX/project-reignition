shader_type spatial;

/**
Color to use for the shock.
*/
uniform vec3 shock_color : source_color = vec3(1.0, 0.0, 0.0);
/**
Initial amplitude of the shock. This will start at this amplitude and
gradually attenuate.
*/
uniform float amplitude = 30.0;
/**
How fast shold it move side to side, more frequency means it'll move more quickly
side to side.
*/
uniform float frequecy = 10.0;
uniform sampler2D maintex;


void fragment() {
	float exponent = mod(TIME, 3.0);
	vec3 normal_color = texture(maintex, UV).rgb;
	
	ALBEDO = (normal_color + shock_color).rgb;
}