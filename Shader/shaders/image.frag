void mainImage( out vec4 f, vec2 g )
{
	f = texelFetch(iChannel0, ivec2(g),0); // thanks to dave hoskins
}
