<?xml version="1.0" encoding="UTF-8"?>
<!--
     NTSC shader v2
     Author: Themaister
     License: GPLv3
-->
<shader language="GLSL" style="GLES2">

   <!-- 1st pass: Scale and modulate YIQ -->
   <vertex><![CDATA[
      #version 120
      uniform mat4 rubyMVPMatrix;
      attribute vec2 rubyVertexCoord;
      attribute vec2 rubyTexCoord;
      varying vec2 tex_coord;

      varying vec2 pix_no;
      uniform vec2 rubyTextureSize;
      uniform vec2 rubyInputSize;
      uniform vec2 rubyOutputSize;

      void main()
      {
         gl_Position = rubyMVPMatrix * vec4(rubyVertexCoord, 0.0, 1.0);
         tex_coord = rubyTexCoord;
         pix_no = rubyTexCoord * rubyTextureSize * (rubyOutputSize / rubyInputSize);
      }
   ]]></vertex>
   <fragment filter="nearest" scale_x="4.0" scale_y="1.0" frame_count_mod="2" float_framebuffer="true"><![CDATA[
      #version 120
      varying vec2 tex_coord;
      uniform sampler2D rubyTexture;
      uniform int rubyFrameCount;
      varying vec2 pix_no;

#define PI 3.14159265
#define CHROMA_MOD_FREQ (0.4 * PI)
#define CHROMA_AMP 1.0
#define ENCODE_GAMMA (1.0 / 2.2)

      const mat3 yiq_mat = mat3(
         0.2989, 0.5959, 0.2115,
         0.5870, -0.2744, -0.5229,
         0.1140, -0.3216, 0.3114);

      vec3 rgb2yiq(vec3 col)
      {
         return yiq_mat * col;
      }

      void main()
      {
         vec3 col = texture2D(rubyTexture, tex_coord).rgb;
         vec3 yiq = rgb2yiq(pow(col, vec3(ENCODE_GAMMA)));

         float chroma_phase = PI * 0.6667 * (mod(pix_no.y, 3.0) + float(rubyFrameCount));
         float mod_phase = chroma_phase + pix_no.x * CHROMA_MOD_FREQ;

         float i_mod = CHROMA_AMP * cos(mod_phase);
         float q_mod = CHROMA_AMP * sin(mod_phase);

         yiq = vec3(yiq.x, yiq.y * i_mod, yiq.z * q_mod);
         gl_FragColor = vec4(yiq, 1.0);
      }
   ]]></fragment>

   <!-- 2nd pass - Create composite signal,
        low-pass and demodulate separately -->
   <vertex><![CDATA[
      #version 120
      uniform mat4 rubyMVPMatrix;
      attribute vec2 rubyVertexCoord;
      attribute vec2 rubyTexCoord;
      uniform vec2 rubyTextureSize;
      uniform vec2 rubyOutputSize;

      varying vec2 tex_coord;

      varying vec2 pix_no;

      void main()
      {
         gl_Position = rubyMVPMatrix * vec4(rubyVertexCoord, 0.0, 1.0);
         tex_coord = rubyTexCoord;
         pix_no = rubyTexCoord * rubyTextureSize;
      }
   ]]></vertex>
   <fragment filter="nearest" scale="1.0" frame_count_mod="2" float_framebuffer="true"><![CDATA[
      #version 120
      uniform sampler2D rubyTexture;
      uniform vec2 rubyTextureSize;
      uniform int rubyFrameCount;
      varying vec2 tex_coord;

      varying vec2 pix_no;

#define PI 3.14159265
#define CHROMA_MOD_FREQ (0.4 * PI)

#define CHROMA_AMP 1.0
#define SATURATION 1.0
#define BRIGHTNESS 1.0
#define chroma_mod (2.0 * SATURATION / CHROMA_AMP)

      const float filter[9] = float[9](
         0.0019, 0.0031, -0.0108, 0.0, 0.0407,
         -0.0445, -0.0807, 0.2913, 0.5982
      );

      vec3 fetch_offset(float offset, float one_x)
      {
         return texture2D(rubyTexture, tex_coord + vec2(offset * one_x, 0.0)).xyz;
      }

      void main()
      {
         float one_x = 1.0 / rubyTextureSize.x;
         float chroma_phase = PI * 0.6667 * (mod(pix_no.y, 3.0) + float(rubyFrameCount));
         float mod_phase = chroma_phase + pix_no.x * CHROMA_MOD_FREQ;

         float signal = 0.0;
         for (int i = 0; i < 8; i++)
         {
            float offset = float(i);
            float sums =
               dot(fetch_offset(offset - 8.0, one_x), vec3(1.0)) +
               dot(fetch_offset(8.0 - offset, one_x), vec3(1.0));

            signal += sums * filter[i];
         }
         signal += dot(texture2D(rubyTexture, tex_coord).xyz, vec3(1.0)) * filter[8];

         float i_mod = chroma_mod * cos(mod_phase);
         float q_mod = chroma_mod * sin(mod_phase);

         vec3 out_color = vec3(signal) * vec3(BRIGHTNESS, i_mod, q_mod);
         gl_FragColor = vec4(out_color, 1.0);
      }
   ]]></fragment>

   <!-- 3rd pass - Low-pass luma and chroma, decode to RGB -->
   <vertex><![CDATA[
      #version 120
      uniform mat4 rubyMVPMatrix;
      attribute vec2 rubyVertexCoord;
      attribute vec2 rubyTexCoord;
      varying vec2 tex_coord;

      void main()
      {
         gl_Position = rubyMVPMatrix * vec4(rubyVertexCoord, 0.0, 1.0);
         tex_coord = rubyTexCoord;
      }
   ]]></vertex>
   <fragment scale="1.0" filter="nearest"><![CDATA[
      #version 120
      varying vec2 tex_coord;
      uniform sampler2D rubyTexture;
      uniform vec2 rubyTextureSize;

#define NTSC_GAMMA 2.2

      const float luma_filter[9] = float[9](
         -0.0020, -0.0009, 0.0038, 0.0178, 0.0445,
         0.0817, 0.1214, 0.1519, 0.1634
      );

      const float chroma_filter[9] = float[9](
         0.0046, 0.0082, 0.0182, 0.0353, 0.0501,
         0.0832, 0.1062, 0.1222, 0.1280
      );

      const mat3 yiq2rgb_mat = mat3(
         1.0, 1.0, 1.0,
         0.956, -0.2720, -1.1060,
         0.6210, -0.6474, 1.7046);

      vec3 yiq2rgb(vec3 yiq)
      {
         return yiq2rgb_mat * yiq;
      }

      vec3 fetch_offset(float offset, float one_x)
      {
         return texture2D(rubyTexture, tex_coord + vec2(offset * one_x, 0.0)).xyz;
      }

      void main()
      {
         float one_x = 1.0 / rubyTextureSize.x;
         vec3 signal = vec3(0.0);
         for (int i = 0; i < 8; i++)
         {
            float offset = float(i);

            vec3 sums = fetch_offset(offset - 8.0, one_x) +
               fetch_offset(8.0 - offset, one_x);

            signal += sums * vec3(luma_filter[i], chroma_filter[i], chroma_filter[i]);
         }
         signal += texture2D(rubyTexture, tex_coord).xyz *
            vec3(luma_filter[8], chroma_filter[8], chroma_filter[8]);

         vec3 rgb = pow(yiq2rgb(signal), vec3(NTSC_GAMMA));
         gl_FragColor = vec4(rgb, 1.0);
      }
   ]]></fragment>
</shader>
