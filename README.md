![](/Images/ArchRender0.png)
# If you like what I do and want to support me and this project(as this takes a LOT of my time), Please consider becoming a Github Sponsor or a Patron at patreon.com/Pjbomb2!  This allows me to keep this free for everyone!
# Discord Server: https://discord.gg/4Yh7AZuhcD
## Demo:  https://drive.google.com/file/d/11j8a-7S_fbJsKLKMpmd86aIAcnFq9ZA8/view?usp=sharing
Notes:</br>
Currently working on:
<ul>
  <li>Volumetric voxels, looking for ways to make it faster or be able to precompute individual light contributions</li>
  <li>Looking desperately for optimizations(let me know if you have any ideas)</li>
</ul>
Currently needs to be done but havent implemented fully:
<ul>
    <li>Need More Ideas</li>
</ul>
Currently want to do but havent really started:
<ul>
    <li>Volumetric Clouds(back to square one)</li>
</ul>

# Compute Shader Based Unity PathTracer
A passion projects that has been going on for awhile with the goal of bringing at least interactive pathtracing to everyone in unity, regardless of their hardware
## Features: 
<ul>
<li>Fast Compute Shader based path tracing</li>
<li>Emissive, Video Player, and Disney BSDF for materials</li>
<li>Ability to move, add, and remove objects during play</li>
<li>Ability to update material properties on the fly during play</li>
<li>ASVGF and SVGF Denoiser</li>
<li>Compressed Wide Bounding Volume Hierarchy as the Acceleration Structure (See Ylitie et al. 2017 below)</li>
<li>PBR Texture Support</li>
<li>Next Event Estimation with Multiple Importance Sampling for Explicit Light Sampling</li>
<li>Support for default unity lights which interact via NEE(Supports Directional, Point Spot, and Area lights)</li>
<li>Bloom, Depth of Field, AutoExposure, TAA</li>
<li>No specific GPU vendor needed(this will run on integrated graphics if you so wish it, aka no RTX cores)</li>
<li>MagicaVoxel support</li>
<li>Ability to pathtrace voxels and triangle scenes at the same time</li>
<li>Precomputed Multiple Atmospheric Scattering for dynamic and realtime sky(from ebruneton below)</li>
<li>Object Instancing</li>
<li>ReSTIR GI for faster convergence in complex scenes and more complete images in scenes with changing lighting</li>
<li>Simple upscaling ability for performance increase</li>
<li>Hardware RT Support for modern Nvidia Cards</li>
<li>Supports Built-in, HDRP, and URP</li>
<li>Full skinned mesh support for animated skinned meshes</li>
</ul>

