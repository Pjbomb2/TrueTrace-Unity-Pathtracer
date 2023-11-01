![](/Images/ArchRender0.png)
# If you like what I do and want to support me and this project(as this takes a LOT of my time), Please consider becoming a Github Sponsor or a Patron at patreon.com/Pjbomb2!  This allows me to keep this free for everyone!
# Discord Server: https://discord.gg/4Yh7AZuhcD
## Demo:  https://drive.google.com/file/d/1vzrRyNftRRVyJ1SVbUvyNhQh0uXQso-N/view?usp=sharing
Notes:</br>

# Compute Shader Based Unity PathTracer
A passion projects that has been going on for a while with the goal of bringing at least interactive path-tracing to everyone in unity, regardless of their hardware
## Features: 
<ul>
<li>Fast Compute Shader based path tracing</li>
<li>Disney BSDF for materials with support for emissive meshes and Video Players</li>
<li>Ability to move, add, and remove objects during play</li>
<li>Ability to update material properties on the fly during play</li>
<li>ASVGF, SVGF, and my own Recurrent Denoisers</li>
<li>Compressed Wide Bounding Volume Hierarchy as the Acceleration Structure (See Ylitie et al. 2017 below)</li>
<li>PBR Texture Support</li>
<li>Next Event Estimation with Multiple Importance Sampling for Explicit Light Sampling</li>
<li>Support for all default unity lights, which interact via NEE</li>
<li>Bloom, Depth of Field, AutoExposure, TAA, Tonemappiong</li>
<li>No specific GPU vendor needed(this will run on integrated graphics if you so wish it, aka no RTX cores needed)</li>
<li>Precomputed Multiple Atmospheric Scattering for dynamic and realtime sky(from ebruneton below)</li>
<li>Object Instancing</li>
<li>ReSTIR GI for faster convergence in complex scenes and more complete images in scenes with changing lighting</li>
<li>Simple upscaling ability for performance increase</li>
<li>Hardware RT Support for modern cards</li>
<li>Supports Built-in, HDRP, and URP</li>
<li>Full skinned mesh support for animated skinned meshes</li>
</ul>

