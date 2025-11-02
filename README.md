# TrueTrace: A High Performance Compute Shader based Unity Pathtracer

![](/.Images/Yanus1.png)
### A passion projects that has been going on for a while with the goal of bringing at least interactive path-tracing to everyone in unity, regardless of their hardware
# Discord Server: https://discord.gg/4Yh7AZuhcD
## Demo:  https://drive.google.com/file/d/1sb_zRycX23AlC3QQ9LfqrtEZzBj47Z-Y/view?usp=sharing



## Features: 
<ul>
  <li>High Performance Compute Shader based path tracing without RT cores</li>
  <li>Compressed Wide Bounding Volume Hierarchy for High Performance Software RT(no RT Cores)</li>
  <li>No specific GPU vendor needed(this will run on integrated graphics if you so wish it, aka no RTX cores needed)</li>
  <li>Hardware RT Support for modern cards</li>
  <li>Full Disney BSDF for materials</li>
  <li>Full support for complex emissive meshes</li>
  <li>Runtime transform, add, and removal of objects</li>
  <li>Runtime modifiable materials</li>
  <li>ASVGF for realtime denoising</li>
  <li>OIDN for offline denoising</li>
  <li>PBR Texture Support</li>
  <li>Next Event Estimation with Multiple Importance Sampling</li>
  <li>Spherical Gaussian Light Tree or the Light BVH for Next Event Estimation</li>
  <li>Support for all default unity lights(Using Next Event Estimation)</li>
  <li>Bloom, Depth of Field, AutoExposure, TAA, Tonemapping</li>
  <li>Precomputed Multiple Atmospheric Scattering for the sky</li>
  <li>Object Instancing(Without RT Cores)</li>
  <li>ReSTIR GI(cursed implementation)</li>
  <li>Simple upscaling ability</li>
  <li>Supports Built-in, HDRP, and URP</li>
  <li>Full skinned mesh support</li>
  <li>Supports deformable standard meshes</li>
  <li>Supports unity heightmap terrain and heightmap trees</li>
  <li>Enironment Map Importance Sampling</li>
  <li>Radiance Cache</li>
  <li>Material Preset System</li>
  <li>Efficient Panorama Rendering</li>
  <li>IES for spotlights</li>
  <li>True Bindless texturing(Thanks to Meetem)</li>
  <li>Convolution Bloom(Not mine)</li>
  <li>Vulkan and Metal support(Your mileage may vary)</li>
  <li>Mesh slicing using SDFs(Does not modify meshes, for rendering cuts only, like cross-sections)</li>
  <li>Lambert or EON diffuse models</li>
  <li>Chromatic Aberation, Saturation, Colored Vignette</li>
  <li>Full Multiscatter Fog(Not realtime)</li>
  <li>Orthographic Camera Support</li>
  <li>Photon Mapping for fast(but not realtime) caustics</li>
  <li>Animatable Material Properties</li> 
</ul>

