To use the SDF Slicer:
Add a sphere object to the hierarchy and put it outside of TrueTrace's "Scene" Gameobject
Add the "TTSDFHandler" Component to the sphere

DoBackfacing allows rays to travel infinitely after exiting the last SDF(hard to explain)
Max Step Count and Min Step Size are for tuning the "resolution" of the slices(since it uses sphere-tracing/ray-marching, intersections are more approximations)