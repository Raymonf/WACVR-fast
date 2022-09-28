using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security.Principal;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    public List<GameObject> Lights = new List<GameObject>();
    List<Material> Materials = new List<Material>();
    public static bool useIPC = false;
    public static bool useIPC_Config = true;
    static Texture2D RGBColor2D;

    public static MemoryMappedFile sharedBuffer;
    public static MemoryMappedViewAccessor sharedBufferAccessor;

    private byte[] buffer = new byte[1920];
    private IEnumerator[] coroutines = new IEnumerator[240];
    public float FadeDuration = 0.5f;

    private void Start() 
    {
        if (JsonConfiguration.HasKey("useIPC")) 
            useIPC_Config = JsonConfiguration.GetBoolean("useIPC");
        else 
            JsonConfiguration.SetBoolean("useIPC", useIPC_Config);

        for (int i = 0; i < Lights.Count; i++)
            Materials.Add(Lights[i].GetComponent<Renderer>().material);
        
        if (useIPC_Config)
        {
            InitializeIPC("Local\\WACVR_SHARED_BUFFER", 2164);
            RGBColor2D = new Texture2D(480, 1, TextureFormat.RGBA32, false);
            //RGBColor2D.filterMode = FilterMode.Point; //for debugging
            //GetComponent<Renderer>().material.mainTexture = RGBColor2D; //for debugging
        }
    }
    private void Update() 
    {
        GetBytesFromMemory();
        GetTextureFromBytes();
        if (useIPC_Config)
            CheckIPCState();
        if (useIPC)
            UpdateLED();
    }
    private void CheckIPCState()
    {
        if (RGBColor2D.GetPixel(0 , 0).a == 0)
            useIPC = false;
        else
            useIPC = true;
    }
    private void InitializeIPC(string sharedMemoryName, int sharedMemorySize)
    {
        MemoryMappedFileSecurity CustomSecurity = new MemoryMappedFileSecurity();
        SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        var acct = sid.Translate(typeof(NTAccount)) as NTAccount;
        CustomSecurity.AddAccessRule(new System.Security.AccessControl.AccessRule<MemoryMappedFileRights>(acct.ToString(), MemoryMappedFileRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
        sharedBuffer = MemoryMappedFile.CreateOrOpen(sharedMemoryName, sharedMemorySize, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, CustomSecurity, System.IO.HandleInheritability.Inheritable);
        sharedBufferAccessor = sharedBuffer.CreateViewAccessor();
    }
    private void UpdateLED()
    {
        var colors = RGBColor2D.GetPixels32();

        int index = 0;
        for (int i = 0; i < 30; i++)
        {
            for (int ii = 0; ii < 4; ii++)
            {
                /*Materials[119 - i - ii * 30].SetColor("_EmissionColor", RGBColor2D.GetPixel(index * 2, 0));
                Materials[119 - i - ii * 30].SetColor("_EmissionColor2", RGBColor2D.GetPixel(index * 2 + 1, 0));
                Materials[210 + i - ii * 30].SetColor("_EmissionColor", RGBColor2D.GetPixel((index + 120) * 2, 0));
                Materials[210 + i - ii * 30].SetColor("_EmissionColor2", RGBColor2D.GetPixel((index + 120) * 2 + 1, 0));*/
                Materials[119 - i - ii * 30].SetColor("_EmissionColor", colors[index * 2]);
                Materials[119 - i - ii * 30].SetColor("_EmissionColor2", colors[index * 2 + 1]);
                Materials[210 + i - ii * 30].SetColor("_EmissionColor", colors[(index + 120) * 2]);
                Materials[210 + i - ii * 30].SetColor("_EmissionColor2", colors[(index + 120) * 2 + 1]);
                index++;
            }
        }
    }
    void GetTextureFromBytes()
    {
        RGBColor2D.LoadRawTextureData(buffer);
        RGBColor2D.Apply();
    }

    unsafe void GetBytesFromMemory()
    {
        // sharedBufferAccessor.ReadArray<byte>(244, buffer, 0, 1920);
        // https://stackoverflow.com/a/7956222
        const int offset = 244;
        const int num = 1920;
        try
        {
            byte* ptr = (byte*)0;
            sharedBufferAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            Marshal.Copy(IntPtr.Add(new IntPtr(ptr), offset), buffer, 0, num);
        }
        finally
        {
            sharedBufferAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    public void UpdateLightFade(int Area, bool State)
    {
        if(useIPC)
            return;

        Area -= 1;
        if (State)
        {
            Materials[Area].SetColor("_EmissionColor", new Color(1f, 1f, 1f, 1f));
            Materials[Area].SetColor("_EmissionColor2", new Color(1f, 1f, 1f, 1f));
        }
        else
        {
            if (coroutines[Area] != null)
                StopCoroutine(coroutines[Area]);
            coroutines[Area] = FadeOut(Area, Materials[Area]);
            StartCoroutine(coroutines[Area]);
        }      
    }
    public IEnumerator FadeOut(int Area, Material mat)
    {
        for (float time = 0f; time < FadeDuration; time += Time.deltaTime)
        {
            float p = 1 - time / FadeDuration;
            mat.SetColor("_EmissionColor", new Color(p, p, p, 1f));
            mat.SetColor("_EmissionColor2", new Color(p, p, p, 1f));
            yield return null;
        }
    }
}
