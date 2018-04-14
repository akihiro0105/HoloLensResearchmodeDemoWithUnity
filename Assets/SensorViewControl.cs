using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_UWP
using System;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

public class SensorViewControl : MonoBehaviour {

    private Texture2D tex = null;
    private byte[] bytes = null;
    // Use this for initialization
    void Start() {
#if UNITY_UWP
        Task.Run(() => { InitSensor(); });
#endif
    }
	
	// Update is called once per frame
	void Update () {
		
	}

#if UNITY_UWP
    
    private async void InitSensor()
    {
        var mediaFrameSourceGroupList = await MediaFrameSourceGroup.FindAllAsync();
        var mediaFrameSourceGroup = mediaFrameSourceGroupList[0];
        var mediaFrameSourceInfo = mediaFrameSourceGroup.SourceInfos[0];
        var mediaCapture = new MediaCapture();
        var settings = new MediaCaptureInitializationSettings()
        {
            SourceGroup = mediaFrameSourceGroup,
            SharingMode = MediaCaptureSharingMode.SharedReadOnly,
            StreamingCaptureMode = StreamingCaptureMode.Video,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
        };
        try
        {
            await mediaCapture.InitializeAsync(settings);
            var mediaFrameSource = mediaCapture.FrameSources[mediaFrameSourceInfo.Id];
            var mediaframereader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource, mediaFrameSource.CurrentFormat.Subtype);
            mediaframereader.FrameArrived += FrameArrived;
            await mediaframereader.StartAsync();
        }
        catch (Exception e)
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() => { Debug.Log(e); }, true);
        }
    }

    private void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var mediaframereference = sender.TryAcquireLatestFrame();
        if (mediaframereference != null)
        {
            var videomediaframe = mediaframereference?.VideoMediaFrame;
            var softwarebitmap = videomediaframe?.SoftwareBitmap;
            if (softwarebitmap != null)
            {
                softwarebitmap = SoftwareBitmap.Convert(softwarebitmap, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
                int w = softwarebitmap.PixelWidth;
                int h = softwarebitmap.PixelHeight;
                if (bytes==null)
                {
                    bytes = new byte[w * h * 4];
                }
                softwarebitmap.CopyToBuffer(bytes.AsBuffer());
                softwarebitmap.Dispose();
                UnityEngine.WSA.Application.InvokeOnAppThread(() => {
                    if (tex == null)
                    {
                        tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                        GetComponent<Renderer>().material.mainTexture = tex;
                    }
                    for (int i = 0; i < bytes.Length / 4; ++i)
                    {
                        byte b = bytes[i * 4];
                        bytes[i * 4 + 0] = 0;
                        bytes[i * 4 + 1] = 0;
                        bytes[i * 4 + 2] = 0;
                        bytes[i * 4 + 3] = 255;
                        if (b==0)
                        {
                            bytes[i * 4 + 0] = 128;
                        }
                        if (b==1)
                        {
                            bytes[i * 4 + 0] = 255;
                        }
                    }
                    tex.LoadRawTextureData(bytes);
                    tex.Apply();
                }, true);
            }
            mediaframereference.Dispose();
        }
    }
#endif
}
