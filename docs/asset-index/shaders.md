> **원본 위치:** `AssetDownloads/tmp_game/assets/shaders/` (gitignore — 바이너리는 추적하지 않는다).
> 이 문서는 목록만 저장소에 남긴 사본이다. 아래 링크는 그 원본을 가리킨다.

# 셰이더 (shaders/)

각 하위 폴더에 한국어 `README.md`(파일별 설명·대상 엔진)가 있고, 항목마다 `SOURCE.json`과 라이선스 원문이 있다.

## 엔진 표기
- **Unity Built-in**: 현재 저장소의 Unity 6 게임(내장 파이프라인)에 바로 가까운 것. `OnRenderImage` 이미지 이펙트 등.
- **Unity URP**: Shader Graph/Renderer Feature. 내장 파이프라인 프로젝트에서는 HLSL 로직만 참고.
- **Godot 4 (.gdshader)**: godotshaders.com 및 GitHub. Unity에서 쓰려면 HLSL로 옮긴다(로직은 거의 1:1).
- **GLSL/slang (libretro)**: RetroArch 형식. 알고리즘을 HLSL 풀스크린 패스로 옮긴다.

## 라이선스 요약
- 대부분 **MIT / CC0 / Public Domain / Unlicense**.
- **BSD-3-Clause**: `crt/Unity_CRTEffect`, `flashlight-darkness/VolumetricLights` — 허용적, 저작권 고지 유지.
- **BSL-1.0 (Boost)**: `crt/Cathode-Retro` — 허용적(바이너리 배포 시 고지 불필요), 요청 목록 외라 명시.
- **CC-BY 3.0**: `film-grain-vhs/libretro-permissive/.../film-grain.slang` — "Film Grain shader by Martins Upitis" 크레딧 필요.
- **GPL (참고 전용)**: `crt/gpl-reference/libretro/` — crt-easymode, crt-geom, zfast_crt, crt-pi. **게임 빌드에 넣지 말 것.**
- godotshaders.com 항목: 페이지에 명시된 라이선스(CC0 또는 MIT)만 가져왔고 GPL·"Shadertoy port" 표기 항목은 제외했다. 코드만 해당 라이선스이며 페이지의 스크린샷은 포함하지 않았다.

## crt/ — CRT / 스캔라인 / 인광

