Notes:</br>
Currently working on:
<ul>
  <li>ReSTIR</li>
  <li>Need More Ideas</li>
</ul>
Currently needs to be done but havent started:
<ul>
    <li>Precomputed multiple scattering atmosphere(struggling a lot)</li>
    <li>Reduce Memory Spike on Start</li>
    <li>Improve Direct Light Sampling for meshes(look into ReSTIR or ReGIR?)</li>
    <li>Make Voxels able to be volumetric</li>
</ul>
Currently want to do but havent started:
<ul>
    <li>ReSTIR</li>
    <li>Need More Ideas</li>
</ul>

# Compute Shader Based Unity PathTracer
A passion projects that has been going on for awhile(about a year in unity, with my earliest version I can find being version 30(whereas I am now on version 256), which was made on 5-7-2021), finally at a place where I feel comfortable tentatively uploading it to Github for others to use
What is it?
Its my attempt at a Real-Time pathtracer built from scratch in Unity using Compute Shaders
## Features: 
<ul>
<li>Relatively fast Compute Shader based path tracing</li>
<li>Diffuse, Glossy(kinda), Dielectric(think of glass), Conductor(metal), Diffuse Transmission, Emissive</li>
<li>Ability to move, add, and remove objects during play</li>
<li>Ability to update material properties on the fly during play</li>
<li>SVGF Denoiser and Atrous Denoiser</li>
<li>BVH Building off of main thread for loading objects, allows objects to be spawned, and then built without lagging the main thread, and appearing when its done(All lag from spawning objects actually comes from remaking the texture atlas, lower res atlas's remove all lag, still investigating different ways of loading textures because of this)</li>
<li>Compressed Wide Bounding Volume Hierarchy as the Acceleration Structure (See Ylitie et al. 2017 below)</li>
<li>Albedo Textures(just apply them to the GameObjects material)</li>
<li>Next Event Estimation with Multiple Importance Sampling for Explicit Light Sampling</li>
<li>Objects are loaded as gameobject meshes(most common way of having meshes in unity)</li>
<li>Support for default unity lights which interact via NEE(Supports Directional, Point, and Spot lights)</li>
<li>Support for Normal maps and Emission masks</li>
<li>Global homogenous fog with adjustable density</li>
<li>Taking Full Resolution Screenshots</li>
<li>Bloom</li>
<li>PBR Texture Support</li>
<li>No specific GPU vendor needed(this will run on integrated graphics if you so wish it, aka no RTX cores)</li>
<li>Basic MagicaVoxel support</li>
<li>Ability to pathtrace voxels and triangle scenes at the same time seamlessly</li>
<li>Depth of Field</li>
<li>AutoExposure</li>
<li>Temporal Anti-Aliasing</li>
</ul>

[Ylitie et al](https://research.nvidia.com/sites/default/files/publications/ylitie2017hpg-paper.pdf)
</br>

If you have any questions, or suggestions, etc. let me know either through github issues or something else! I am always looking for more stuff to add, and more ways to make it more user friendly or appealing for others to use, and ways to improve this overall
You can contact me easiest through my discord: Pjbomb2#6129


## Notes:
Let me know if you use this for anything, I would be excited to see any use of this!  Just please give some credit somewhere if you use it, thank you!

# Requires Unity 2021 or higher
# Instructions:
## Required Settings Changes:
<ul>
  <li>Set the Color Space to Linear through Edit Tab(Top Left) -> Project Settings -> Player -> Other Settings -> Color Space, and change from Gamma to Linear</li>
  <li>Change the Graphics Api for Windows to DirectX12 through Edit Tab(Top Left) -> Project Settings -> Player -> Other Settings -> Untoggle "Auto Graphics API For Windows", then click the little + that appears, select "Direct3D12(Experimental)", and drag that to the top.  A restart of the editor is required</li>
  <li>Enable Unsafe Code(Its for memory management) through Edit -> Project Settings -> Player -> Other Settings -> "Allow 'unsafe' Code" (near the bottom)</li>
</ul>
</br>
## Additional Requirements:
<ul>
  <li>You need to make sure that all textures have Read/Write enabled in their import settings(click on a texture in the Project menu, look at its options in the inspector, turning on Read/Write, and clicking apply at the bottom).  I would also reccomend turning off MipMapping</li>
  <li>For Skinned Meshes, their index format needs to be set to 32 bits, and their mesh to Read/Write enabled.  This can be found by clicking on the imported fbx, going to it in the inspector, going to the Model tab, turning on Read/Write, changing the Index Format from Auto to 32 Bit, and clicking Apply at the bottom</li>
</ul>
</br>
## General Setup
<ul>
  <li>Download and import the UnityPackage provided</li>
  <li>For quick setup, make sure you have a Main Camera(there by unity default), just open the Pathtracer Settings menu under the Pathtracer tab, it will reorganize the Hierarchy a bit, and give everything their required scripts  If you dont want it to do this, and do it manually read below</li>
</ul>
</br>
## Setting it up manually
<ul>
  <li>You need to do the below before opening the Pathtracer settings window or else it will do its autosetup.  Additionally, you can study the provided DemoScene's hierarchy, as it provides basically all common variations of objects and their relations</li>
  <li>First, you need a main camera, which unity automatically provides.  This camera will need attatched to it the RayTracingMaster script, under Assets/Resources, and should have the FlyCamera Script attatched(Under Assets/Resources/Utility)</li>
  <li>Next, you need a GameObject called Scene, and this will be the gameobject that all others except the camera will be parented to(It will be the root object). This gameobject will need the AssetManager script attatched to it, found under Assets/Resources/BVH</li>
  <li>Next, all objects you want to trace will need a parent.  This parent can either be themselves for individual objects, or will need to be nested under another gameobject to be grouped(for increasing performance, group wherever you can).  These Parents need to have a ParentObject attatched, as this defines groups, with its children all being seen as one group(Located under Assets/Resources/BVH)</li>
  <li>Finally, all meshes and skinned meshes you want to trace need a RayTracingObject script attatched to them, as this defines what should be and should not be pathtraced(Located under Assets/Resources)</li>
  <li>One last note, if your using Unity lights as well, each one of these needs a RayTracingLights script attatched to it(Located under Assets/Resources)</li>
</ul>
## General Use/Notes
<ul>
  <li>Objects can be added and removed at will simply by toggling them on/off in the hierarchy(dont click them if they are complex objects), but they will take time to appear</li>
  <li>If you change the emissiveness of an object, you need to dissable and re-enable its parent(basically reloading it) if you want to take advantage of NEE correctly sampling it</li>
  <li>If you use normal maps, they need to be in unity normal map format, and emissive masks need to have at least 1 component be red as thats what I use to determin what parsts should be emissive(it will use the albedo tex as surface color)</li>
  <li>To set up PBR, all textures go into their proper names, but Roughness goes into the Occlusion texture(Since path tracing calculates ambient occlusion by default, this texture is not normally needed, and there being no proper place for a Roughness texture in the default material, I have decided this was a good compromise)</li>
</ul>
## MagicaVoxel Usage
<ul>
  <li>Before anything, the files need to be in .txt format, to do this, go to the file in file explorer, and rename the .vox to .txt and change the format</li>
  <li>Firstly, you still need to have a gameobject under the scene gameobject to attatch your voxel model to</li>
  <li>Second, you need to attatch a VoxelObject to that gameobject(Located under Assets->Resources->BVH->VoxelObject)</li>
  <li>Next you need to attatch the voxel model to this script, by dragging your voxel model asset in the project tab to the VoxelRef space in the VoxelObject script</li>
  <li>That should be it, it will get grouped into the building along with meshes, and having at least 1 voxel object in the scene will turn on its inclusion.  Removing or turning off all voxel related gameobjects will turn it back off</li>
    
    
## Controls:
Camera Controls: WASD, Mouse, and press T to freeze the camera
</br>
## Editor Window Guide
BVH Options Description - 
<ul>
  <li>Build Aggregated BVH - Allows you to pre-build objects BVH's before running so you dont have to wait every time you go into play mode for it to build.  To know when its done building, the Object Parent Name will appear in the console telling you its complete(and thus wont need to be rebuilt every time you hit play)</li>
  <li>Clear Parent Data - Clears the data stored in parent gameobjects, allowing you to actually click them without crashing or lagging(but will then require the BVH to be rebuilt)</li>
  <li>Sun Position - Affects the sun position from the Precompute Atmospheric Scattering, and if you have a directional light, it will affect this as well(for NEE)</li>
  <li>Max Bounces - Sets the maximum number of bounces a ray can achieve</li>
  <li>Use Russian Roulette - Highly reccomended to leave this on, kills rays that may not contribute much early, and thus greatly increases performance</li>
  <li>Enable Object Moving - Allows objects to be moved during play, and allows for added objects to spawn in when they are done building</li>
  <li>Allow Image Accumulation - Allows the image to accumulate while the camera is not moving</li>
  <li>Use Next Event Estimation - Enables shadow rays, NEE, and MIS for direct light sampling</li>
  <li>Allow Volumetrics - Turns on pathtracing of global volumetric homogenous fog</li>
  <li>(If Allow Volumetrics is on) Volume Density - Adjusts density of the global fog</li>  
  <li>Allow Mesh Skinning - Turns on the ability for skinned meshes to be animated or deformed with respect to their armeture</li>
  <li>Allow Bloom - Turns on or off Bloom</li>
  <li>Use DoF - Turns on or off Depth of Field, and its associated settings</li>
  <li>Use Auto Exposure - Turns on or off Auto Exposure(impacts a lot more than I thought it would)</li>
  <li>Use SVGF Denoiser - Turns on the SVGF denoiser</li>
  <li>(If SVGF Denosier is on)Atrous Kernel Size - The amount of times the SVGF denoiser runs through the Atrous kernel</li>
  <li>Use Atrous Denoiser - Turns on the Atrous denoiser(can be combined with SVGF)</li>
  <li>Current Samples - Shows how many samples have currently been accumulated</li>
  <li>Take Screenshot - Takes a screenshot at game view resolution and saves it to Assets/ScreenShots(You need to create this folder)</li>
  <li>QuickStart - Will attempt to automatially assign RayTracingObjects and ParentObjects to all child under the GameObject named "Scene" with an AssetManager attatched</li>
  </ul>
  
 ## Materials
 <ul>
  <li>Emission - Pretty self explanatory, the higher it is, the bright the object is(and the higher chance it will be sampled for NEE)</li>
  <li>Roughness - Applys to Conductors and Dielectrics - Higher roughness makes objects more rough, but in Diffuse Transmission it basically represents how clear the material is</li>
  <li>Eta - For Conductors it changes the color of reflected light, but inverted, but for Dielectrics, only the x component is used, and that X component is the Dielectrics IOR(Index of Refraction, with 1 being air), and for SSS(see below), it is used the same as for conductors</li>
  <li>Base Color - So this will be automatically set to whatever the material of the objects color is, and it will also be overridden by textures, but its there so you can manually change it, works for all material types</li>
  <li>Mat Type - 0 is diffuse(if you comment out the UsePretty in the RayTracingShader.compute, otherwise this is glossy) - 1 is Conductor(Metallic) - 2 is Dielectric(so transparent/glassy materials) - 3 is glossy - 4 is mask(but is not used yet due to performance things) - 5 is a "Volumetric" material(not very good yet though) - 6 is hacked together "SubSurface Scattering"(SSS) - and 7 is Diffuse Transmission, with roughness defining how close to the origional direction the new ray will go</li>
</ul>
  
# Sample Images(Taken from various stages of development)

![](/Images/ReSTIR1.png)
![](/Images/ReSTIR2.png)
![](/Images/SSS1.png)
![](/Images/AutoExpose1.png)
![](/Images/DoF2.png)
![](/Images/VoxelsWithMesh2.png)
![](/Images/VoxelsWithMesh1.png)
![](/Images/Voxels2.png)
![](/Images/DragoonInAScene.png)
![](/Images/Voxels1.png)
![](/Images/PBRTest1.png)
![](/Images/NewFog.png)
![](/Images/Sunset1.png)
![](/Images/VolumeScene2.png)
![](/Images/VolumeScene1.png)
![](/Images/FurryGif1.gif)
![](/Images/FurryRender.png)
![](/Images/NewSponza3.png)
![](/Images/NewSponza2.png)
![](/Images/NewSponza1.png)
![](/Images/SponzaLion.png)
![](/Images/Sponza-Diffuse.png)
![](/Images/Another-Sponza.png)
![](/Images/Bistro-Chair.png)
![](/Images/Bistro-Glasses.png)
![](/Images/Bistro-Inside.png)
![](/Images/Early-Atrous.png)
![](/Images/Early-Mitsuba-Parser.png)
![](/Images/Early-Tests.png)
![](/Images/Lensing-Example.png)
![](/Images/Car.png)
![](/Images/KitchenScene.png)
![](/Images/living-room.png)
![](/Images/PrettyScene.png)
![](/Images/Sponza.png)
![](/Images/SpaceShip.png)
![](/Images/Material-Testing.png)


# Credits(will continue to expand when I have time)
Biggest thanks to Zuen who helped me a huge amount with the new BVH and traversal, thanks to them I got to where I am now, and I am very thankful to them for their help and patience
</br>
https://github.com/jan-van-bergen
</br></br>
Scenes From:
<ul>
  <li>https://benedikt-bitterli.me/resources/</li>
  <li>https://casual-effects.com/data/</li>
  <li>https://www.intel.com/content/www/us/en/developer/topic-technology/graphics-research/samples.html</li>
  <li>https://sketchfab.com/3d-models/tallers-de-la-fundacio-llorenc-artigas-22-5224b721b0854998a1808fcea3cff924</li>
  <li>https://sketchfab.com/3d-models/dae-bilora-bella-46-camera-game-ready-asset-eeb9d9f0627f4783b5d16a8732f0d1a4</li>
  <li>https://www.unrealengine.com/marketplace/en-US/product/victorian-train-station-and-railroad-modular-set</li>
</ul>



## Ideas/Reminders for later
ReGIR, Find an alternative to atlas's, Reduce memory consumption, work on adding back in Mitsuba scene support
