using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PTShaderLoader : MonoBehaviour
{
    static public int ptKernel;							// kernel index, to reference the exact kernel to run and to set buffers for

    static public RenderTexture outputTexture;          // in this example we use 1024x1024 texture, and Dispatch() method will be called for each pixel
                                                        // compute buffers are a class to pass information into the compute shader
    static public ComputeBuffer areaRectBuffer;         // this buffer of four double numbers contains rect borders in fractal coordinates
    static public ComputeBuffer colorsBuffer;           // this buffer of 256 colors contains the colors to paint pixels depending on calculations
                                                        // compute buffers can read arrays, but it is not necessary, their size will be passed in the compute shader anyway, to allocate video memory
    static public double[] areaRectArray;               // this array will contain the numbers that will be passet into the buffer
    static public Color[] colorArray;                   // this array of COlor will contain the colors, that will be passed to compute shader by compute buffer
                                                        // render texture is a class to have a texture inside video memory and rapidly access it from cpu
                                                        // now some UI elements
    static public GameObject mainCanvas;                // just Unity canvas
    static public UnityEngine.UI.Image outputImage;     // this image's material will have a reference to the outputTexture, so it will show what we've written there by compute shader
                                                        // this is compute shaders instance
    static public ComputeShader _shader;				// we will need to link the code to this class, and then weĺl use it to actually run kernels from the code

    // Start is called before the first frame update
    void Start()
    {
        initTexture();
        initCanvas();
        initBuffers();
        initShader();
    }

    // Update is called once per frame
    void Update()
    {
        Scene scene = gameObject.scene;
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        List<Mesh> meshes = new List<Mesh>();

        // Get meshes
        foreach (GameObject gameObject in allObjects)
        {
            if (gameObject.activeInHierarchy)
            {
                Component component = gameObject.GetComponent("MeshFilter");
                MeshFilter meshFilter = (MeshFilter)component;
                Mesh mesh = meshFilter.mesh;

                if (mesh != null)
                    meshes.Add(mesh);
            }
        }

        // 
    }

    static void initTexture()
    {
        outputTexture = new RenderTexture(1024, 1024, 32);
        outputTexture.enableRandomWrite = true;                 // this is requred to work as compute shader side written texture
                                                                //outputTexture.memorylessMode = RenderTextureMemoryless.None;
                                                                //outputTexture.
        outputTexture.Create();                                 // yes, we need to run Create() to actually create the texture
        outputTexture.filterMode = FilterMode.Point;            // not necessary, I just wanted to have clean pixels
    }

    static public void initCanvas()
    {
        mainCanvas = GameObject.Find("canvas");
        mainCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
        mainCanvas.GetComponent<Canvas>().worldCamera = Camera.main;
        mainCanvas.GetComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        mainCanvas.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        mainCanvas.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight = 1.0f;
        outputImage = GameObject.Find("canvas/image").GetComponent<UnityEngine.UI.Image>();
        outputImage.material.mainTexture = outputTexture;
        outputImage.type = UnityEngine.UI.Image.Type.Simple;
        outputImage.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1080);
    }
    static void initBuffers()
    {
        
        areaRectBuffer = new ComputeBuffer(areaRectArray.Length, sizeof(double));       // amount of video memory for this buffer = array length * element size
        areaRectBuffer.SetData(areaRectArray);      // here we link array to the buffer
                                                    // here we create a color palette to visualize fractal (each pixel will be colored depending on how many iterations required to move a point outside the R = 2 circle)
        colorArray = new Color[256];
        int i = 0;
        while (i < colorArray.Length)
        {
            colorArray[i] = new Color(0, 0, 0, 1);
            if (i >= 0 && i < 128)
                colorArray[i] += new Color(0, 0, Mathf.PingPong(i * 4, 256) / 256, 1);
            if (i >= 64 && i < 192)
                colorArray[i] += new Color(0, Mathf.PingPong((i - 64) * 4, 256) / 256, 0, 1);
            if (i >= 128 && i < 256)
                colorArray[i] += new Color(Mathf.PingPong(i * 4, 256) / 256, 0, 0, 1);
            i++;
        }
        colorsBuffer = new ComputeBuffer(colorArray.Length, 4 * 4);     // Color size is four values of four bytes, so 4 * 4
        colorsBuffer.SetData(colorArray);           // again, we're setting color array to the buffer

        /*

		Note: if you want to use compute shader for general purpose calculations, you may want to get the results from the video memory
		Then after dispatch you would need to load data by calling:

		buffer.GetData(someArray)

		this will copy information from gpu side buffer to the cpu side memory
		But getting data from gpu memory is a very slow operation, and may be a bottleneck, so use with caution

		*/
    }
    static void initShader()
    {
        _shader = Resources.Load<ComputeShader>("RTVertexShader");           // here we link computer shader code file to the shader class
        ptKernel = _shader.FindKernel("DoTheThing");                       // we retrieve kernel index by name from the code
                                                                        // folowwing three lines allocate video memory and write there our data, kernel will then be able to use the data in calculations
        _shader.SetBuffer(ptKernel, "rect", areaRectBuffer);              // setting rect buffer
        _shader.SetBuffer(ptKernel, "colors", colorsBuffer);              // setting color palette buffer
        _shader.SetTexture(ptKernel, "outputTexture", outputTexture);        // setting texture

        /*

		Note:
		One compute shader file can have different kernels. They can share buffers, but you would have to call SetBuffer() for each kernel
		For example:
		_shader.SetBuffer(kernel1, "someBuffer", bufferToShare);
		_shader.SetBuffer(kernel2, "someBuffer", bufferToShare);
		
		*/
    }
}