| 항목 | 엔진 | 라이선스 | 작성자 |
|---|---|---|---|
| [`Cathode-Retro`](https://github.com/DeadlyRedCube/Cathode-Retro) | HLSL (+GLSL 변환), 엔진 독립 C++ 호스트 | BSL-1.0 | Josh Jones (DeadlyRedCube) |
| [`Flowerwall-CRT-shader-for-Godot`](https://github.com/art-oria/Flowerwall-CRT-shader-for-Godot) | Godot 4 (.gdshader + 애드온 스크립트) | MIT | Art (art-oria) |
| [`Godot-3-2D-CRT-Shader`](https://github.com/hiulit/Godot-3-2D-CRT-Shader) | Godot 3 (canvas_item .shader, Godot 4는 소폭 수정 필요) | MIT | Xavier Gomez Gosalbez (hiulit) |
| [`Simple-CRT-Shader`](https://github.com/yunoda-3DCG/Simple-CRT-Shader) | Unity Built-in (image effect .shader + C#) | MIT | yunoda |
| [`SimpleGodotCRTShader`](https://github.com/henriquelalves/SimpleGodotCRTShader) | Godot 4 (.gdshader, addon) | MIT | Henrique Lacreta Alves |
| [`URP_RetroCRTShader`](https://github.com/Cyanilux/URP_RetroCRTShader) | Unity URP (Shader Graph) | MIT | Cyanilux |
| [`Unity_CRTEffect`](https://github.com/XJINE/Unity_CRTEffect) | Unity Built-in (image effect .shader/.cginc + C#) | BSD-3-Clause | XJINE |
| [`godot-crt-lottes-shader`](https://github.com/qarlosh/godot-crt-lottes-shader) | Godot 3/4 (.shader/.gdshader) | Unlicense | qarlosh (port of Timothy Lottes crt-lottes, public domain) |
| [`godotshaders-com/4-3-scanline`](https://godotshaders.com/shader/4-3-scanline/) | Godot 4 (canvas_item) | CC0-1.0 | OK |
| [`godotshaders-com/animal-well-inspired-crt-effect`](https://godotshaders.com/shader/animal-well-inspired-crt-effect/) | Godot 4 (canvas_item) | CC0-1.0 | nitricswight |
| [`godotshaders-com/crt-display-shader-pixel-mask-scanlines-glow-godot-4-4-1`](https://godotshaders.com/shader/crt-display-shader-pixel-mask-scanlines-glow-godot-4-4-1/) | Godot 4 (spatial) | CC0-1.0 | paitorocxon |
| [`godotshaders-com/crt-video-like-shader`](https://godotshaders.com/shader/crt-video-like-shader/) | Godot 4 (canvas_item) | MIT | tqef |
| [`godotshaders-com/crt-with-luminance-preservation-no-scanlines`](https://godotshaders.com/shader/crt-with-luminance-preservation-no-scanlines/) | Godot 4 (canvas_item) | CC0-1.0 | Harrison Allen |
| [`godotshaders-com/semi-realistic-crt-emulation`](https://godotshaders.com/shader/semi-realistic-crt-emulation/) | Godot 4 (canvas_item) | CC0-1.0 | ekkochambers |
| [`godotshaders-com/simple-crt-bulge`](https://godotshaders.com/shader/simple-crt-bulge/) | Godot 4 (canvas_item) | CC0-1.0 | blitpxl |
| [`godotshaders-com/test-crt-vcr`](https://godotshaders.com/shader/test-crt-vcr/) | Godot 4 (canvas_item) | CC0-1.0 | banwarfarms |
| [`godotshaders-com/vhs-scanline-color-fuzz`](https://godotshaders.com/shader/vhs-scanline-color-fuzz/) | Godot 4 (canvas_item) | CC0-1.0 | davi_danger |
| [`gpl-reference/libretro`](https://github.com/libretro/glsl-shaders ; https://github.com/libretro/slang-shaders) | GLSL/slang | GPL-2.0-or-later (crt-geom, zfast_crt, crt-pi) / GPL (crt-easymode, version unspecified) | cgwg/Themaister/DOLLS (crt-geom), SoltanGris42 (zfast_crt), davej (crt-pi), EasyMode (crt-easymode) |
| [`libretro-glsl-permissive`](https://github.com/libretro/glsl-shaders) | GLSL (RetroArch .glsl/.glslp 형식, 엔진 이식 필요) | Mixed per file: Public Domain / CC0 / MIT | Various (see per-file headers): Timothy Lottes, Hyllian, J. Kyle Pittman (crtsim), torridgristle, Martins Upitis, etc. |
| [`libretro-slang-permissive`](https://github.com/libretro/slang-shaders) | Vulkan GLSL (RetroArch .slang/.slangp 형식) | Mixed per file: Public Domain / CC0 / MIT | Various (see per-file headers): Timothy Lottes, Hyllian, J. Kyle Pittman (crtsim), torridgristle, Martins Upitis, etc. |

## water-refraction/ — 수중 커스틱·굴절

| 항목 | 엔진 | 라이선스 | 작성자 |
|---|---|---|---|
| [`InteractiveStylizedWater`](https://github.com/mozankatip/InteractiveStylizedWater) | Unity URP (Shader Graph) | MIT | Mert Ozan Katipoglu |
| [`Stylized-Water-Shader`](https://github.com/Malidos/Stylized-Water-Shader) | Godot 4 (.gdshader) | CC0-1.0 | Malidos |
| [`URP-WaterShaders`](https://github.com/aniruddhahar/URP-WaterShaders) | Unity URP (Shader Graph) | MIT | aniruddhahar |
| [`URPUnderwaterEffects`](https://github.com/End3r6/URPUnderwaterEffects) | Unity URP (Renderer Feature + HLSL .shader) | MIT | End3r6 |
| [`godotshaders-com/2d-water-distortion-effect-godot-4`](https://godotshaders.com/shader/2d-water-distortion-effect-godot-4/) | Godot 4 (canvas_item) | CC0-1.0 | zeludtnecniv |
| [`godotshaders-com/3d-low-distortion-refraction-low-poly-glass`](https://godotshaders.com/shader/3d-low-distortion-refraction-low-poly-glass/) | Godot 4 (spatial) | CC0-1.0 | KnightNine |
| [`godotshaders-com/absorption-based-stylized-water`](https://godotshaders.com/shader/absorption-based-stylized-water/) | Godot 4 (spatial) | CC0-1.0 | Malido |
| [`godotshaders-com/chaep-caustics-unshade`](https://godotshaders.com/shader/chaep-caustics-unshade/) | Godot 4 (spatial) | CC0-1.0 | WaltGD |
| [`godotshaders-com/realistic-animated-shader-with-foamdropletscausticswaves`](https://godotshaders.com/shader/realistic-animated-shader-with-foamdropletscausticswaves/) | Godot 4 (spatial) | CC0-1.0 | lonely_studios |
| [`godotshaders-com/realistic-water-with-traced-and-simple-reflection-and-refraction-v2`](https://godotshaders.com/shader/realistic-water-with-traced-and-simple-reflection-and-refraction-v2/) | Godot 4 (spatial) | CC0-1.0 | smallcableboi |
| [`godotshaders-com/screen-space-refraction-shader`](https://godotshaders.com/shader/screen-space-refraction-shader/) | Godot 4 (spatial) | CC0-1.0 | mujtaba-io |
| [`godotshaders-com/sine-wave-camera-view-shader`](https://godotshaders.com/shader/sine-wave-camera-view-shader/) | Godot 4 (canvas_item) | CC0-1.0 | mujtaba-io |
| [`godotshaders-com/snells-window`](https://godotshaders.com/shader/snells-window/) | Godot 4 (spatial) | CC0-1.0 | tentabrobpy |
| [`godotshaders-com/transparent-water-shader-supporting-ssr`](https://godotshaders.com/shader/transparent-water-shader-supporting-ssr/) | Godot 4 (spatial) | MIT | marcelb |
| [`godotshaders-com/underwater-camera-effect`](https://godotshaders.com/shader/underwater-camera-effect/) | Godot 4 (spatial) | CC0-1.0 | Hylocereus |
| [`godotshaders-com/water-shader-3d-godot-4-3`](https://godotshaders.com/shader/water-shader-3d-godot-4-3/) | Godot 4 (spatial) | CC0-1.0 | Lesus |
| [`godotshaders-com/water-with-caustics`](https://godotshaders.com/shader/water-with-caustics/) | Godot 4 (spatial) | CC0-1.0 | binbun |
| [`unity-water-shader2d`](https://github.com/real-marco-b/unity-water-shader2d) | Unity Built-in (2D .shader) | MIT | real-marco-b |

## reflection/ — 창문·평면 반사

| 항목 | 엔진 | 라이선스 | 작성자 |
|---|---|---|---|
| [`AdamPlaneReflection`](https://github.com/keijiro/AdamPlaneReflection) | Unity Built-in (.shader + C#) | MIT | Unity Technologies / Keijiro Takahashi |
| [`Godot-SSPR`](https://github.com/Estecsky/Godot-SSPR) | Godot 4 (.gdshader + compute .glsl) | MIT | AQ_Echoo (Estecsky) |
| [`UnityURP-MobileScreenSpacePlanarReflection`](https://github.com/ColinLeung-NiloCat/UnityURP-MobileScreenSpacePlanarReflection) | Unity URP (Renderer Feature, compute + HLSL) | MIT | ColinLeung-NiloCat |
| [`godotshaders-com/2d-mirror-effect`](https://godotshaders.com/shader/2d-mirror-effect/) | Godot 4 (canvas_item) | CC0-1.0 | pend00 |
| [`godotshaders-com/procedural-window-rain-drop-shader`](https://godotshaders.com/shader/procedural-window-rain-drop-shader/) | Godot 4 (spatial) | MIT | arlez80 |
| [`godotshaders-com/rain-on-glass`](https://godotshaders.com/shader/rain-on-glass/) | Godot 4 (canvas_item) | MIT | Gerardo LCDF |
| [`godotshaders-com/rain-puddles-with-screen-space-reflections`](https://godotshaders.com/shader/rain-puddles-with-screen-space-reflections/) | Godot 4 (spatial) | CC0-1.0 | shadecore_dev |
| [`godotshaders-com/realistic-glass-with-traced-and-simple-reflection-and-refraction`](https://godotshaders.com/shader/realistic-glass-with-traced-and-simple-reflection-and-refraction/) | Godot 4 (spatial) | CC0-1.0 | smallcableboi |
| [`godotshaders-com/simple-zoom-reflections-mirrors`](https://godotshaders.com/shader/simple-zoom-reflections-mirrors/) | Godot 4 (canvas_item) | CC0-1.0 | Maaack |
| [`kMirrors`](https://github.com/Kink3d/kMirrors) | Unity URP (C# + .shader) | MIT | Matt Dean (Kink3d) |
| [`planar-reflections-unity`](https://github.com/eldskald/planar-reflections-unity) | Unity Built-in + URP (.cginc/.shader + C#) | MIT | Rafael Bordoni (eldskald) |

## film-grain-vhs/ — 필름 그레인·VHS·색수차·비네트·디더

| 항목 | 엔진 | 라이선스 | 작성자 |
|---|---|---|---|
| [`CrowFX-Unity-Image-Effects`](https://github.com/Luci0n/CrowFX-Unity-Image-Effects) | Unity (Built-in/URP 이미지 이펙트 .shader + C#) | MIT | Luci0n |
| [`Godot-Hi-8-Demo`](https://github.com/cyanideoneup/Godot-Hi-8-Demo) | Godot 4 (.gdshader) | MIT | cyanideoneup |
| [`KinoBloom`](https://github.com/keijiro/KinoBloom) | Unity Built-in (image effect) | MIT | Keijiro Takahashi |
| [`KinoFringe`](https://github.com/keijiro/KinoFringe) | Unity Built-in (image effect) | MIT | Keijiro Takahashi |
| [`KinoGlitch`](https://github.com/keijiro/KinoGlitch) | Unity Built-in (image effect) | MIT | Keijiro Takahashi |
| [`VHS-Effect`](https://github.com/bandinopla/VHS-Effect) | Unity URP (Shader Graph) | MIT | bandinopla (after Ben Cloward) |
| [`godotshaders-com/aberration-phasmophobia-effect`](https://godotshaders.com/shader/aberration-phasmophobia-effect/) | Godot 4 (canvas_item) | CC0-1.0 | Grau |
| [`godotshaders-com/adjustable-chromatic-aberration`](https://godotshaders.com/shader/adjustable-chromatic-aberration/) | Godot 4 (canvas_item) | MIT | alfroids |
| [`godotshaders-com/advanced-side-vignette`](https://godotshaders.com/shader/advanced-side-vignette/) | Godot 4 (canvas_item) | CC0-1.0 | herrmarx |
| [`godotshaders-com/bit-depth-posterize-post-process-with-optional-dithering`](https://godotshaders.com/shader/bit-depth-posterize-post-process-with-optional-dithering/) | Godot 4 (canvas_item) | CC0-1.0 | miwls |
| [`godotshaders-com/camcorder-horror-shader`](https://godotshaders.com/shader/camcorder-horror-shader/) | Godot 4 (canvas_item) | CC0-1.0 | How2Bboss |
| [`godotshaders-com/camera-vignette-shader`](https://godotshaders.com/shader/camera-vignette-shader/) | Godot 4 (canvas_item) | CC0-1.0 | MDoubleDee |
| [`godotshaders-com/chromatic-aberration-for-3d-post-processing`](https://godotshaders.com/shader/chromatic-aberration-for-3d-post-processing/) | Godot 4 (spatial) | CC0-1.0 | Castro1709 |
| [`godotshaders-com/chromatic-aberration-vignette`](https://godotshaders.com/shader/chromatic-aberration-vignette/) | Godot 4 (canvas_item) | CC0-1.0 | Ructoon |
| [`godotshaders-com/classic-dithering-shader`](https://godotshaders.com/shader/classic-dithering-shader/) | Godot 4 (canvas_item) | CC0-1.0 | zessbin |
| [`godotshaders-com/colour-correction-grading`](https://godotshaders.com/shader/colour-correction-grading/) | Godot 4 (canvas_item) | CC0-1.0 | lil_sue |
| [`godotshaders-com/crt-vhs-simple`](https://godotshaders.com/shader/crt-vhs-simple/) | Godot 4 (canvas_item) | CC0-1.0 | eliprogramer178 |
| [`godotshaders-com/darkness-weighted-film-grain-effect`](https://godotshaders.com/shader/darkness-weighted-film-grain-effect/) | Godot 4 (canvas_item) | CC0-1.0 | Joshulties |
| [`godotshaders-com/film-grain-shader`](https://godotshaders.com/shader/film-grain-shader/) | Godot 4 (canvas_item) | CC0-1.0 | mujtaba-io |
| [`godotshaders-com/pixelated-horror-vignette-dot-matrix-downres`](https://godotshaders.com/shader/pixelated-horror-vignette-dot-matrix-downres/) | Godot 4 (spatial) | CC0-1.0 | jorbyte |
| [`godotshaders-com/radial-chromatic-aberration`](https://godotshaders.com/shader/radial-chromatic-aberration/) | Godot 4 (canvas_item) | CC0-1.0 | gorroto |
| [`godotshaders-com/realistic-photography-camera`](https://godotshaders.com/shader/realistic-photography-camera/) | Godot 4 (canvas_item) | CC0-1.0 | TuniTem |
| [`godotshaders-com/retro-luma-color-reduction-quantization-posterize-dithering`](https://godotshaders.com/shader/retro-luma-color-reduction-quantization-posterize-dithering/) | Godot 4 (canvas_item) | CC0-1.0 | ProfesorShader |
| [`godotshaders-com/vhs-crt-broadcast`](https://godotshaders.com/shader/vhs-crt-broadcast/) | Godot 4 (canvas_item) | CC0-1.0 | banwarfarms |
| [`godotshaders-com/vhs-scanline-glitch`](https://godotshaders.com/shader/vhs-scanline-glitch/) | Godot 4 (canvas_item) | CC0-1.0 | hailyn |
| [`godotshaders-com/vhs-shader`](https://godotshaders.com/shader/vhs-shader/) | Godot 4 (canvas_item) | CC0-1.0 | Vacation |
| [`godotshaders-com/vhs-tape-effect`](https://godotshaders.com/shader/vhs-tape-effect/) | Godot 4 (canvas_item) | CC0-1.0 | blblblblb |
| [`godotshaders-com/vignette`](https://godotshaders.com/shader/vignette/) | Godot 4 (canvas_item) | CC0-1.0 | crocoby |
| [`libretro-permissive`](https://github.com/libretro/slang-shaders ; https://github.com/libretro/glsl-shaders) | slang/GLSL (RetroArch 형식) | Mixed per file: CC0-1.0 / Public Domain / MIT / CC-BY-3.0 (film-grain.slang) | Various: hunterk, Hyllian, Martins Upitis, godotshaders.com contributor (VHS & CRT monitor effect), etc. |

## flashlight-darkness/ — 손전등·어둠·볼류메트릭

| 항목 | 엔진 | 라이선스 | 작성자 |
|---|---|---|---|
| [`2D-Volumetric-Lighting`](https://github.com/aszecsei/2D-Volumetric-Lighting) | Unity 2D (.shader + C#) | CC0-1.0 | aszecsei |
| [`Unity-URP-Volumetric-Light`](https://github.com/CristianQiu/Unity-URP-Volumetric-Light) | Unity URP (Unity 2022.3/6, HLSL + Renderer Feature) | MIT | Cristian Qiu |
| [`VolumetricLights`](https://github.com/SlightlyMad/VolumetricLights) | Unity Built-in (.shader + C#) | BSD-3-Clause | Michal Skalsky |
| [`godotshaders-com/2d-retro-dithered-lighting-fog-of-war`](https://godotshaders.com/shader/2d-retro-dithered-lighting-fog-of-war/) | Godot 4 (canvas_item) | MIT | nachomakesgames |
| [`godotshaders-com/2d-sdf-lighting-shader-without-shadows-or-light-occlusion`](https://godotshaders.com/shader/2d-sdf-lighting-shader-without-shadows-or-light-occlusion/) | Godot 4 (canvas_item) | CC0-1.0 | electrick |
| [`godotshaders-com/additive-volume-integral`](https://godotshaders.com/shader/additive-volume-integral/) | Godot 4 (spatial) | CC0-1.0 | tentabrobpy |
| [`godotshaders-com/fake-godrays-godot-4-2`](https://godotshaders.com/shader/fake-godrays-godot-4-2/) | Godot 4 (spatial) | CC0-1.0 | Ivan Isaev 643 |
| [`godotshaders-com/field-of-view-circular-cone-rectangle-mask-shader`](https://godotshaders.com/shader/field-of-view-circular-cone-rectangle-mask-shader/) | Godot 4 (canvas_item) | CC0-1.0 | JInDaifer |
| [`godotshaders-com/god-rays`](https://godotshaders.com/shader/god-rays/) | Godot 4 (canvas_item) | CC0-1.0 | pend00 |
| [`godotshaders-com/scary-dark-vignette`](https://godotshaders.com/shader/scary-dark-vignette/) | Godot 4 (canvas_item) | CC0-1.0 | Silver637 |
| [`godotshaders-com/screen-space-god-rays-godot-4-3`](https://godotshaders.com/shader/screen-space-god-rays-godot-4-3/) | Godot 4 (canvas_item) | CC0-1.0 | Qtan1 |
| [`godotshaders-com/shooting-cone`](https://godotshaders.com/shader/shooting-cone/) | Godot 4 (canvas_item) | MIT | GOSIjnr |
| [`godotshaders-com/spatial-light-shaft`](https://godotshaders.com/shader/spatial-light-shaft/) | Godot 4 (spatial) | CC0-1.0 | absentSpaghetti |
| [`godotshaders-com/visionconeenergy`](https://godotshaders.com/shader/visionconeenergy/) | Godot 4 (spatial) | CC0-1.0 | Anvoltrix Games |

## ice-crack/ — 얼음·서리·균열

| 항목 | 엔진 | 라이선스 | 작성자 |
|---|---|---|---|
| [`Godot-Glass-Break-Effect`](https://github.com/Lord0Sanz/Godot-Glass-Break-Effect) | Godot 4 (.gdshader) | MIT | Lord0Sanz |
| [`Unity-URP-ShaderGraph-Ice-Shader`](https://github.com/brogli/Unity-URP-ShaderGraph-Ice-Shader) | Unity URP (Shader Graph) | MIT | brogli |
| [`cracked-ice`](https://github.com/danielpokladek-shaders/cracked-ice) | Unity URP (HLSL .shader + Shader Graph) | MIT | Shader Vault (Daniel Pokladek) |
| [`godotshaders-com/frostbite`](https://godotshaders.com/shader/frostbite/) | Godot 4 (canvas_item) | CC0-1.0 | TTien63 |
| [`godotshaders-com/frosted-glass-fast`](https://godotshaders.com/shader/frosted-glass-fast/) | Godot 4 (spatial) | CC0-1.0 | binbun |
| [`godotshaders-com/frosted-glass-gaussian-blur`](https://godotshaders.com/shader/frosted-glass-gaussian-blur/) | Godot 4 (spatial) | CC0-1.0 | binbun |
| [`godotshaders-com/ice-covering`](https://godotshaders.com/shader/ice-covering/) | Godot 4 (canvas_item) | MIT | fritzy |
| [`godotshaders-com/impact-glass-shader`](https://godotshaders.com/shader/impact-glass-shader/) | Godot 4 (canvas_item) | MIT | Lord0Sanz |
| [`godotshaders-com/improved-frosted-glass`](https://godotshaders.com/shader/improved-frosted-glass/) | Godot 4 (spatial) | CC0-1.0 | sfammonius |
| [`godotshaders-com/screen-space-frost-with-volumetric-snow`](https://godotshaders.com/shader/screen-space-frost-with-volumetric-snow/) | Godot 4 (spatial) | MIT | xtarsia |
| [`phase-transition`](https://github.com/robert-leitl/phase-transition) | WebGL (GLSL + JS) | MIT | Robert Leitl |
| [`shaders-ice`](https://github.com/daniel-ilett/shaders-ice) | Unity URP (Shader Graph) | MIT | Daniel Ilett |
| [`unity-frosted-glass`](https://github.com/andydbc/unity-frosted-glass) | Unity Built-in (.shader + C# CommandBuffer) | MIT | Andy Duboc |

## 받지 못했거나 뺀 것
- **crt-royale**(GPL, 대용량 다중 패스): gpl-reference에도 넣지 않았다. 필요하면 libretro/slang-shaders에서 직접 참고.
- libretro에서 라이선스 헤더가 없는 셰이더(fakelottes, crt-consumer, crt-yah, cathode-retro libretro 포트 등)는 권리 불명이라 제외. Cathode-Retro는 원 저장소(BSL-1.0)에서 따로 받았다.
- `film_noise1.png`(libretro film 노이즈 텍스처): 출처 불명이라 제외 — 노이즈 텍스처는 직접 생성.
- Shadertoy 코드(기본 CC BY-NC-SA, 상업 불가)는 전부 제외.
- Unity 저장소에 딸린 서드파티 폴더(Standard Assets, TextMesh Pro, PostProcessing v1)는 저자 소유가 아니라 제외. Unity 씬·모델·ProjectSettings도 제외하고 셰이더·스크립트·작은 텍스처(≤2MB)·README·LICENSE만 복사.
- Zlib 라이선스 `armory-caustics-volume`은 Armory3D(Blender) 전용이라 제외.

총 105개 항목, 용량 46M.
