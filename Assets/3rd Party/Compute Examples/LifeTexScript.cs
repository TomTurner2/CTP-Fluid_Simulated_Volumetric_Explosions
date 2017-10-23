using UnityEngine;
using System.Collections;

public class LifeTexScript: MonoBehaviour {

    public ComputeShader shader;
    public float StepTime = 0.2f;
    public int TexResolution = 256;

    Renderer rend;
    RenderTexture[] myRt;
    int currTex = 0;
    const int numTex = 2;
    bool bDoUpdate = false;
    float lastUpdate = 0.0f;

    // Use this for initialization
    void Start () {
        myRt = new RenderTexture[numTex];
        for (int i = 0; i < numTex; i++)
        {
            myRt[i] = new RenderTexture(TexResolution, TexResolution, 24);
            myRt[i].enableRandomWrite = true;
            myRt[i].Create();
        }

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        ResetComputeSim();
    }

    private void ResetComputeSim()
    {
        int kernelHandle = shader.FindKernel("CSRandom");
        shader.SetInt("RandOffset", (int)(Time.timeSinceLevelLoad * 100));
        shader.SetTexture(kernelHandle, "Result", myRt[currTex]);
        shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);
        rend.material.SetTexture("_MainTex", myRt[currTex]); 
    }


    private void ComputeStepFrame()
    {
        int prevTex = currTex;
        currTex = (currTex + 1) % numTex;

        int kernelHandle = shader.FindKernel("CSMain");

        shader.SetTexture(kernelHandle, "Prev", myRt[prevTex]);
        shader.SetInt("RandOffset", (int)(Time.timeSinceLevelLoad * 100));
        shader.SetInt("TexSize", TexResolution - 1);
        shader.SetTexture(kernelHandle, "Result", myRt[currTex]);
        shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);

        rend.material.SetTexture("_MainTex", myRt[currTex]);
        lastUpdate -= StepTime;
    }

   
    void Update () {
        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            bDoUpdate = !bDoUpdate;
            lastUpdate = 0.0f;
        }

        if(bDoUpdate && lastUpdate > StepTime)
            ComputeStepFrame();
        lastUpdate += Time.deltaTime;

        if (Input.GetKeyUp(KeyCode.W))
            ResetComputeSim();


    }
}
