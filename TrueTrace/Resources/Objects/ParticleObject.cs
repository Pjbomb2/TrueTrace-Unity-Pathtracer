using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;


public class ParticleObject : MonoBehaviour
{
    ParticleSystem PartSystem;

    [System.Serializable]
    public struct SphereData {
        public uint Offset;
        public Vector3 Center;
        public float Radius;
    }
    public SphereData[] Spheres;


    public void UpdateParticle() {
        PartSystem = this.gameObject.GetComponent<ParticleSystem>();
        ParticleSystem.Particle[] Particles = new ParticleSystem.Particle[PartSystem.maxParticles];
        PartSystem.GetParticles(Particles);
        int SphereCount = PartSystem.maxParticles;
        int SphereLayer = Mathf.CeilToInt(SphereCount / 2.0f);
        SphereCount += SphereLayer;
        while(SphereLayer > 1) {
            SphereCount += SphereLayer;
            SphereLayer = Mathf.CeilToInt(SphereLayer / 2.0f);
        }
        Spheres = new SphereData[SphereCount + 1];
        AABB aabb = new AABB();
        aabb.init();
        for(int i = 0; i < PartSystem.particleCount; i++) {
            ParticleSystem.Particle Part = Particles[i];
            // if(Part.position.x == this.transform.position.x || Part.position.y == this.transform.position.y || Part.position.z == this.transform.position.z) continue;
            AABB tempAABB = new AABB();
            tempAABB.BBMax = Part.GetCurrentSize3D(PartSystem) + Part.position;
            tempAABB.BBMin = Part.position - Part.GetCurrentSize3D(PartSystem);
            aabb.Extend(ref tempAABB);
        }
        Spheres[0].Center = (aabb.BBMax + aabb.BBMin) / 2.0f;
        Vector3 Extents = (aabb.BBMax - aabb.BBMin) / 1.5f;
        Spheres[0].Radius = Mathf.Max(Mathf.Max(Extents.x, Extents.y), Extents.z);

    }
    public void Update() {
        UpdateParticle();
    }

    void OnDrawGizmos() {
        Update();
        foreach(var S in Spheres) {
            Gizmos.DrawWireSphere(S.Center, S.Radius);
        }
    }
}
