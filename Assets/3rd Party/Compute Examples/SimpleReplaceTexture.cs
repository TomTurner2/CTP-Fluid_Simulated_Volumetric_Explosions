using UnityEngine;
using System.Collections;

public class SimpleReplaceTexture : MonoBehaviour {

public ComputeShader shader;
public int TexResolution = 256;

  Renderer rend;
  RenderTexture myRt;

  // Use this for initialization
  void Start() {
    myRt = new RenderTexture(TexResolution, TexResolution, 24);
    myRt.enableRandomWrite = true;
    myRt.Create();

    rend = GetComponent<Renderer>();
    rend.enabled = true;

    UpdateTextureFromCompute();
  }

private void UpdateTextureFromCompute() {
    int kernelHandle = shader.FindKernel("CSMain");
    shader.SetInt("RandOffset", (int)(Time.timeSinceLevelLoad * 100));

    shader.SetTexture(kernelHandle, "Result", myRt);
    shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);

    rend.material.SetTexture("_MainTex", myRt);
  }

  void Update() {
    if (Input.GetKeyUp(KeyCode.Alpha1))
      UpdateTextureFromCompute();
  }
}
