using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using HoloToolkit.Unity.InputModule;
using Newtonsoft.Json;//

using System.Linq;

public class ImageToCustomVisionAPI : MonoBehaviour, IInputClickHandler
{
	string customVisionURL = "https://japaneast.api.cognitive.microsoft.com/customvision/v3.0/Prediction/36f95826-948e-49da-8d09-aa135b665725/detect/iterations/TrafficLight%20Itr.3/image"; // 自身のCustom Vision Services URL（filesのほう）を貼り付ける
	string apiKey = "7091a4ccdc2d4ba59ea7b22b7d45dd28"; //自身のPrediction-Key（filesのほう）を貼り付ける

	public GameObject ImageFrameObject;
	public Text textObject;
    public GameObject BlockingSphere;
	UnityEngine.XR.WSA.WebCam.PhotoCapture photoCaptureObject = null;

	void Start()
	{
		InputManager.Instance.PushFallbackInputHandler(gameObject);
	}

	//カメラの設定 ここから。
	void OnPhotoCaptureCreated(UnityEngine.XR.WSA.WebCam.PhotoCapture captureObject)
	{
		photoCaptureObject = captureObject;

		Resolution cameraResolution = UnityEngine.XR.WSA.WebCam.PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

		UnityEngine.XR.WSA.WebCam.CameraParameters c = new UnityEngine.XR.WSA.WebCam.CameraParameters();
		c.hologramOpacity = 0.0f;
		c.cameraResolutionWidth = cameraResolution.width;
		c.cameraResolutionHeight = cameraResolution.height;
		c.pixelFormat = UnityEngine.XR.WSA.WebCam.CapturePixelFormat.JPEG;

		captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
	}

	void OnStoppedPhotoMode(UnityEngine.XR.WSA.WebCam.PhotoCapture.PhotoCaptureResult result)
	{
		photoCaptureObject.Dispose();
		photoCaptureObject = null;
	}

	private void OnPhotoModeStarted(UnityEngine.XR.WSA.WebCam.PhotoCapture.PhotoCaptureResult result)
	{

		if (result.success)
		{
			photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
		}
		else
		{
		}
	}

	//カメラの設定 ここまで。
	private void OnCapturedPhotoToMemory(UnityEngine.XR.WSA.WebCam.PhotoCapture.PhotoCaptureResult result, UnityEngine.XR.WSA.WebCam.PhotoCaptureFrame photoCaptureFrame)
	{

		if (result.success)
		{
			List<byte> imageBufferList = new List<byte>();
			// Copy the raw IMFMediaBuffer data into our empty byte list.
			photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

			DisplayImage(imageBufferList.ToArray());                            //画像表示処理呼び出し
			StartCoroutine(GetVisionDataFromImages(imageBufferList.ToArray())); //API呼び出し
		}

		photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
	}


	/// <summary>
	/// Get data from the Cognitive Services Custom Vision Services
	/// Stores the response into the responseData string
	/// </summary>
	/// <returns> IEnumerator - needs to be called in a Coroutine </returns>
	IEnumerator GetVisionDataFromImages(byte[] imageData)
	{
		var headers = new Dictionary<string, string>() {
			{ "Prediction-Key", apiKey },
			{ "Content-Type", "application/octet-stream" }
		};

        WWW www = new WWW(customVisionURL, imageData, headers);

        yield return www;
		string responseData = www.text; // Save the response as JSON string

        ResponceJson json = JsonConvert.DeserializeObject<ResponceJson>(responseData);

        float tmpProbability = 0.0f;
		string str = "";


        Prediction bestMatch = json.Predictions[0];

        for (int i = 0; i < json.Predictions.Length; i++)
        {
        	Prediction obj = (Prediction)json.Predictions[i];

        	Debug.Log(obj.TagName);

        	if (tmpProbability < obj.Probability)
        	{
        		str = obj.Probability.ToString("P") + "の確率で" + obj.TagName + "です";
        		tmpProbability = obj.Probability;
                bestMatch = obj;
                Debug.Log(bestMatch.Probability);
            }
        }
        textObject.text = str;
        Debug.Log(bestMatch.Probability+":"+bestMatch.BoundingBox.Left+","+ bestMatch.BoundingBox.Top + "," + bestMatch.BoundingBox.Width + "," + bestMatch.BoundingBox.Height);

        float tempx = bestMatch.BoundingBox.Left + bestMatch.BoundingBox.Width;
        float x = (tempx * 722) - 361;

        float tempy = bestMatch.BoundingBox.Top + bestMatch.BoundingBox.Height;
        float y = (tempy * 424) - 212;

        Debug.Log(x + ":" + y);

        BlockingSphere.transform.localPosition = new Vector3(x, y, 0);
    }

    // キャプチャした画像をImageに貼り付ける（ImageFrameObject実態はCubeを薄くしたもの）
    private void DisplayImage(byte[] imageData)
	{
		Texture2D imageTxtr = new Texture2D(2, 2);
		imageTxtr.LoadImage(imageData);
		ImageFrameObject.GetComponent<Renderer>().material.mainTexture = imageTxtr;
	}

	// エアタップの取得
	public void OnInputClicked(InputClickedEventData eventData)
	{
		textObject.text = "Call Custom Services...";
        BlockingSphere.transform.localPosition = new Vector3(0, 0, 0);
        UnityEngine.XR.WSA.WebCam.PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }
}

[Serializable]
public class ResponceJson
{
	public string Id;
	public string Project;
	public string Iteration;
	public string Created;

	public Prediction[] Predictions;
}

[Serializable]
public class Prediction
{
    public string TagId;
    public string TagName;
    public float Probability;

    public Positions BoundingBox;
}

[Serializable]
public class Positions
{
    public float Left;
    public float Top;
    public float Width;
    public float Height;
}