[Ylitie et al](https://research.nvidia.com/sites/default/files/publications/ylitie2017hpg-paper.pdf)
</br>[ebruneton](https://ebruneton.github.io/precomputed_atmospheric_scattering/)
</br>

If you have any questions, or suggestions, etc. let me know either through github issues or my twitter or my discord! I am always looking for more stuff to add, and more ways to make it more user friendly or appealing for others to use
## You can contact me easiest through my discord server or my twitter: https://twitter.com/Pjbomb2


## Notes:
Let me know if you use this for anything, I would be excited to see any use of this!  Just please give some credit somewhere if you use it, thank you!

# Requires Unity 2021 or higher
# Instructions:
## Required Settings Changes:
<ul>
  <li>Set the Color Space to Linear through Edit Tab(Top Left) -> Project Settings -> Player -> Other Settings -> Color Space, and change from Gamma to Linear</li>
  <li>Enable Unsafe Code(Its for memory management) through Edit -> Project Settings -> Player -> Other Settings -> "Allow 'unsafe' Code" (near the bottom)</li>
  <li>Change the Graphics Api for Windows to DirectX12 through Edit Tab(Top Left) -> Project Settings -> Player -> Other Settings -> Untoggle "Auto Graphics API For Windows", then click the little + that appears, select "Direct3D12(Experimental)", and drag that to the top.  A restart of the editor is required</li>
</ul>

## Controls:
Camera Controls: WASD, Mouse, and press T to freeze/unfreeze the camera(Camera starts frozen), and shift increases speed

## General Setup
<ul>
  <li>Download and import the UnityPackage provided and open the new Pathtracer Settings at the top of the screen(This WILL re-arrange your hierarchy a bit)</li>
  <li>Whenever you add a new object(or tree of objects), you need to add it to under the gameobject named Scene, and its reccomended you press "Auto Assign Scripts" to automatically assign the needed scripts to the new objects</li>
  <li>Dont use free resolution</li>
</ul>

## Basic script structure breakdown:
<ul>
  <li>Top Level is a gameobject called Scene with an AssetManager and RayTracingMaster script attatched</li>
  <li>Second Level: Parent Object Script - Attatch this to all objects that will have children with meshes you want to raytrace(can be a child to any gameobject as long as its eventual parent is the AssetManager script)</li>
  <li>Third Level: RayTracingObject Script - This defines what meshes get raytraced, must either be a direct child of a gameobject with the ParentObject Script, or in the same gameobject as the ParentObject Script</li>
  <li>Misc Level: Unity Lights - Must have a RayTracingLight script attatched to be considered(and UseNEE needs to be on), can be ANYWHERE in the hierarchy, only supports Point, Spotlight, and 1 Directional</li>
</ul>

## General Use/Notes
<ul>
  <li>The camera you want to render from, you attatch the RenderHandler script to(if you have a camera tagged MainCamera, this will be done automatically)</li>
  <li>The green/red rectangle shows when the acceleration structure is done building, and thus ready to render, red means that its not done, and green means its done building</li>
  <li>Objects can be added and removed at will simply by toggling the associated gameobject with a ParentObject script on/off in the hierarchy(clicking on parent objects with complex objects for children will lag), but they will take time to appear as the acceleration structure needs to  be rebuilt for them</li>
  <li>If you change the emissiveness of an object, you need to disable and re-enable its parent(basically reloading it) if you want to take advantage of NEE correctly sampling it(Does not need to be reloaded for Naive tracing)</li>
  <li>If you use normal maps, they need to be in unity normal map format</li>
  <li>To set up PBR with the default material, all textures go into their proper names, but Roughness goes into the Occlusion texture(Since path tracing gets ambient occlusion by default)</li>
  <li>If you are using blendshapes to change geometry of a skinned mesh, you may need to go to the import settings of it(in the inspector), turn off Legacy Blendshape Normals, and make sure all normals are imported, not calculated, otherwise the normals for blendshapes might be wrong</li>
</ul>

## MagicaVoxel Usage
<ul>
  <li>Before anything, the files need to be in .txt format, to do this, go to the file in file explorer, and rename the .vox to .txt and change the format</li>
  <li>Firstly, you still need to have a gameobject under the scene gameobject to attatch your voxel model to</li>
  <li>Second, you need to attatch a VoxelObject to that gameobject(Located under Assets->Resources->BVH->VoxelObject)</li>
  <li>Next you need to attatch the voxel model to this script, by dragging your voxel model asset in the project tab to the VoxelRef space in the VoxelObject script</li>
</ul>

## Using Instancing
<ul>
  <li>First, there needs to be a gameobject called InstancedStorage in the scene with the InstanceManager attatched to it as a sibling object of the Scene gameobject(this is automatically created on initial start of the scene and pathtracing settings)</li>
  <li>Second, all objects that will be the source of instanced objects will need to go under the InstancedStorage and can be arranged like normal objects(with regards to the layout of parentobject to raytracingobjects)</li>
  <li>Finally, to instance the objects, you just need gameobjects with the InstanceObject script attatched to them under the Scene gameobject, and then drag the desired object instance from the hierarchy to the Instance Parent slot in the InstanceObject script(all of this is displayed in the demoscene)</li>
</ul>

## Linking your own Materials
<ul>
  <li>So this isnt how to use your own materials, this is how to take your material, and have the textures assign properly</li>
  <li>Firstly, all material links are defined under Resources -> Utility -> MaterialMappings; On the first time running with a new material, a basic definition for it will be created at the bottom of this file</li>
  <li>Secondly, all data that you need for this is gotten from clicking on the material you want to add support for in unity, going to the inspector, pressing the 3 dots in the top right, and then Select Shader</li>
  <li>Then, you need to expand the arrow called "Properties", and the names on the left side will be the names you need to enter into the MaterialMappings file</li>
  <li>From here, you need to look at the names on the right side of the list under the properties dropdown, and match what the variable is doing to the texture/range name on the left</li>
  <li>For example, for the Standard Shader/Material, if on the right theres an entry called "Metallic (Range)", you know this is the metallic float variable, so you take the name on the left of this, which in this case is "_Metallic".  Copy the "_Metallic", go to the MaterialMappings xml file, find your material by its shader name, find the corropsponding variable (in this case we are looking for MetallicRange), and replace the "null" with "_Metallic"</li>
  <li>Doing this will indicate that the slider property "Metallic" in the materials UI(the slider you directly interact in unity if you click the material) is actually the Metallic property in my own material, and should be set to the same value</li>
</ul>

## Using HDRP
<ul>
  <li>All you need to do is to use it like normal, but go into TrueTrace -> Resources -> GlobalDefines.cginc, and uncomment the #define HDRP</li>
</ul>

## Using Hardware RT
<ul>
  <li>First off, this REQUIRES unity 2023 or higher and REQUIRES an Nvidia GPU of 20 series or higher</li>
  <li>All you need to do is uncomment the #define HardwareRT in the following files:</li>
  <li>TrueTrace -> Resources -> GlobalDefines.cginc</li>
  <li>TrueTrace -> Resources -> AssetManager.cs</li>
  <li>TrueTrace -> Resources -> RayTracingMaster.cs</li>
  <li>TrueTrace -> Resources -> Objects -> ParentObject.cs</li>
  <li>Then just use like normal</li>
</ul>

## Editor Window Guide
BVH Options Description - 
<ul>
  <li>Build Aggregated BVH(Recommended to do any time you change objects in edit mode)- Allows you to pre-build objects BVH's before running so you dont have to wait every time you go into play mode for it to build.</li>
  <li>Clear Parent Data - Clears the data stored in parent gameobjects, allowing you to actually click them without lagging(but will then require the BVH to be rebuilt)</li>
  <li>Take Screenshot - Requires a folder called Screenshots under the asset folder, takes a screenshot</li>
  <li>Auto Assign Scripts - Assigns all required scripts to all objects under the Scene gameobject, best way to add objects</li>
  <li>Make All Static - Utility button that takes all objects in the scene and puts them under one parent object, not reccomended for general use</li>
  <li>Force Instances - Looks at all meshes in the scene, sees what objects have the same meshes, and makes them into instances, keep in mind instances use the same material and textures</li>
  <li>Remaining Objects - Objects still being processed</li>
  <li>Max Bounces - Sets the maximum number of bounces a ray can achieve</li>
  <li>Render Scale - Render scale in comparison to gameview size, turn to below 1 while in edit mode to decrease rendered resolution(to then be upscaled)</li>
  <li>Atlas Size - Maximum size of the texture atlas used(All textures are packed into atlas's so I can send them to the GPU)</li>
  <li>Use Russian Roulette - Highly reccomended to leave this on, kills rays that may not contribute much early, and thus greatly increases performance</li>
  <li>Enable Object Moving - Allows objects to be moved during play, and allows for added objects to spawn in when they are done building</li>
  <li>Allow Image Accumulation - Allows the image to accumulate while the camera is not moving</li>
  <li>Use Next Event Estimation - Enables shadow rays/NEE for direct light sampling</li>
  <li>RIS Count - Number of RIS passes done for default unity lights(select the best light out of X number of lights)</li>
  <li>Allow Mesh Skinning - Turns on the ability for skinned meshes to be animated or deformed with respect to their armeture</li>
  <li>Allow Bloom - Turns on or off Bloom</li>
  <li>Enable DoF - Turns on or off Depth of Field, and its associated settings</li>
  <li>Enable Auto/Manual Exposure - Turns on or off Auto Exposure(impacts a lot more than I thought it would)</li>
  <li>Use ReSTIR GI - Enables ReSTIR GI which is usually much higher quality</li>
  <li>Do Sample Connection Validation - Confirms that two samples are mutually visable and throws it away if they are not</li>
  <li>Update Rate - How many pixels per frame get re-traced to ensure they are still valid paths(7 is a good number to aim for here)</li>
  <li>Enable Indirect Clamping - Can improve splotchyness when Permutation Samples is on</li>
  <li>Enable Temporal - Enables the Temporal pass of ReSTIR GI(allows samples to travel across time</li>
  <li>Temporal M Cap - How long a sample may live for, lower means lighting updates faster(until 0 which is the opposite) but more noise(reccomended either 0 or around 12, but can be played with)</li>
  <li>Permute Temporal Samples - Turns on permutation sampling, can lead to much higher quality much faster, but a lower M cap is reccomended(around 3-12)</li>
  <li>Enable Spatial - Enables the Spatial pass of ReSTIR GI(Allows pixels to choose to use the neighboring pixels sample instead)</li>
  <li>Spatial Sample Count - How many neighboring pixels are looked at(turn to 0 to make it adapative to sample count)</li>
  <li>Enable Edge Filling - Replaces the black edges when moving the camera to a smear, up to personal preference</li>
  <li>Minimum Spatial Radius - The minimum radius the spatial pass can sample from</li>
  <li>Use Temporal Antialiasing - Enables Temporal Antialiasing(TAA)</li>
  <li>Enable SVGF - Turns on the SVGF denoiser</li>
  <li>(If SVGF Denosier is on)Atrous Kernel Size - The amount of times the SVGF denoiser runs through the Atrous kernel</li>
  <li>Enable ASVGF - Turns on the ASVGF denoiser(ReSTIR GI needs to be off)</li>
  <li>Enable Tonemapping - Turns on Uchimura Tonemapping</li>
  <li>Enable TAAU - Use TAAU for upscaling(if off, you use my semi custom upscaler instead)</li>
  <li>Use Altered Throughput Pipeline - Calculates throughput differently, helps with semi metallic surfaces for ASVGF</li>
  <li>Use Checkerboarding - Only traces half the indirect rays, so quality is sightly decreased(not really noticeable) for a slight increase in performance</li>
  <li>Use AntiFirefly - Enables RCRS filter for getting rid of those single bright pixels</li>
  <li>Enable Median Filter - This needs to be on from the start of the render if you intend to use it(requires a slightly different pipeline)</li>
  <li>Use Median Filter - Turns on the median filter(is rather intensive so its more of a postprocessing step)</li>
  <li>Enable Median Filter Accumulation - Sometimes the median filter can produce some noise, this is a seperate accumulation pass to get rid of those</li>
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
  <li>ClearCoat - Adds a Clearcoat effect to the object.  Affects only Disney BSDF</li>
  <li>ClearCoatGloss - Influences the Clearcoat.  Affects only Disney BSDF</li>
  <li>Anisotropic - Makes the material(mostly metallic and specular) anisotropic.  Affects only Disney BSDF</li>
  <li>Specular Transmission - Makes an object more or less like glass.  Affects only Disney BSDF(Play with the IOR for this)</li>
  <li>Diffuse Transmission - Makes an object Diffuse but Transmissive(transluscent).  Affects only Disney BSDF</li>
  <li>Transmission Color - Doesnt do anything for now, used for volumetric disney bsdf(which is not yet implemented)</li>
  <li>Flatness - Affects Thin objects.  Affects only Disney BSDF</li>
  <li>Thin - Marks an object as thing so it can be better handled by the BSDF.  Affects only Disney BSDF, can be either 0 or 1</li>
 </ul>

# Known Bugs:
</br>
<ul>
  <li>Report any you find! There WILL be bugs, I just dont know what they are</li>
</ul>

# Huge thanks to these people for being sponsors/patrons:
<ul>
  <li>jhintringer</li>
</ul>

# Sample Images(Taken from various stages of development)

![](/Images/ArchRender1.png)
![](/Images/ArchRender2.png)
![](/Images/ArchRender3.png)
![](/Images/ArchRender4.png)
![](/Images/ArchViz3.png)
![](/Images/ArchViz1.png)
![](/Images/ArchViz2.png)
![](/Images/Loft1.png)
![](/Images/Portal.png)
![](/Images/Loft.png)
![](/Images/Minecraft.png)
![](/Images/Loft1.png)
![](/Images/Furry2.png)
![](/Images/Portal2.png)
![](/Images/NewBlender.png)
![](/Images/NewReSTIRV2.png)
![](/Images/NewSponza3V2.png)
![](/Images/NewSponza1V2.png)
![](/Images/NewSponza2V2.png)
![](/Images/Cornell.png)
![](/Images/SanMiguel1.png)
![](/Images/SanMiguel2.png)
![](/Images/MeshWithVoxels1V2.png)
![](/Images/Sponza1V2.png)
![](/Images/Voxels1V2.png)
![](/Images/BistroUpdated.png)
![](/Images/Room.png)
![](/Images/Lego.png)
![](/Images/RealisticSponza.png)
![](/Images/Blender1.png)
![](/Images/ReSTIR5.png)
![](/Images/ReSTIR2.png)
![](/Images/Restir3.png)
![](/Images/Voxels2.png)
![](/Images/DragoonInAScene.png)
![](/Images/PBRTest1.png)
![](/Images/NewFog.png)
![](/Images/Sunset1.png)
![](/Images/VolumeScene2.png)
![](/Images/VolumeScene1.png)
![](/Images/SpaceShip.png)


# Credits(will continue to expand when I have time)
Biggest thanks to Zuen(R.I.P. You will be missed) who helped me a huge amount with the new BVH and traversal, thanks to them I got to where I am now, and I am very thankful to them for their help and patience
</br>
https://github.com/jan-van-bergen
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

This project uses:
Crytek Sponza
CC BY 3.0
© 2010 Frank Meinl, Crytek

## Ideas/Reminders for later
Reduce memory consumption, work on adding back in Mitsuba scene support
