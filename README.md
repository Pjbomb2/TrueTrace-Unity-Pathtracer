Notes:</br>
Currently working on:
<ul>
  <li>Precomputed multiple scattering atmosphere(struggling a lot)</li>
  <li>GOOD Volumetrics</li>
</ul>
Currently needs to be done but havent started:
<ul>
    <li>Find a way to allow for objects to be added and removed easily during running(I have this technically done but its way too janky feeling for me to actually incorperate it)</li>
</ul>
Currently want to do but havent started:
<ul>
    <li>Add native support for voxels(havent started as I dont know a good way to structure it or what acceleration structure would be good, or a good data format, that supports preferably both solid and volumetric voxels)</li>
</ul>

# Compute Shader Based Fast Unity PathTracer
A passion projects that has been going on for awhile, finally at a place where I feel comfortable tentatively uploading it to Github for others to use
What is it?
Its my attempt at a Real Time pathtracer built from scratch in Unity using Compute Shaders
## Features: 
<ul>
  
<li>Relatively fast Compute Shader based path tracing</li>
<li>Diffuse, Glossy(sorta), Dielectric, Conductor, Diffuse Transmission</li>
<li>Loose but technically there Mitsuba XML file loader</li>
<li>Ability to move objects during play</li>
<li>Ability to update material properties on the fly during play</li>
<li>Basic Atrous denoiser</li>
<li>Multithreaded BVH Building for many meshes at once(not for single meshes)</li>
<li>Compressed Wide Bounding Volume Hierarchy as the Acceleration Structure (See Ylitie et al. 2017 below)</li>
<li>Textures(just apply them to the GameObjects material)</li>
<li>Next Event Estimation with Multiple Importance Sampling for Explicit Light Sampling</li>
</ul>

[Ylitie et al](https://research.nvidia.com/sites/default/files/publications/ylitie2017hpg-paper.pdf)
</br>

If you have any questions, or suggestions, etc. let me know either through github issues or something else! I am always looking for more stuff to add, and more ways to make it more user friendly or appealing for others to use, and ways to improve this overall


## Notes:
Let me know if you use this for anything, I would be excited to see any use of this!  Just please give some credit somewhere if you use it, thank you!

## Instructions:
So first thing, you need to set the color space to Linear.  To do this, you need to go to edit on the top right, Project Settings -> Player -> Other Settings -> Color Space, and set that to linear
</br>
Aside from this, you need to make sure all textures you use are Read/Write enabled(do this by selecting all the textures you will be using, then on the right click Read/Write enabled
</br>
Also preferably set the Graphics API for Windows to DirectX12, and put it at the top of the rendering API's.  This is not require but it gives a large performance increase
</br></br>
You can either use the UnityPackage which includes a small demo scene with the stuff you need to add already set up, or the code raw, but I would reccomend the package as it already comes with a scene with the camera set up.
</br></br>
Camera Controls: WASD, Mouse, and press T to freeze the camera
</br></br>
For each mesh that you want to add to the render, you need to add a RayTracingObject script to it in the inspector
</br></br>
Whenever you add or remove an item from the list of objects to render(simply by activating it, deactivating it, or adding or removing the RayTracingObject script), you need to rebuild the acceleration structure
</br></br>
To do this, you need to open the EditorWindow.  Basically, up at the top of the Unity window, there will be a tab called Window.  Click on that tab, and click on the item in the dropdown called "BVH Options"
</br></br>
BVH Options Description - 
<ul>
  <li>Build Aggregated BVH - Will aggregate all meshes into their defined groupings(which you determine by putting a number in the Object Group section of the RayTracingObject) into larger single meshes, then builds the new BVHs for them.</li>
  <li>Max Bounces - Sets the maximum number of bounces a ray can achieve</li>
  <li>Use Russian Roulette - Highly reccomended to leave this on, kills rays that may not contribute much early, and thus greatly increases performance</li>
  <li>Use Atrous Denoiser - Enables or dissables the Atrous denoiser, the settings below it are values to play with until you get a desired result</li>
  <li>Allow Image Accumulation - Allows the image to accumulate while the camera is not moving</li>
  <li>Enable Object Moving - Recomputes the TLAS every frame, allowing objects to moved while running</li>
  <li>Load Xml - replaces the way that XML's are loaded allowing their folders to be placed in the assets folder in a folder called "Models".  Pressing that will give you a list of possible Mitsuba scenes to load(again, only sees ones that are in the assets folder, inside another folder called "Models").  Clicking on one of the options will load the mesh structure and associated materials to the hierarchy(yay no more manually needing to do that) under the Gameobject named ParentXML(see DemoScene for that) (I will replace this paragraph soon... or when I get around to it I suppose)</li>
  </ul>
  
 ## Materials
 <ul>
  <li>Emission - Pretty self explanatory, the higher it is, the bright the object is</li>
  <li>Roughness - Applys to Conductors and Dielectrics - Higher roughness makes objects more rough</li>
  <li>Eta - idk what this does really but a few things to note - For Conductors it just adds to the material definition, but for Dielectrics, only the x component is used, and that X component is the Dielectrics IOR</li>
  <li>Base Color - So this will be automatically set to whatever the material of the objects color is, and it will also be overridden by textures, but its there so you can manually change it, works for all material types</li>
  <li>Mat Type - 0 is diffuse(if you comment out the UsePretty in the RayTracingShader.compute, otherwise this is glossy) - 1 is Conductor(Metallic) - 2 is Dielectric(so transparent/glassy materials) - 3 is glossy - 4 is mask(but is not used yet technically) - 5 is a "Volumetric" material(not very good yet though) - and 6 is Diffuse Transmission, with roughness defining how close to the origional direction the new ray will go</li>
  <li>IsParent - Allows you to explicitly define which object in a group should be the parent, allowing for animations to be done with less hassel</li>
</ul>
  
# Sample Images(Taken from various stages of development)

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
  <li>https://casual-effects.com/data/</li
</ul>



## Ideas/Reminders for later
    See if any performance beenfit can be gotten by allowing mesh instancing, so for meshes that are the exact same(possibly have the user define this, or see if I can have it be automatic), simple put their Root node in the list, but only have one reference, reducing memory consumption(how much of an impact would this actually have though?)