MASSIVE thanks to 
[Alex Bakanov(AKA Meetem)](https://github.com/Meetem)
for bringing bindless textures to unity!
</br>[Ylitie et al](https://research.nvidia.com/sites/default/files/publications/ylitie2017hpg-paper.pdf)
</br>[ebruneton](https://ebruneton.github.io/precomputed_atmospheric_scattering/)
</br>[Convolutional Bloom](https://github.com/AKGWSB/FFTConvolutionBloom)
</br>[Spherical Gaussian Light Tree](https://gpuopen.com/download/publications/Hierarchical_Light_Sampling_with_Accurate_Spherical_Gaussian_Lighting.pdf)
</br>[Light BVH(PBRT 4)](https://pbr-book.org/4ed/Light_Sources/Light_Sampling#x3-LightBoundingVolumeHierarchies)
</br>[vMF Diffuse Model](https://research.nvidia.com/publication/2024-07_vmf-diffuse-unified-rough-diffuse-brdf)
</br>[EON Diffuse Model](https://arxiv.org/pdf/2410.18026)
</br>[URP Compatability script inspiration](https://github.com/Andyfanshen/CustomRayTracing/tree/RenderGraph-(URP-23.3-beta%2B))
</br>[Chromatic Aberation](https://www.shadertoy.com/view/wsdBWM)
</br>[Contrast/Saturation](https://www.shadertoy.com/view/XdcXzn)
</br>[Vignette Base](https://www.shadertoy.com/view/tt2cDK)
</br>[Vignette Color](https://www.shadertoy.com/view/4lVGWw)

### If you like what I do and want to support me or this project, Please consider becoming a Github Sponsor or a Patron at patreon.com/Pjbomb2!  This allows me to keep this free for everyone!
### You can contact me easiest through my discord server(above) or my [twitter](https://x.com/Pjbomb2) with bugs, ideas, or thoughts on the project!


## Adding new objects
### Automatic methods
<ul>
  <li>First, add your gameobjects as a child of the "Scene" gameobject created by TrueTrace.</li>
  <li>Global setup: Press "Auto Assign Scripts" in the TrueTrace settings menu.</li>
  <li>Local setup: Go to "Hierarchy Options" in the TrueTrace settings menu. </li>
  <li>Drag the root gameobject that you added to the "Selective Auto Assign Scripts" section.</li>
  <li>Click "Selective Auto Assign".</li>
</ul>
</br>

### Manual method
<ul>
  <li>First, each gameobject that contains a mesh needs the script "RayTracingObject" added to it.(This keeps track of per-mesh materials)</li>
  <li>For non-skinned meshes: </li>
  <ul>
    <li>Add the script "ParentObject" to either:</li>
    <ul>
      <li>Each gameobject that has a "RayTracingObject" script</li>
      <li>OR</li>
      <li>The Direct Parent gameobject of gameobjects that have a "RayTracingObject" script(Groups their meshes together, increasing performance)</li>
    </ul>
  </ul>
  <li>For Skinned Meshes:</li>
  <ul>
    <li>Any parent gameobject of the RayTracingObjects(will group together all Skinned Children)</li>
  </ul>
  <li>For default unity lights, you just add the "RayTracingLight" script to each one</li>
</ul>


## General Use/Notes
<ul>
  <li>DX12 is recommended, as it enables use of OIDN, Bindless texturing, RT Cores, and higher performance</li>
  <li>The camera you want to render from, you attach the RenderHandler script to(if you have a camera tagged MainCamera, this will be done automatically)</li>
  <li>The green/red rectangle shows when the acceleration structure is done building, and thus ready to render, red means that its not done, and green means its done building, a ding will sound when it completes if it takes longer than 15 seconds(Turn on Truetrace Settings -> Functionality Settings</li>
  <li>Objects can be added and removed at will simply by toggling the associated GameObject with a ParentObject script on/off in the hierarchy(clicking on parent objects with complex objects for children will lag), but they will take time to appear as the acceleration structure needs to be rebuilt for them</li>
  <li>Emissive meshes need to be have a non-zero emissive value when they are built or rebuilt to work with NEE, but after that can have their emissiveness changed at will</li>
  <li>To set up PBR with the DEFAULT BIRP material, all textures go into their proper names, but Roughness goes into the Occlusion texture(This can be changed in the MaterialPairing menu)</li>
  <li>If you are using blendshapes to change geometry of a skinned mesh, you may need to go to the import settings of it(in the inspector), turn off Legacy Blendshape Normals, and make sure all normals are imported, not calculated, otherwise the normals for blendshapes might be wrong</li>
  <li>If you use HDRIs, or CubeMaps for the skybox, you need to format as the texture to a Texture2D in the inspector of the image, unity will convert it automatically, then put it in the slot in "Scene Settings" in the TrueTrace settings menu</li>
  <li>Some settings will be hidden behind an "Advanced Mode" toggle found in "Functionality Settings"</li>
</ul>

## Animating TT Materials
<ul>
  <li>To animate properties of a truetrace material, you need to add the script "MaterialAnimationView" to the same gameobject as the RayTracingObject</li>
  <li>Next, you need to select the targeted submaterial in the dropdown of the newly added script</li>
  <li>Finally, you can now animate any material property by animating the SelectedMaterial properties.</li> 
</ul>

## URP Setup
<ul>
  <li>Add the URPTTInjectPass to the currently used renderer as a renderfeature(The name of which can be found in the Camera)</li>
</ul>

## Creating Non-Standard Images
<ul>
    <li>Attatch the "TTAdvancedImageGen" script to any gameobject in the hierarchy, and fill out the settings you want.</li>
</ul>

## Using Instancing
<ul>
  <li>For use with HWRT, you need unity 6000.0.31f1 or LATER, otherwise this ONLY works with SWRT</li>
  <li>Firstly, all objects that will be the source of instanced objects will need to go under the InstancedStorage and can be arranged as single dynamic objects(ParentObject + RayTracingObject script both go on the same gameobject as the mesh)</li>
  <li>Then, to instance the objects, you just need GameObjects with the InstanceObject script attatched to them under the Scene GameObject, and then drag the desired object instance from the hierarchy to the Instance Parent slot in the InstanceObject script</li>
</ul>

## Linking Shader Textures to TrueTrace
<ul>
  <li>You only need to do this once per SHADER, not once per material. Unconfigured shaders will just appear white in truetrace</li>
  <li>In the PathTracingSettings, click the tab called "Material Pair Options"</li>
  <li>Drag any material that has the shader you want to pair into the material slot that appears</li>
  <li>From here, you will see 3 buttons, click those to add the input type and connect it to the output tab.  Do this with the any material using the default shader for an example</li>
  <li>Once this is done, click "Apply Material Links" and rebuild the BVH in the "Main Options" tab to update the objects in the scene</li>
</ul>

## Functionality Settings Contents
<ul>
  <li>NOTES 1: MOST OF THESE IN THIS TABLE HAVE TOOLTIPS THAT APPEAR IF YOU HOVER OVER THEM!</li>
  <li>NOTES 2: SOME OF THESE ARE HIDDEN BEHIND TTAdvancedMode, WHICH CAN BE TOGGLED IN THIS MENU.</li>
  <li>Enable RT Cores - (DX12 Only, REQUIRES UNITY 2023 OR HIGHER)Enables Hardware RT for cards that support it.</li>
  <li>Use Old Light BVH Instead of Gaussian Tree - Disables the Gaussian Tree for higher performance but worse light sampling on metallics.</li>
  <li>Enable OIDN - (DX12 Only) Adds the OIDN denoiser to the Denoiser list in "Main Options"</li>
  <li>FULLY Disable Radiance Cache - Will free the memory(RAM/VRAM) usually used by the Radiance Cache</li>
  <li>Use Rasterized Lighting For Direct - Experimental, only known to work in BIRP, Forces truetrace to only render indirect</li>
  <li>Enable Emissive Texture Aware Light BVH - Allows for smarter/better sampling of emissive meshes by considering their emissive masks/textures; Can use lots of RAM.</li>
  <li>Load TT Settings From Global File - Normally, each scene has its own TTSettings file, but activating this allows you to put any TTSettings file into the RayTracingMaster script(attached to the Scene gameobject), and it will be used instead.</li>
  <li>Enable Verbose Logging - Truetrace will yell more information into the console.</li>
  <li>Enable Triangle Splitting - Optimization for SWRT that splits triangles to improve tracing performance.</li>
  <li>Enable Strict Memory Reduction - Shrinks compute buffers when objects are removed, which causes stuttering on object add/remove but saves VRAM.</li>
  <li>Save Multiple Maps On Screenshot - Any sort of image saved by truetrace will also generate images of the corrosponding material and mesh IDs.</li>
  <li>Enable Photon Mapped Caustics - Enables the photon mapping pass for caustic generation.  Materials/RayTracingObject scripts(attached to each Mesh) will need to have their flag enabled to generate caustics.</li>
  <li>Remove TT Scripts During Save - Will delete all unmodified raytracingobject and parentobject scripts when the scene is saved, then add them back. This helps with version control.</li>
  <li>Fade Mapping - Not super compatable with realtime denoisers, but allows for surfaces with variable transparency, based on alpha texture.</li>
  <li>Stained Glass - Whether or not to color shadow rays that pass through colored glass, dictated by material parameters: Thin, Albedo, Scatter Distance.</li>
  <li>Use Light BVH - Toggles the use of EITHER the Light BVH or Gaussian Tree on/off; uses the RIS count of NEE if off. Turn off for maximum speed, but poor emissive mesh sampling quality.</li>
  <li>Quick Radcache Toggle - Toggles the radcache on/off. Useful for comparing to ground truth pathtracing.</li>
  <li>Use Texture LOD - Samples LOD of textures based on bounce number, can improve performance(DX12 only).</li>
  <li>Double Buffer Light Tree - Enables double buffering of the light tree, allowing for stable moving emissive objects with ASVGF, but slightly hurts performance with ASVGF.</li>
  <li>Use BSDF Lights - Allows naive sampling of emissive triangles using MIS.  Turn off if your having some issues with fireflies to rely entirely on the light BVH.</li>
</ul>


## Editor Window Guide
TrueTrace Options Description - 
<ul>
  <li>Build Aggregated BVH(Recommended to do any time you add/remove objects in edit mode)- Allows you to pre-build objects BVH's before running so you dont have to wait every time you go into play mode for it to build.</li>
  <li>Take Screenshot - Takes a screenshot to the path under "Functionality Settings" in the TrueTrace options</li>
  <li>Clear Parent Data - Clears the data stored in parent GameObjects, allowing you to actually click them without lagging</li>
  <li>Auto Assign Scripts - Assigns all required scripts to all objects under the Scene GameObject, best way to add objects</li>
  <li>Remaining Objects - Objects still being processed</li>
  <li>Max Bounces - Sets the maximum number of bounces a ray can achieve</li>
  <li>Internal Resolution Ratio - Render scale in comparison to gameview size, turn to below 1 while in edit mode to decrease rendered resolution(to then be upscaled)</li>
  <li>Use Russian Roulette - Highly recommended to leave this on, kills rays that may not contribute much early, and thus greatly increases performance</li>
  <li>Enable Object Moving - Allows objects to be moved during play, and allows for added objects to spawn in when they are done building</li>
  <li>Allow Image Accumulation - Allows the image to accumulate while the camera is not moving</li>
  <li>Use Next Event Estimation - Enables shadow rays/NEE for direct light sampling</li>
    <ul>
      <li>RIS Count - Number of RIS passes done for unity and mesh lights(for mesh lights, it only works if Use Light BVH is off in Functionality Settings)</li>
    </ul>
  <li>Allow Mesh Skinning - Turns on the ability for skinned meshes to be animated or deformed with respect to their armeture</li>
  <li>Denoiser - Allows you to switch between different denoisers</li>
  <li>Use ReSTIR GI - Enables ReSTIR GI which is usually much higher quality(Works with Recur and SVGF denoisers)</li>
    <ul>
      <li>Do Sample Connection Validation - Confirms that two samples are mutually visable and throws it away if they are not</li>
      <li>Enable Temporal - Enables the Temporal pass of ReSTIR GI(allows samples to travel across time, current useless)</li>
      <li>Temporal M Cap - How long a sample may live for, lower means lighting updates faster(until 0 which is the opposite) but more noise(recommended either 0 or around 12, but can be played with)</li>
      <li>Enable Spatial - Enables the Spatial pass of ReSTIR GI(Allows pixels to choose to use the neighboring pixels sample instead)</li>
    </ul>
  <li>Upscaler(ONLY when "Interal Resolution Ratio" is NOT 1) - Allows selection from one of a few upscaling methods</li>
  <li>Use Partial Rendering - Traces only 1 out of (X*X) rays, improving performance</li>
  <li>Enable AntiFirefly - Enables RCRS filter for getting rid of those single bright pixels, will produce artifacts in image if activated too early(frames accumulated)</li>
    <ul>
      <li>Frames Before Anti-Firefly - Frames accumulated before triggering Anti-Firefly</li>
      <li>Anti-Firefly Frame Interval - Anti-Firefly will run once every X frames, this is X</li>
    </ul>
  <li>RR Ignores Primary Hit - Allows for an extra bounce basically, makes it so that dark objects arent noisier, but at the cost of performance</li>
  <li>Atmospheric Scatter Samples - controls how many multiscatter atmospheric samples are precomputed</li>
  <li>Current Samples - Shows how many samples have currently been accumulated</li>
  <li>Enable Tonemapping - Turns on tonemapping, and allows you to select a specific tonemapper</li>
  <li>Use Sharpness Filter - Contrast Adaptive Sharpening</li>
  <li>Enable Bloom - Turns on or off Bloom</li>
  <li>Enable DoF - Turns on or off Depth of Field, and its associated settings</li>
    <ul>
      <li>CTRL + Middle Mouse - Autofocuses to whatever object your mouse is hovering over in the game view</li>
      <li>CTRL + Middle Mouse Scroll - Adjusts the Aperature Size</li>
    </ul>
  <li>Enable Auto/Manual Exposure - Turns on or off Exposure adjustment</li>
  <li>Enable TAA - Enables Temporal Antialiasing</li>
  <li>Enable FXAA - Enables FXAA</li>
</ul>

## Advanced Options
GlobalDefines.cginc Description - 
<ul>
  <li>NOTE: THIS IS REFERING TO THE FILE "GlobalDefines.cginc" WHICH INCLUDES EXTRA FUNCTIONALITY</li>
  <li>To access: TrueTrace-Unity-Pathtracer -> TrueTrace -> Resources -> GlobalDefines.cginc(the blue one)</li>
  <li>Options(Toggle them by removing/adding "//" in front of each #define:</li>
  <li>DONT MODIFY DIRECTLY:</li>
  <ul>
    <li>HardwareRT - This is handled from the CPU side under Functionality Settings</li>
    <li>HDRP - This is handled from the CPU side automatically</li>
    <li>UseSGTree - This is handled from the CPU side under Functionality Settings</li>
    <li>MultiMapScreenshot - This is handled from the CPU side under Functionality Settings; named "Save Multiple Maps On Screenshot"; Truetrace will also save images of material ID and mesh ID maps when using TTAdvancedImageGen or the Screenshot button</li>
  </ul>
  <li>These are fine to modify yourself:</li>
  <ul>
    <li>AdvancedAlphaMapped - Allows for cutout objects to be evaluated during traversal, saving performance for cutout objects but can degrade performance otherwise; best left ON</li>
    <li>ExtraSampleValidation - Allows ReSTIR to shoot 2 shadow rays, one for direct, one for indirect; is a lot more expensive(in SWRT) but can be worth it</li>
    <li>ReSTIRAdvancedValidation - Uses a special small trick to make more use of ReSTIR shadow rays in later frames</li>
    <li>IgnoreGlassShadow - Allows NEE rays to pass through glass</li>
    <li>IgnoreGlassMain - Turning this on basically removes glass from the scene</li>
    <li>FadeMapping - Allows the use of FadeMapping in the Material Options, not stable in denoising(also accessable from Functionality Settings)</li>
    <li>PointFiltering - Samples textures with a point filter, instead of linear or trilinear</li>
    <li>StainedGlassShadow - Paired with "IgnoreGlassShadow", allows colored glass to color these shadow rays that pass through them(also accessable from Functionality Settings)</li>
    <li>IgnoreBackfacing - Rays will not intersect triangle facing away from the ray(also accessable from Functionality Settings)</li>
    <li>LBVH - Whether or not to use RIS for triangle sampling or one of the light trees(also accessable from Functionality Settings)</li>
    <li>AccurateEmissionTex - Whether or not to sample emission textues</li>
    <li>UseTextureLOD - When bindless is on, this allows for dumb sampling of texture LOD's(also accessable from Functionality Settings)</li>
    <li>EONDiffuse - Whether to use Lambert diffuse model or EON diffuse model(also accessable from Functionality Settings)</li>
    <li>AdvancedBackground - Allows selected surfaces to instead be treated as hitting the skybox(also accessable from Functionality Settings)</li>
    <li>UseBRDFLights - Whether to use MIS when using NEE or not; Turning off CAN help massively with fireflies</li>
    <li>DoubleBufferSGTree - Allows ASVGF to better sample moving emissive meshes, but is a lot more expensive with asvgf on</li>
    <li>Fog - Toggles multiscatter fog, not denoiser compatable(also accessable from Functionality Settings)</li>
    <li>RadCache - Quick toggle for the Radiance Cache(also accessable from Functionality Settings)</li>
    <li>ClampRoughnessToBounce - Clamps the material roughness to be higher for each bounce, helps fight fireflies</li>
    <li>ReSTIRSampleReduction - Experiemental, only pathtraces half the rays and lets ReSTIR fill in the gaps</li>
    <li>ReSTIRReflectionRefinement - Experiemental option to allow for cleaner reflections in near-mirrors</li>
    <li>ShadowGlassAttenuation - Advancement of "StainedGlassShadow"; still experimental</li>
    <li>DisableNormalMaps - Debug thing</li>
    <li>ClayMetalOverride - Allows for clay mode to also define a constant metallic/roughness</li>
    <li>MoreAO - Adds fake AO, can help with brightened corners</li>
  </ul>
</ul>



# Known Bugs:
</br>
<ul>
  <li>Please report any you find to the discord or to me directly.</li>
  <li>If your in DX11 and the image displays black, make sure all TrueTrace compute shaders are set to "Caching Preprocessor".</li>
</ul>

# Huge thanks to these people for sponsoring me:
<ul>
  <li>Thanks to:</li>
  <ul>
    <li>Patreon:</li>
    <ul>
      <li>Niko:         $500</li> 
      <li>Duong:        $5</li>
      <li>MakIt3D:      $5</li>
      <li>Yanus:        $5</li>
      <li>Hanmen:       $5</li>
      <li>Andrew:       $3</li>
    </ul>
    <li>Github Sponsors:</li>
    <ul>
      <li>Jhin:         $5</li>
      <li>Omid:         $2</li>
    </ul>
    <li>Kofi:</li>
    <ul>
    </ul>
  </ul>
</ul>

# Sample Images(Taken from various stages of development)

![](/.Images/Yanus2.png)
![](/.Images/Yanus0.png)
![](/.Images/Caustics1.png)
![](/.Images/ModernBistro.png)
![](/.Images/NewSponza3V2.png)
![](/.Images/NewSponza2V2.png)
![](/.Images/Arch2.png)
![](/.Images/CommonRender.png)
![](/.Images/ArchViz5.png)
![](/.Images/ArchViz4.png)
![](/.Images/Loft1.png)
![](/.Images/Cornell.png)
![](/.Images/Lego.png)
![](/.Images/SpaceShip.png)


# Credits(will continue to expand when I have time)
Biggest thanks to Zuen(R.I.P. You will be missed) who helped me a huge amount with the new BVH and traversal, thanks to them I got to where I am now, and I am very thankful to them for their help and patience
</br>
https://github.com/jan-van-bergen
</br></br>
Scenes From:
<ul>
  <li>https://sketchfab.com/3d-models/modern-house-and-garage-1bed-241e5ee17e874697bceb2feacedf44e1</li>
  <li>https://benedikt-bitterli.me/resources/</li>
  <li>https://casual-effects.com/data/</li>
  <li>https://www.intel.com/content/www/us/en/developer/topic-technology/graphics-research/samples.html</li>
</ul>
</br>
Disney BSDF from: https://schuttejoe.github.io/post/disneybsdf/
Rectangle packer for faster atlas creation from here: https://github.com/ThomasMiz/RectpackSharp/tree/main/RectpackSharp
GPU Texture Compression: https://github.com/aras-p/UnityGPUTexCompression
OIDN Wrapper: https://github.com/guoxx/UnityDenoiserPlugin

This project uses:
Crytek Sponza
CC BY 3.0
© 2010 Frank Meinl, Crytek