[Ylitie et al](https://research.nvidia.com/sites/default/files/publications/ylitie2017hpg-paper.pdf)
</br>[ebruneton](https://ebruneton.github.io/precomputed_atmospheric_scattering/)
</br>

If you have any questions, or suggestions, etc. let me know either through github issues or my twitter or my discord! I am always looking for more stuff to add, and more ways to make it more user friendly or appealing for others to use
## You can contact me easiest through my discord server(above) or my twitter(https://twitter.com/Pjbomb2)


## Notes:
Let me know if you use this for anything, I would be excited to see any use of this!  Just please give some credit somewhere if you use it, thank you!

# Requires Unity 2021 or higher
# Instructions:
## Required Settings Changes:
<ul>
  <li>Set the Color Space to Linear through Edit Tab(Top Left) -> Project Settings -> Player -> Other Settings -> Color Space, and change from Gamma to Linear</li>
  <li>Enable Unsafe Code(Its for memory management) through Edit -> Project Settings -> Player -> Other Settings -> "Allow 'unsafe' Code" (near the bottom)</li>
  <li>Change the Graphics Api for Windows to DirectX12 through Edit Tab(Top Left) -> Project Settings -> Player -> Other Settings -> Untoggle "Auto Graphics API For Windows", then click the little + that appears, select "Direct3D12(Experimental)", and drag that to the top.  A restart of the editor is required</li>
  <li>Your target camera NEEDS to be deferred</li>
</ul>

## Controls:
Camera Controls: WASD, Mouse, and press T to freeze/unfreeze the camera(Camera starts frozen), and shift increases speed

## General Setup
<ul>
  <li>Download and import the UnityPackage provided and open the new Pathtracer Settings at the top of the screen, and click "Arrange Hierarchy"(This WILL re-arrange your hierarchy)</li>
  <li>Whenever you add a new object(or tree of objects), you need to add it to under the GameObject named Scene, and its recommended you press "Auto Assign Scripts" to automatically assign the needed scripts to the new objects</li>
</ul>

## Basic script structure breakdown:
<ul>
  <li>Top Level is a GameObject called Scene with an AssetManager and RayTracingMaster script attached</li>
  <li>Second Level: Parent Object Script - Attach this to all objects that will have children with meshes you want to raytrace</li>
  <li>Third Level: RayTracingObject Script - This defines what meshes get raytraced, must either be a direct child of a GameObject with the ParentObject Script, or in the same GameObject as the ParentObject Script</li>
  <li>Misc Level: Unity Lights - Must have a RayTracingLight script attached to be considered(and UseNEE needs to be on)</li>
</ul>

## General Use/Notes
<ul>
  <li>The camera you want to render from, you attach the RenderHandler script to(if you have a camera tagged MainCamera, this will be done automatically)</li>
  <li>The green/red rectangle shows when the acceleration structure is done building, and thus ready to render, red means that its not done, and green means its done building</li>
  <li>Objects can be added and removed at will simply by toggling the associated GameObject with a ParentObject script on/off in the hierarchy(clicking on parent objects with complex objects for children will lag), but they will take time to appear as the acceleration structure needs to  be rebuilt for them</li>
  <li>Emissive meshes need to be emissive when you build the hierarchy to work with NEE, and can have their emissiveness at will</li>
  <li>If you use normal maps, they need to be in unity normal map format</li>
  <li>To set up PBR with the DEFAULT material, all textures go into their proper names, but Roughness goes into the Occlusion texture(This can be changed in the MaterialPairing menu)</li>
  <li>If you are using blendshapes to change geometry of a skinned mesh, you may need to go to the import settings of it(in the inspector), turn off Legacy Blendshape Normals, and make sure all normals are imported, not calculated, otherwise the normals for blendshapes might be wrong</li>
  <li>A fun thing you may want to do is go to TrueTrace -> Resources -> RenderPipelines -> RendererHandle, and uncomment the "[ImageEffectOpaque]"</li>
  <li>If you use HDRIs, or CubeMaps for the skybox, you need to format as the texture to a cube shape in the inspector of the image, unity will convert it automatically, then put it in the slot in Scene Settings</li>
</ul>

## Using Instancing
<ul>
  <li>Firstly, all objects that will be the source of instanced objects will need to go under the InstancedStorage and can be arranged like normal objects(with regards to the layout of parentobject to raytracingobjects)</li>
  <li>Then, to instance the objects, you just need GameObjects with the InstanceObject script attatched to them under the Scene GameObject, and then drag the desired object instance from the hierarchy to the Instance Parent slot in the InstanceObject script</li>
</ul>

## Linking your own Materials
<ul>
  <li>This is how to take your material, and have the textures assign properly, meaning you don't need to use a specific material</li>
  <li>In the PathTracingSettings, click the tab called "Material Pair Options"</li>
  <li>Drag any material that has the shader you want to pair into the material slot that appears</li>
  <li>From here, you need to select each dropdown that appears and select the property that is associated with the text to the left of the dropdown</li>
  <li>If the names in the dropdown make no sense, you will have to navigate to the shader itself in the inspector(select the material in the project folder, click the 3 dots in the inspector, and click select shader), this should display the names along with what they mean to unity</li>
  <li>Once this is done, click "Apply Material Links" and rebuild the BVH in the "Main Options" tab to update the objects in the scene</li>
</ul>

## Using HDRP
<ul>
  <li>Go into TrueTrace -> Resources -> GlobalDefines.cginc, and uncomment the #define HDRP</li>
  <li>This is used by attatching it to a custom pass, so create a new custom pass(Hierarchy -> Volume -> Custom Pass) and add the custom pass in the inspector called "HDRP Compatibility"</li>
</ul>

## Using Hardware RT
<ul>
  <li>First off, this REQUIRES unity 2023 or higher</li>
  <li>In the TrueTrace settings menu, click on the top right button "Functionality Settings" and toggle HardwareRT</li>
  <li>Uncomment the #define HardwareRT in: TrueTrace -> Resources -> GlobalDefines.cginc</li>
  <li>Then just use like normal, but this does not support Instances</li>
</ul>

## Editor Window Guide
BVH Options Description - 
<ul>
  <li>Build Aggregated BVH(Recommended to do any time you change objects in edit mode)- Allows you to pre-build objects BVH's before running so you dont have to wait every time you go into play mode for it to build.</li>
  <li>Clear Parent Data - Clears the data stored in parent GameObjects, allowing you to actually click them without lagging, and for you to save the scene(but will then require the BVH to be rebuilt)</li>
  <li>Take Screenshot - Takes a screenshot to Assets/ScreenShots folder(Will be created if missing)</li>
  <li>Auto Assign Scripts - Assigns all required scripts to all objects under the Scene GameObject, best way to add objects</li>
  <li>Make All Static - Utility button that takes all objects in the scene and puts them under one parent object, not recommended for general use</li>
  <li>Force Instances - Looks at all meshes in the scene, sees what objects have the same meshes, and makes them into instances, keep in mind instances use the same material and textures</li>
  <li>Remaining Objects - Objects still being processed</li>
  <li>Max Bounces - Sets the maximum number of bounces a ray can achieve</li>
  <li>Render Scale - Render scale in comparison to gameview size, turn to below 1 while in edit mode to decrease rendered resolution(to then be upscaled)</li>
  <li>Atlas Size - Maximum size of the texture atlas used(All textures are packed into atlas's so I can send them to the GPU)</li>
  <li>Use Russian Roulette - Highly recommended to leave this on, kills rays that may not contribute much early, and thus greatly increases performance</li>
  <li>Enable Object Moving - Allows objects to be moved during play, and allows for added objects to spawn in when they are done building</li>
  <li>Allow Image Accumulation - Allows the image to accumulate while the camera is not moving</li>
  <li>Use Next Event Estimation - Enables shadow rays/NEE for direct light sampling</li>
  <li>RIS Count - Number of RIS passes done for lights(select the best light out of X number of randomly selected lights)</li>
  <li>Allow Mesh Skinning - Turns on the ability for skinned meshes to be animated or deformed with respect to their armeture</li>
  <li>Denoisers -> Allows you to switch between different denoisers</li>
  <li>Allow Bloom - Turns on or off Bloom</li>
  <li>Enable DoF - Turns on or off Depth of Field, and its associated settings</li>
  <li>Autofocus DoF - Sets the focal length to bring whatever is in the center of the screen into focus</li>
  <li>Enable Auto/Manual Exposure - Turns on or off Auto Exposure(Set all the way to left to use autoexposure)</li>
  <li>Use ReSTIR GI - Enables ReSTIR GI which is usually much higher quality(Does not work with ASVGF)</li>
  <li>Do Sample Connection Validation - Confirms that two samples are mutually visable and throws it away if they are not</li>
  <li>Update Rate - How many pixels per frame get re-traced to ensure they are still valid paths(7 is a good number to aim for here)</li>
  <li>Enable Temporal - Enables the Temporal pass of ReSTIR GI(allows samples to travel across time</li>
  <li>Temporal M Cap - How long a sample may live for, lower means lighting updates faster(until 0 which is the opposite) but more noise(recommended either 0 or around 12, but can be played with)</li>
  <li>Enable Spatial - Enables the Spatial pass of ReSTIR GI(Allows pixels to choose to use the neighboring pixels sample instead)</li>
  <li>Spatial Sample Count - How many neighboring pixels are looked at(turn to 0 to make it adapative to sample count)</li>
  <li>Minimum Spatial Radius - The minimum radius the spatial pass can sample from</li>
  <li>Use Temporal Antialiasing - Enables Temporal Antialiasing(TAA)</li>
  <li>Enable Tonemapping - Turns on tonemapping</li>
  <li>Enable TAAU - Use TAAU for upscaling(if off, you use my semi custom upscaler instead)</li>
  <li>Use Partial Rendering - Traces only 1 out of X rays</li>
  <li>Use AntiFirefly - Enables RCRS filter for getting rid of those single bright pixels</li>
  <li>RR Ignores Primary Hit - Allows for an extra bounce basically, makes it so that dark objects arent noisier, but at the cost of performance</li>
  <li>Atmospheric Scatter Samples - Lower this to 1 if you keep crashing on entering play mode(controls how many atmospheric samples are precomputed)</li>
  <li>Current Samples - Shows how many samples have currently been accumulated</li>
  </ul>

## Materials(RayTracingObject script)
 <ul>
  <li>Selected Material - Selects which material you want to edit on the mesh</li>
  <li>Material Type - Select your type of material you want(You usually want Disney, its the most versatile)</li>
  <li>Base Color - If theres no Albedo Texture, this is the color the object will be</li>
  <li>Emission - The emittance of an object(how much light it gives off)</li>
  <li>Emission Color - Changes the color of emissive objects, most useful when you have an emission mask on an object</li>
  <li>Roughness - Roughness of the object</li>
</ul>

## Disney BSDF Only Properties
<ul>
  <li>IOR - Index of Refraction of an object.  Affects only Disney BSDF</li>
  <li>Metallic - How metallic an object is.  Affects only Disney BSDF</li>
  <li>Specular - Adds specular reflection to an object, use in conjunction with Roughness and IOR.  Affects only Disney BSDF</li>
  <li>Specular Tint - Weights color more towards the objects color for specular reflections.  Affects only Disney BSDF</li>
  <li>Sheen - Adds Sheen to objects.  Affects only Disney BSDF</li>
  <li>SheenTint - Allows you to choose if an objects sheen is white or is that objects base color.  Affects only Disney BSDF</li>
  <li>ClearCoat - Adds a ClearCoat effect to the object.  Affects only Disney BSDF</li>
  <li>ClearCoatGloss - Influences the ClearCoat.  Affects only Disney BSDF</li>
  <li>Anisotropic - Makes the material(mostly metallic and specular) anisotropic.  Affects only Disney BSDF</li>
  <li>Specular Transmission - Makes an object more or less like glass.  Affects only Disney BSDF(Play with the IOR for this)</li>
  <li>Diffuse Transmission - Makes an object Diffuse but Transmissive(transluscent).  Affects only Disney BSDF</li>
  <li>Transmission Color - doesn't do anything for now, used for volumetric disney bsdf(which is not yet implemented)</li>
  <li>Flatness - Affects Thin objects.  Affects only Disney BSDF</li>
  <li>Thin - Marks an object as thin so it can be better handled by the BSDF.  Affects only Disney BSDF, can be either 0 or 1</li>
 </ul>

# Known Bugs:
</br>
<ul>
  <li>Report any you find! There WILL be bugs, I just dont know what they are</li>
</ul>

# Huge thanks to these people for being sponsors/patrons:
<ul>
  <li>MakIt3D</li>
  <li>Christian Wauben</li>
  <li>John Draisey</li>
  <li>Andrew Varga</li>
  <li>Duong Nguyen</li>
  <li>UnityCoder</li>
  <li>jhintringer</li>
</ul>

# Sample Images(Taken from various stages of development)

![](/Images/SunTemple1.png)
![](/Images/ModernBistro.png)
![](/Images/SunTemple2.png)
![](/Images/Cozy1.png)
![](/Images/ArchRender1.png)
![](/Images/ArchRender2.png)
![](/Images/ArchRender3.png)
![](/Images/ArchRender4.png)
![](/Images/ArchViz3.png)
![](/Images/ArchViz1.png)
![](/Images/ArchViz2.png)
![](/Images/Loft1.png)
![](/Images/Portal.png)
![](/Images/ShittyMesh.png)
![](/Images/HQFurry.png)
![](/Images/Loft.png)
![](/Images/Minecraft.png)
![](/Images/Loft1.png)
![](/Images/Portal2.png)
![](/Images/NewBlender.png)
![](/Images/NewReSTIRV2.png)
![](/Images/NewSponza3V2.png)
![](/Images/NewSponza1V2.png)
![](/Images/NewSponza2V2.png)
![](/Images/Cornell.png)
![](/Images/SanMiguel1.png)
![](/Images/SanMiguel2.png)
![](/Images/Sponza1V2.png)
![](/Images/BistroUpdated.png)
![](/Images/Room.png)
![](/Images/Lego.png)
![](/Images/RealisticSponza.png)
![](/Images/Blender1.png)
![](/Images/ReSTIR5.png)
![](/Images/ReSTIR2.png)
![](/Images/Restir3.png)
![](/Images/PBRTest1.png)
![](/Images/NewFog.png)
![](/Images/Sunset1.png)
![](/Images/VolumeScene2.png)
![](/Images/VolumeScene1.png)
![](/Images/SpaceShip.png)


# Credits(will continue to expand when I have time)
Biggest thanks to Zuen(R.I.P. You will be missed) who helped me a huge amount with the new BVH and traversal, thanks to them I got to where I am now, and I am very thankful to them for their help and patience
</br>
https://github.com/jan-van-bergen(R.I.P. You are missed)
</br></br>
Scenes From:
<ul>
  <li>https://www.blendermarket.com/products/blender-eevee-modern-villa</li>
  <li>https://sketchfab.com/3d-models/bali-villa-modern-barn-house-scandinavian-barn-145684ac3b9b457f88ff2798acdb4306</li>
  <li>https://sketchfab.com/3d-models/bali-villa-a-frame-house-mid-century-modern-c57267678a924eacad51414afd3ade24</li>
  <li>https://benedikt-bitterli.me/resources/</li>
  <li>https://casual-effects.com/data/</li>
  <li>https://www.intel.com/content/www/us/en/developer/topic-technology/graphics-research/samples.html</li>
  <li>https://sketchfab.com/3d-models/tallers-de-la-fundacio-llorenc-artigas-22-5224b721b0854998a1808fcea3cff924</li>
  <li>https://sketchfab.com/3d-models/dae-bilora-bella-46-camera-game-ready-asset-eeb9d9f0627f4783b5d16a8732f0d1a4</li>
  <li>https://www.unrealengine.com/marketplace/en-US/product/victorian-train-station-and-railroad-modular-set</li>
</ul>
</br>
Disney BSDF from: https://schuttejoe.github.io/post/disneybsdf/
Rectangle packer for faster atlas creation from here: https://github.com/ThomasMiz/RectpackSharp/tree/main/RectpackSharp
GPU Texture Compression: https://github.com/aras-p/UnityGPUTexCompression

This project uses:
Crytek Sponza
CC BY 3.0
© 2010 Frank Meinl, Crytek