Notes:</br>
Currently working on:
<ul>
  <li>None</li>
</ul>
Currently needs to be done but havent started:
<ul>
    <li>Precomputed multiple scattering atmosphere(struggling a lot)</li>
    <li>Reduce Memory Spike on Start</li>
    <li>Improve Direct Light Sampling for meshes(look into ReSTIR or ReGIR?)</li>
</ul>
Currently want to do but havent started:
<ul>
    <li>Add native support for voxels(havent started as I dont know a good way to structure it or what acceleration structure would be good, or a good data format, that supports preferably both solid and volumetric voxels)</li>
</ul>

# Compute Shader Based Unity PathTracer
A passion projects that has been going on for awhile, finally at a place where I feel comfortable tentatively uploading it to Github for others to use
What is it?
Its my attempt at a Real-Time pathtracer built from scratch in Unity using Compute Shaders
## Features: 
<ul>
<li>Relatively fast Compute Shader based path tracing</li>
<li>Diffuse, Glossy(kinda), Dielectric(think of glass), Conductor(metal), Diffuse Transmission</li>
<li>Ability to move, add, and remove objects during play</li>
<li>Ability to update material properties on the fly during play</li>
<li>SVGF Denoiser(not sure its working 100% correctly)</li>
<li>BVH Building off of main thread for loading objects, allows objects to be spawned, and then built without lagging the main thread, and appearing when its done</li>
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
</ul>

