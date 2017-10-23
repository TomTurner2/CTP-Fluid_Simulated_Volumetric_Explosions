using UnityEngine;
using System.Collections;

public class ParticleTexScript: MonoBehaviour {

    public ComputeShader shader;
    public int TexResolution = 256;
    public int NumParticles = 200;
    public Vector4 RepelPoint;

    Renderer rend;
    RenderTexture myRt;
    bool bDoUpdate = false;

    struct MyParticle
    {
        public Vector2 pos;
        public Vector2 dir;
        public Vector3 col;
        public float aliveTime;
    }

    ComputeBuffer particleBuffer;

    // Use this for initialization
    void Start () {
        myRt = new RenderTexture(TexResolution, TexResolution, 24);
        myRt.enableRandomWrite = true;
        myRt.Create();

        // Round particles UP to nearest number
        if((NumParticles % 10) > 0)
        {
            NumParticles += 10 - (NumParticles % 10);
        }        

        particleBuffer = new ComputeBuffer(NumParticles, sizeof(float) * 8, ComputeBufferType.Default);

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        ResetComputeSim();
    }

    void OnDestroy()
    { 
        myRt.Release();
        particleBuffer.Release();
    }

    private void ResetComputeSim()
    {
        MyParticle[] pArray = new MyParticle[NumParticles];
        //particleBuffer.GetData(pArray);

        for (int i=0; i<NumParticles; i++)
        {
            MyParticle p = new MyParticle();
            p.pos = new Vector2(Random.Range(10, TexResolution - 10), Random.Range(10, TexResolution - 10));
            p.dir = new Vector2(Random.Range(-50, +50), Random.Range(-50,+50));
            Color c = Random.ColorHSV(0, 1.0f, 0.5f, 1.0f, 0.5f, 1.0f);
            p.col = new Vector4(c.r, c.g, c.b, 0.0f);
            pArray[i] = p;
        }

        particleBuffer.SetData(pArray);
        ComputeStepFrame();
    }


    private void ComputeStepFrame()
    {
        shader.SetInt("RandOffset", (int)(Time.timeSinceLevelLoad * 100));
        shader.SetInt("TexSize", TexResolution - 1);
        shader.SetFloat("DeltaTime", Time.deltaTime);
        shader.SetVector("RepelPoint", RepelPoint);

        int kernelHandle = shader.FindKernel("CSRenderWipe");
        shader.SetTexture(kernelHandle, "Result", myRt);
        shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);

        kernelHandle = shader.FindKernel("CSMain");
        shader.SetTexture(kernelHandle, "Result", myRt);
        shader.SetBuffer(kernelHandle, "PartBuffer", particleBuffer);
        shader.Dispatch(kernelHandle, NumParticles / 10, 1, 1);

        rend.material.SetTexture("_MainTex", myRt); 
    }

   
    void Update () {
        RepelPoint = Vector4.zero;

        if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
        {
            RaycastHit hit;
            Ray mr = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mr, out hit))
            {
                RepelPoint = hit.textureCoord * TexResolution;
                RepelPoint.z = 50.0f;
                RepelPoint.w = Input.GetMouseButton(0) ? +100.0f:-100.0f;
            }
            else
            {
                RepelPoint = Vector4.zero;
            }
        }

        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            bDoUpdate = !bDoUpdate;
        }

        if(bDoUpdate)
            ComputeStepFrame();

        if (Input.GetKeyUp(KeyCode.E))
            ResetComputeSim();


    }
}
