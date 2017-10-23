using UnityEngine;
using System.Collections;

public class C64EncodeScript: MonoBehaviour {

    public ComputeShader shader;
    public Texture2D sourceTex;

    Renderer rend;
    RenderTexture myRt;

    const int cellWidth = 8;
    const int cellHeight = 8;
    const int blockWidth = 40;
    const int blockHeight = 25;

    int stage = 0;
    const int num_stages = 3;
    string[] kernalStage = new string[num_stages]{
        "CSHiRes_OnlyRes",
        "CSHiRes_ResCol",
        "CSHiRes"
    };

    // Use this for initialization
    void Start () {
        myRt = new RenderTexture(blockWidth*cellWidth, blockHeight*cellHeight, 24);   // HiRes is 40x25
        myRt.enableRandomWrite = true;
        myRt.Create();
        
        rend = GetComponent<Renderer>();
        rend.enabled = true;

        ComputeStepFrame();
    }

    void OnDestroy()
    { 
        myRt.Release();
    }
    
    private void ComputeStepFrame()
    {
        int kernelHandle = shader.FindKernel(kernalStage[stage]);
        shader.SetTexture(kernelHandle, "SourceTex", sourceTex);
        shader.SetTexture(kernelHandle, "Result", myRt);
        shader.Dispatch(kernelHandle, blockWidth, blockHeight, 1);

        rend.material.SetTexture("_MainTex", myRt);

        stage = (stage + 1) % num_stages;
    }

   
    void Update () {

        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            ComputeStepFrame();
        }
        

    }
}