[Ylitie et al](https://research.nvidia.com/sites/default/files/publications/ylitie2017hpg-paper.pdf)
</br>

If you have any questions, or suggestions, etc. let me know either through github issues or something else! I am always looking for more stuff to add, and more ways to make it more user friendly or appealing for others to use, and ways to improve this overall
You can contact me easiest through my discord: Pjbomb2#6129


## Notes:
Let me know if you use this for anything, I would be excited to see any use of this!  Just please give some credit somewhere if you use it, thank you!

## Instructions:
REQUIRES UNITY 2021 OR HIGHER
</br>
So first thing, you need to set the color space to Linear.  To do this, you need to go to edit on the top right, Project Settings -> Player -> Other Settings -> Color Space, and set that to linear
</br>
Next set the Graphics API for Windows to DirectX12, and put it at the top of the rendering API's.  This is not require but it gives a large performance increase(Edit tab on top left -> Project Settings -> Player -> Other Settings -> untoggle "Auto Graphics API for Windows" -> Click the new + button that appears -> click Direct3D12 -> drag the new Direct3D12 (Experimental) to the top of the list)
</br>
Next, you need to enable Unsafe Code(Despite its name, I only use it to explicitely define array sizes in a struct, MASSIVELY reduces memory use).  To do this, go to Edit -> Project Settings -> Player -> Other Settings -> "Allow 'unsafe' Code" (near the bottom)
</br>
Finally you need to make sure all textures you use are Read/Write enabled(do this by selecting all the textures you will be using, then on the right click Read/Write enabled
</br></br>
You can either use the UnityPackage which includes a small demo scene with the stuff you need to add already set up, or the code raw, but I would reccomend the package as it already comes with a scene with the camera set up.
</br></br>
For Skinnedmeshes, you need to set their index format to 32 bit, do this by clicking the skinned mesh in the project bar(contains everything in the project), then find IndexFormat in the inspector(usually set to auto, near the bottom of the Model tab), and set it to 32 bits
</br></br>
Camera Controls: WASD, Mouse, and press T to freeze the camera
</br></br>
## Setting up your scene(will make a video for this eventually)
First, I highly reccomend you look at the demo scene provided by the package for this, but heres how the hierarchy needs to be configured
</br></br>
First, you need a main camera, and attatched to this main camera, you need to attach the RayTracingMaster script located in Resources(and the Fly Camera script located in Resources -> Utility)
</br></br>
Next, all default unity lights you want to use need a RayTracingLights script(located in Resources).  These can go anywhere in the hierarchy
</br></br>
Next, you need a gameobject, preferably at the top layer of the hierarchy(so it itself is not contained as a child to any other gameobject) called Scene, and give this the AssetManager script(located in Resources -> BVH).  All mesh objects and such should have this as a top parent
</br></br>
Next, you need to create groups of gameobjects(so aka, any objects that should have a single BVH built over(so objects that are either static or move all at once together)).  These are defined as gameobjects with the ParentObject script attatched to them, which is located in (Resources -> BVH).  These objects will not have meshes, but will have gameobjects with meshes as their children.
</br></br>
Finally, all gameobjects that are meshes that you wish to trace will have to be a child of one of the gameobjects with a parent script.  You attach the RayTracingObject script to each of these(located in Resources).
</br></br>
First note, GameObjects with the parent script attached can have children that are themselves also gameobjects with ParentObject scripts, allowing for different groups to be still nested(and thus can inherit transforms).
</br></br>
Second note, You can now add, remove, and move objects at will during run, they will just take some time to appear.  Nothing special should need to be done to do this, just do it how you normally would(if this ends up not being this easy, let me know so I can take it into account)
</br></br>
Third note, if you change the emissiveness of an object, you need to dissable and re-enable its parent(basically reloading it) if you want to take advantage of NEE correctly sampling it
</br></br>
One last thing, in the event that you use normal maps, they need to be in unity normal map format, and emissive masks for now need to have at least 1 component be red as thats what I use to determin what parsts should be emissive(it will use the albedo tex as surface color)
</br></br>
</br></br>
Finally, to set up PBR, all textures go into their proper names, but Roughness goes into the Occlusion texture(Since path tracing calculates ambient occlusion by default, this texture is not normally needed, and there being no proper place for a Roughness texture in the default material, I have decided this was a good compromise)
</br></br>
</br></br>
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
  <li>Use SVGF Denoiser - Turns on the SVGF denoiser</li>
  <li>(If SVGF Denosier is on)Atrous Kernel Size - The amount of times the SVGF denoiser runs through the Atrous kernel</li>
  <li>Use Atrous Denoiser - Turns on the Atrous denoiser(can be combined with SVGF)</li>
  <li>Current Samples - Shows how many samples have currently been accumulated</li>
  <li>Take Screenshot - Takes a screenshot at game view resolution and saves it to Assets/ScreenShots(You need to create this folder)</li>
  </ul>
  
 ## Materials
 <ul>
  <li>Emission - Pretty self explanatory, the higher it is, the bright the object is(and the higher chance it will be sampled for NEE)</li>
  <li>Roughness - Applys to Conductors and Dielectrics - Higher roughness makes objects more rough</li>
  <li>Eta - idk what this does really but a few things to note - For Conductors it just adds to the material definition, but for Dielectrics, only the x component is used, and that X component is the Dielectrics IOR</li>
  <li>Base Color - So this will be automatically set to whatever the material of the objects color is, and it will also be overridden by textures, but its there so you can manually change it, works for all material types</li>
  <li>Mat Type - 0 is diffuse(if you comment out the UsePretty in the RayTracingShader.compute, otherwise this is glossy) - 1 is Conductor(Metallic) - 2 is Dielectric(so transparent/glassy materials) - 3 is glossy - 4 is mask(but is not used yet due to performance things) - 5 is a "Volumetric" material(not very good yet though) - and 6 is Diffuse Transmission, with roughness defining how close to the origional direction the new ray will go</li>
  <li>IsParent - Allows you to explicitly define which object in a group should be the parent, allowing for animations to be done with less hassel</li>
</ul>
  
# Sample Images(Taken from various stages of development)

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
</ul>



## Ideas/Reminders for later
ReSTIR, ReGIR, Optimized Atlas Creation, Reduce memory consumption, add support for Roughness/Metallic atlas, work on adding back in Mitsuba scene support
