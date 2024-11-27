using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
public class RenderHandle2 : MonoBehaviour
{

    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
         // TrueTrace.AssetManager.Assets.InstanceData.RenderInstances2();
         // Graphics.Blit(source, destination);
    }
}
