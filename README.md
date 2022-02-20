# Compute Shader Based Fast Unity PathTracer
A passion projects that has been going on for awhile, finally at a place where I feel comfortable tentatively uploading it to Github for others to use
What is it?
Its my attempt at a Real Time pathtracer built from scratch in Unity using Compute Shaders
## Features: 
<ul>
  
<li>Relatively fast Compute Shader based path tracing</li>
<li>Diffuse, Glossy(sorta), Dielectric, Conductor</li>
<li>Loose but technically there Mitsuba XML file loader</li>
<li>Ability to move objects while running</li>
<li>Realtime ability to update material properties</li>
<li>Basic Atrous denoiser</li>
  <li>Multithreaded BVH Building for many meshes at once(not for single meshes)</li>
<li>Compressed Wide Bounding Volume Hierarchy as the Acceleration Structure</li>
</ul>

If you have any questions, or suggestions, etc. let me know! I am always looking for more stuff to add, and more ways to make it more user friendly or appealing for others to use

## Instructions:
You can either use the UnityPackage which includes a small demo scene with the stuff you need to add already set up, or the code raw, but I would reccomend the package as it already comes with a scene with the camera set up.
</br>
For each mesh that you want to add to the render, you need to add a RayTracingObject script to it in the inspector
</br>
Whenever you add or remove an item from the list of objects to render(simply by activating it, deactivating it, or adding or removing the RayTracingObject script), you need to rebuild the acceleration structure
</br>
To do this, you need to open the EditorWindow.  Basically, up at the top of the Unity window, there will be a tab called Window.  Click on that tab, and click on the item in the dropdown called "BVH Options"
</br>
BVH Options Description
<ul>
  <li>Construct BVH's - Normal construction of acceleration structure, one click and wait for the Total Construction Time message to appear in console, then your ready to play</li>
  <li>Update TLAS - In case you need to manually update the Top Level Acceleration Structure</li>
  <li>Build Aggregated BVH - Will aggregate all non static meshes(which you determine by putting a 1 or a 0 in the Dynamic section of the RayTracingObject) into one mesh, then build the BVH for them.  This gives a decent enough performance bump for me to add it</li>
  <li>Update Materials - In case you need to manually update the materials</li>
  <li>Setup - Currently used for the XML Parser, will add a description for how to use that later</li>
  <li>Max Bounces - Sets the maximum number of bounces a ray can achieve</li>
  <li>Use Russian Roulette - Highly reccomended to leave this on, kills rays that may not contribute much early, and thus greatly increases performance</li>
  <li>Use Atrous Denoiser - Enables or dissables the Atrous denoiser, the settings below it are values to play with until you get a desired result</li>
  
  </ul>
  
  

![](/Images/Sponza-Diffuse.png)
![](/Images/Another-Sponza.png)
![](/Images/Bistro-Chair.png)
![](/Images/Bistro-Glasses.png)
![](/Images/Bistro-Inside.png)
![](/Images/Early-Atrous.png)
![](/Images/Early-Mitsuba-Parser.png)
![](/Images/Early-Tests.png)
![](/Images/Lensing-Example.png)
![](/Images/Material-Testing.png)


# Credits(will continue to expand when I have time)
Biggest thanks to Zuen who helped me a huge amount with the new BVH and traversal, thanks to them I got to where I am now, and I am very thankful to them for their help and patience
</br>
Their github is at Jan Van Bergen



</br></br>
# REMINDER FOR SELF
Need to add a way to turn off the automatic update of the TLAS thats easier than going into code and commenting out 3 lines in RayTracingMaster(the UpdateTLAS and the 2 lines below it)
