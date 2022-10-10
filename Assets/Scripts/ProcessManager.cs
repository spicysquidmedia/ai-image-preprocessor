namespace OpenCvSharp.Demo
{
    using System;
    using System.Text;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

    using UnityEngine;
    using UnityEngine.UI;
    using UnityEditor;

    using OpenCvSharp;

    public class ProcessManager : MonoBehaviour
    {
        public InputField inputFolderPathText;
        public InputField outputFolderPathText;
        public InputField outputWidthText;
        public InputField outputHeightText;
        public Text logText;

        private int maxLines = 6;
        private Queue<string> queue = new Queue<string>();

        private void Start()
		{
            inputFolderPathText.text = PlayerPrefs.GetString("_inputFolder", "");
            outputFolderPathText.text = PlayerPrefs.GetString("_outputFolder", "");

            outputWidthText.text = PlayerPrefs.GetString("_outputWidth", "512");
            outputHeightText.text = PlayerPrefs.GetString("_outputHeight", "512");
        }


        ////////////////////
        // Change Handlers
        ////////////////////

        public void InputFolderChanged()
        {
            PlayerPrefs.SetString("_inputFolder", inputFolderPathText.text);
		}

        public void OutputFolderChanged()
        {
            PlayerPrefs.SetString("_outputFolder", outputFolderPathText.text);
        }

        public void OutputWidthChanged()
        {
            PlayerPrefs.SetString("_outputWidth", outputWidthText.text);
        }

        public void OutputHeightChanged()
        {
            PlayerPrefs.SetString("_outputHeight", outputHeightText.text);
        }


        ////////////////////
        // Button Handlers
        ////////////////////

        public void BrowseForInputFolder()
        {
            // TODO: Replace EditorUtility functions with non-EditorUtility functions (EditorUtility classes are not available when publishing).
            /*Debug.Log("Browse for input folder button clicked.");
            string directory = EditorUtility.OpenFolderPanel("Select Directory", "", "");
            if(directory != "") {
                inputFolderPathText.text = directory;
                PlayerPrefs.SetString("_inputFolder", directory);
            }*/
        }

        public void BrowseForOutputFolder()
        {
            // TODO: Replace EditorUtility functions with non-EditorUtility functions (EditorUtility classes are not available when publishing).
            /*Debug.Log("Browse for output folder button clicked.");
            string directory = EditorUtility.OpenFolderPanel("Select Directory", "", "");
            if(directory != "") {
                outputFolderPathText.text = directory;
                PlayerPrefs.SetString("_outputFolder", directory);
            }*/
        }

        public void Process()
        {
            if(inputFolderPathText.text.Trim() == "") {
                writeLogLine("Input path cannot be blank.");
                return;
			}
            if(outputFolderPathText.text.Trim() == "") {
                writeLogLine("Output path cannot be blank.");
                return;
            }

            DirectoryInfo dir = new DirectoryInfo(inputFolderPathText.text);
            FileInfo[] info = dir.GetFiles();
            var fullNames = info.Select(f => f.Name).ToArray();
            foreach (string name in fullNames)
            {
                bool contains = false;
                if (name.Contains(".png")) { contains = true; }
                if (name.Contains(".jpg")) { contains = true; }
                if (name.Contains(".jpeg")) { contains = true; }

                if(contains == false) { continue; }

                writeLogLine($"Processing {name}");
                this.ProcessImage(name);
            }
        }

        ////////////////////
        // Log
        ////////////////////

        private void writeLogLine(string line)
        {
            if (queue.Count >= maxLines) queue.Dequeue();
            queue.Enqueue("•" + line);

            var builder = new StringBuilder();
            foreach (string st in queue) {
                builder.Append(st).Append("\n");
            }

            this.logText.text = builder.ToString();
		}

        ////////////////////
        // Image Processing
        ////////////////////

        public void ProcessImage(string fileName)
        {
            string fullInputPath = Path.Join(inputFolderPathText.text, fileName);
            string fullOutputPath = Path.Join(outputFolderPathText.text, fileName);

            Texture2D tex = null;
            byte[] fileData;
            if (File.Exists(fullInputPath))
            {
                fileData = File.ReadAllBytes(fullInputPath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            }



            Mat image = Unity.TextureToMat(tex);
            Mat grayMat = new Mat();
            Cv2.CvtColor(image, grayMat, ColorConversionCodes.BGR2GRAY);

            // This parameter should introduce same result of http://opencv.jp/wordpress/wp-content/uploads/lenna_SURF-150x150.png            
            ORB orb = ORB.Create(500);
            KeyPoint[] keyPoints = orb.Detect(grayMat);

            List<float> xPos = new List<float>();
            List<float> yPos = new List<float>();

            //Debug.Log($"KeyPoint has {keyPoints.Length} items.");
            foreach (var t in keyPoints)
            {
                //Debug.Log(t.Pt);

                /*var dotGO = new GameObject();
                Image dotImage = dotGO.AddComponent<Image>();
                dotGO.GetComponent<RectTransform>().SetParent(this.image.transform);
                //dotGO.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                //dotGO.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                dotGO.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                dotGO.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                dotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(4, 4);
                dotGO.GetComponent<RectTransform>().localPosition = new Vector2(t.Pt.X, -t.Pt.Y);*/

                xPos.Add(t.Pt.X);
                yPos.Add(t.Pt.Y);
            }

            float xAvg = xPos.Average();
            float yAvg = yPos.Average();
            //Debug.Log($"Average is {xAvg},{yAvg}");

            float imageWidth = tex.width;
            float imageHeight = tex.height;

            UnityEngine.Rect cropRect = new UnityEngine.Rect();
            if (imageHeight > imageWidth)
            {
                cropRect.x = 0;
                cropRect.y = imageHeight - yAvg;
                cropRect.y -= imageWidth / 2;
                //if(cropRect.y < 0) { cropRect.y = 0; }
                cropRect.width = imageWidth;
                cropRect.height = imageWidth;

                if (cropRect.y + cropRect.height > imageHeight) { cropRect.y = imageHeight - cropRect.height; }
            }
            else
            {
                cropRect.x = xAvg - (imageHeight / 2);
                cropRect.y = 0;
                cropRect.width = imageHeight;
                cropRect.height = imageHeight;

                if (cropRect.x < 0) { cropRect.x = 0; }
                if (cropRect.x + imageHeight > imageWidth) { cropRect.x = imageWidth - imageHeight; }
            }

            Texture2D result = new Texture2D((int)cropRect.width, (int)cropRect.height);
            result.SetPixels(tex.GetPixels((int)cropRect.x, (int)cropRect.y, (int)cropRect.width, (int)cropRect.height));
            result.Apply();

            int outputWidth = int.Parse(outputWidthText.text);
            int outputHeight = int.Parse(outputHeightText.text);
            result = this.ScaleTexture(result, outputWidth, outputHeight);

            byte[] bytes = result.EncodeToPNG();
            if(fileName.Contains(".png")) {
                bytes = result.EncodeToPNG();
			}
            if(fileName.Contains(".jpg") || fileName.Contains(".jpeg")) {
                bytes = result.EncodeToJPG();
            }
            File.Delete(fullOutputPath);
            File.WriteAllBytes(fullOutputPath, bytes);
        }

        private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
            Color[] rpixels = result.GetPixels(0);
            float incX = (1.0f / (float)targetWidth);
            float incY = (1.0f / (float)targetHeight);
            for (int px = 0; px < rpixels.Length; px++)
            {
                rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth), incY * ((float)Mathf.Floor(px / targetWidth)));
            }
            result.SetPixels(rpixels, 0);
            result.Apply();
            return result;
        }
    }
}