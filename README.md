# TrueTrace: A High Performance Compute Shader based Unity Pathtracer

![](/.Images/Yanus0.png)
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
  <li>Efficient Spherical Gaussian Light Tree for Next Event Estimation</li>
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
</ul>

MASSIVE thanks to 
[Alex Bakanov(AKA Meetem)](https://github.com/Meetem)
for bringing bindless textures to unity!
</br>[Ylitie et al](https://research.nvidia.com/sites/default/files/publications/ylitie2017hpg-paper.pdf)
</br>[ebruneton](https://ebruneton.github.io/precomputed_atmospheric_scattering/)
</br>[Convolutional Bloom](https://github.com/AKGWSB/FFTConvolutionBloom)
</br>[Spherical Gaussian Light Tree](https://gpuopen.com/download/publications/Hierarchical_Light_Sampling_with_Accurate_Spherical_Gaussian_Lighting.pdf)
</br>


### If you like what I do and want to support me or this project, Please consider becoming a Github Sponsor or a Patron at patreon.com/Pjbomb2!  This allows me to keep this free for everyone!
### You can contact me easiest through my discord server(above) or my twitter(https://x.com/Pjbomb2) with bugs, ideas, or thoughts on the project!


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
  <li>First, add your gameobjects as a child of the "Scene" gameobject created by TrueTrace.</li>
  <li>Each gameobject that contains a mesh needs the script "RayTracingObject" added to it.</li>
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
  <li>DX12 is recommended, as it enables use of OIDN, Bindless texturing, RT Cores, and slightly higher performance</li>
  <li>The camera you want to render from, you attach the RenderHandler script to(if you have a camera tagged MainCamera, this will be done automatically)</li>
  <li>The green/red rectangle shows when the acceleration structure is done building, and thus ready to render, red means that its not done, and green means its done building, a ding will sound when it completes if it takes longer than 15 seconds(Turn on Truetrace Settings -> Functionality Settings</li>
  <li>Objects can be added and removed at will simply by toggling the associated GameObject with a ParentObject script on/off in the hierarchy(clicking on parent objects with complex objects for children will lag), but they will take time to appear as the acceleration structure needs to  be rebuilt for them</li>
  <li>Emissive meshes need to be have a non-zero emissive value when they are built or rebuilt to work with NEE, but after that can have their emissiveness changed at will</li>
  <li>To set up PBR with the DEFAULT material, all textures go into their proper names, but Roughness goes into the Occlusion texture(This can be changed in the MaterialPairing menu)</li>
  <li>If you are using blendshapes to change geometry of a skinned mesh, you may need to go to the import settings of it(in the inspector), turn off Legacy Blendshape Normals, and make sure all normals are imported, not calculated, otherwise the normals for blendshapes might be wrong</li>
  <li>If you use HDRIs, or CubeMaps for the skybox, you need to format as the texture to a Texture2D in the inspector of the image, unity will convert it automatically, then put it in the slot in "Scene Settings" in the TrueTrace settings menu</li>
</ul>

## Creating Panoramas
<ul>
    <li>Attatch the "PanoramaDoer" script to the "Scene" gameobject in the hierarchy</li>
    <li>Set your settings in the PanoramaDoer</li>
    <li>Enter Play Mode like normal and click the "Create Panorama" button in TrueTrace Settings</li>
    <li>The rest is automatic, from rendering, to stitching and will automatically exit play mode when finished</li>
    <li>The final Panorama will be put in Assets -> Screenshots, and the intermediate slices are in Assets -> TempPanorama</li>
</ul>

## Using Instancing
<ul>
  <li>THIS DOES NOT WORK WITH RT CORES</li>
  <li>Firstly, all objects that will be the source of instanced objects will need to go under the InstancedStorage and can be arranged like normal objects(with regards to the layout of parentobject to raytracingobjects)</li>
  <li>Then, to instance the objects, you just need GameObjects with the InstanceObject script attatched to them under the Scene GameObject, and then drag the desired object instance from the hierarchy to the Instance Parent slot in the InstanceObject script</li>
</ul>

## Linking Shader Textures to TrueTrace
<ul>
  <li>In the PathTracingSettings, click the tab called "Material Pair Options"</li>
  <li>Drag any material that has the shader you want to pair into the material slot that appears</li>
  <li>From here, you will see 4 buttons, click those to add the input type and connect it to the output tab.  Do this with the default material for an example</li>
  <li>Once this is done, click "Apply Material Links" and rebuild the BVH in the "Main Options" tab to update the objects in the scene</li>
</ul>

## Functionality Settings Contents
<ul>
  <li>Enable RT Cores - (DX12 Only, REQUIRES UNITY 2023 OR HIGHER)Enables Hardware RT for cards that support it.</li>
  <li>Disable Bindless Textures - (DX12 Only, Disables bindless texturing, and uses the atlas fallback(Limits resolution).</li>
  <li>Use DX11 - Disables DX12 only toggles, but allows truetrace to run in DX11.</li>
  <li>Enable OIDN - (DX12 Only) Adds the OIDN denoiser to the Denoiser list in "Main Options"</li>
  <li>Disable Radiance Cache - Not reccomended, but will free the memory usually used by the Radiance Cache</li>
  <li>Enable Emissive Texture Aware Light BVH - Allows for smarter/better sampling of emissive meshes by considering their emissive masks/textures; Can use lots of RAM.</li>
</ul>


## Editor Window Guide
TrueTrace Options Description - 
<ul>
  <li>Build Aggregated BVH(Recommended to do any time you change objects in edit mode)- Allows you to pre-build objects BVH's before running so you dont have to wait every time you go into play mode for it to build.</li>
  <li>Clear Parent Data - Clears the data stored in parent GameObjects, allowing you to actually click them without lagging</li>
  <li>Take Screenshot - Takes a screenshot to the path under "Functionality Settings" in the TrueTrace options</li>
  <li>Auto Assign Scripts - Assigns all required scripts to all objects under the Scene GameObject, best way to add objects</li>
  <li>Remaining Objects - Objects still being processed</li>
  <li>Max Bounces - Sets the maximum number of bounces a ray can achieve</li>
  <li>Internal Resolution Ratio - Render scale in comparison to gameview size, turn to below 1 while in edit mode to decrease rendered resolution(to then be upscaled)</li>
  <li>Atlas Size - Maximum size of the texture atlas used(All textures are packed into atlas's so I can send them to the GPU)</li>
  <li>Use Russian Roulette - Highly recommended to leave this on, kills rays that may not contribute much early, and thus greatly increases performance</li>
  <li>Enable Object Moving - Allows objects to be moved during play, and allows for added objects to spawn in when they are done building</li>
  <li>Allow Image Accumulation - Allows the image to accumulate while the camera is not moving</li>
  <li>Use Next Event Estimation - Enables shadow rays/NEE for direct light sampling</li>
    <ul>
      <li>RIS Count - Number of RIS passes done for lights(select the best light out of X number of randomly selected lights, only works if LBVH is off in "GlobalDefines.cginc")</li>
    </ul>
  <li>Allow Mesh Skinning - Turns on the ability for skinned meshes to be animated or deformed with respect to their armeture</li>
  <li>Denoiser - Allows you to switch between different denoisers</li>
  <li>Allow Bloom - Turns on or off Bloom</li>
  <li>Sharpness Filter - Contrast Adaptive Sharpening</li>
  <li>Enable DoF - Turns on or off Depth of Field, and its associated settings</li>
  <li>Enable Auto/Manual Exposure - Turns on or off Exposure</li>
  <li>Use ReSTIR GI - Enables ReSTIR GI which is usually much higher quality(Works with Recur and SVGF denoisers)</li>
    <ul>
      <li>Do Sample Connection Validation - Confirms that two samples are mutually visable and throws it away if they are not</li>
      <li>Update Rate - How many pixels per frame get re-traced to ensure they are still valid paths(7 or 33 is a good number to aim for here at 1080p)</li>]
      <li>Enable Temporal - Enables the Temporal pass of ReSTIR GI(allows samples to travel across time</li>
      <li>Temporal M Cap - How long a sample may live for, lower means lighting updates faster(until 0 which is the opposite) but more noise(recommended either 0 or around 12, but can be played with)</li>
      <li>Enable Spatial - Enables the Spatial pass of ReSTIR GI(Allows pixels to choose to use the neighboring pixels sample instead)</li>
      <li>Spatial Sample Count - How many neighboring pixels are looked at(turn to 0 to make it adapative to sample count)</li>
      <li>Minimum Spatial Radius - The minimum radius the spatial pass can sample from</li>
    </ul>
  <li>Enable TAA - Enables Temporal Antialiasing</li>
  <li>Enable Tonemapping - Turns on tonemapping, and allows you to select a specific tonemapper</li>
  <li>Enable TAAU - Use TAAU for upscaling(if off, you use my semi custom upscaler instead)</li>
  <li>Use Partial Rendering - Traces only 1 out of X rays</li>
  <li>Use AntiFirefly - Enables RCRS filter for getting rid of those single bright pixels</li>
  <li>RR Ignores Primary Hit - Allows for an extra bounce basically, makes it so that dark objects arent noisier, but at the cost of performance</li>
  <li>Atmospheric Scatter Samples - Lower this to 1 if you keep crashing on entering play mode(controls how many atmospheric samples are precomputed)</li>
  <li>Current Samples - Shows how many samples have currently been accumulated</li>
</ul>

## GlobalDefines.cginc Settings
<ul>
  <li>AdvancedAlphaMapping - Enables or Disables the support of cutout objects(performance penalty)</li>
  <li>ExtraSampleValidation - Shoots an additional ray(2 instead of 1) in ReSTIR GI ray validation for sharper shadows</li>
  <li>IgnoreGlassShadow - Shadow Rays can pass through glass</li>
  <li>IgnoreGlassMain - Main Rays can pass through glass</li>
  <li>FadeMapping - Enables experimental Fade material type</li>
  <li>HardwareRT - Turn on if your in Unity 2023 or higher and want to use Hardware RT cores</li>
  <li>PointFiltering - Switch between point and linear filtering for albedo textures</li>
  <li>StainedGlassShadows - Shadow rays passing through glass will be tinted to the glass color</li>
  <li>IgnoreBackFacing - Culls backfacing triangles</li>
  <li>WhiteLights - Forces all lights to be white</li>
  <li>LBVH - Enable/Disable the light BVH</li>
  <li>FasterLightSampling - Uses an alternative method for calculating LBVH PDF that is a bit wrong, but much faster</li>
  <li>AccurateEmissionTex - Turn on/off emission textures</li>
  <li>RadianceCache - Turn on/off the Radiance Cache</li>
  <li>IndirectRetraceWeighting - Adds indirect lighting into ReSTIR GI retracing/luminance validation</li>
  <li>TrueBlack - Allows materials to be truely black, removes the bottom limit</li>
  <li>AdvancedRadCacheAlt - Experimental working set for the Radiance Cache, not recomended</li>
  <li>UseTextureLOD - (Only works with Bindless)Enables Texture LOD</li>
  <li>DebugView - Replace that "DVNone" with any of the defines below, from "DVNone" to "DVGIView"</li>
</ul>


## IES System
<ul>
    <li>Add the texture highlighted in the image below to the "IES Profile" slot in the raytracinglights component thats added to standard unity lights(directional, point, spot, etc. type lights)</li>
</ul>

![](/.Images/IESInstructions0.png)

# Known Bugs:
</br>
<ul>
  <li>Report any you find! There WILL be bugs, I just dont know what they are</li>
</ul>

# Huge thanks to these people for being sponsors/patrons:
<ul>
  <li>Thanks to:</li>
  <ul>
    <li>Patreon:</li>
    <ul>
      <li>Niko Kudos:   $500</li> 
      <li>Duong Nguyen: $5</li>
      <li>MakIt3D:      $5</li>
      <li>Yanus:        $5</li>
      <li>Andrew Varga: $3.34</li>
      <li>DJ Huang:     $3</li>
    </ul>
    <li>Github Sponsors:</li>
    <ul>
      <li>Omid:         $2</li>
    </ul>
    <li>Kofi:</li>
  </ul>
</ul>

# Sample Images(Taken from various stages of development)

![](/.Images/Arch2.png)
![](/.Images/CommonRender.png)
![](/.Images/SunTemple1.png)
![](/.Images/ModernBistro.png)
![](/.Images/ArchViz5.png)
![](/.Images/ArchViz4.png)
![](/.Images/Loft1.png)
![](/.Images/NewSponza3V2.png)
![](/.Images/NewSponza2V2.png)
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
