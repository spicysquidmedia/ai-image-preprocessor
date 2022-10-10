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

    using SFB;

    public class ProcessManager : MonoBehaviour
    {
        [Header("Folder Settings")]
        public InputField inputFolderPathText;
        public InputField outputFolderPathText;

        [Header("Output Settings")]
        public InputField outputWidthText;
        public InputField outputHeightText;
        public Toggle exportImageToggle;
        public Toggle exportCropDataToggle;

        [Header("Processing")]
        public GameObject processingOverlay;
        public Text logText;

        private int maxLines = 6;
        private Queue<string> queue = new Queue<string>();

        private void Start()
		{
            inputFolderPathText.text = PlayerPrefs.GetString("_inputFolder", "");
            outputFolderPathText.text = PlayerPrefs.GetString("_outputFolder", "");

            outputWidthText.text = PlayerPrefs.GetString("_outputWidth", "512");
            outputHeightText.text = PlayerPrefs.GetString("_outputHeight", "512");

            processingOverlay.SetActive(false);
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
            string[] directory = StandaloneFileBrowser.OpenFolderPanel("", "", false);
            if(directory.Length != 1) { return; }

            inputFolderPathText.text = directory[0];
            PlayerPrefs.SetString("_inputFolder", directory[0]);
        }

        public void BrowseForOutputFolder()
        {
            string[] directory = StandaloneFileBrowser.OpenFolderPanel("", "", false);
            if (directory.Length != 1) { return; }

            outputFolderPathText.text = directory[0];
            PlayerPrefs.SetString("_outputFolder", directory[0]);
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

            processingOverlay.SetActive(true);
            if(exportCropDataToggle.isOn == true) {
                string csvPath = Path.Join(outputFolderPathText.text, "cropData.csv");
                File.Delete(csvPath);
                string line = $"Filename,X,Y,Width,Height\n";
            }

            StartCoroutine(ProcessAllImages());
        }

        private IEnumerator ProcessAllImages()
        {
            DirectoryInfo dir = new DirectoryInfo(inputFolderPathText.text);
            FileInfo[] info = dir.GetFiles();
            var fullNames = info.Select(f => f.Name).ToArray();
            foreach (string name in fullNames)
            {
                bool contains = false;
                if (name.Contains(".png")) { contains = true; }
                if (name.Contains(".jpg")) { contains = true; }
                if (name.Contains(".jpeg")) { contains = true; }

                if (contains == false) { continue; }

                writeLogLine($"Processing {name}");
                ProcessImage(name);
                yield return new WaitForSeconds(0.1f);
            }

            processingOverlay.SetActive(false);
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
            int outputWidth = int.Parse(outputWidthText.text);
            int outputHeight = int.Parse(outputHeightText.text);

            Texture2D texture = LoadTexture(fullInputPath);

            // Get meta data for texture.
            Vector2 poiAvg = GetAvgORBKeypointPosition(texture);
            UnityEngine.Rect cropRect = GetCropRect(texture, poiAvg);

            if(exportCropDataToggle.isOn == true) {
                SaveCropData(fileName, cropRect);
			}
            if(exportImageToggle.isOn == true) {
                ProcessAndSaveImage(texture, cropRect, outputWidth, outputHeight, fileName, fullOutputPath);
            }
        }

        private void SaveCropData(string fileName, UnityEngine.Rect cropRect)
        {
            string csvPath = Path.Join(outputFolderPathText.text, "cropData.csv");
            string line = $"{fileName},{cropRect.x},{cropRect.y},{cropRect.width},{cropRect.height}\n";

            File.AppendAllText(csvPath, line);
		}

        private void ProcessAndSaveImage(Texture2D texture, UnityEngine.Rect cropRect, int outputWidth, int outputHeight, string fileName, string fullOutputPath)
        {
            // Crop/scale texture.
            Texture2D result = CropTexture(texture, cropRect);
            result = this.ScaleTexture(result, outputWidth, outputHeight);

            byte[] bytes = new byte[0];
            if (fileName.Contains(".png"))
            {
                bytes = result.EncodeToPNG();
            }
            if (fileName.Contains(".jpg") || fileName.Contains(".jpeg"))
            {
                bytes = result.EncodeToJPG();
            }
            File.Delete(fullOutputPath);
            File.WriteAllBytes(fullOutputPath, bytes);
        }

        ////////////////////
        // Image Helpers
        ////////////////////

        private UnityEngine.Rect GetCropRect(Texture2D texture, Vector2 poiAvg)
        {
            float imageWidth = texture.width;
            float imageHeight = texture.height;

            UnityEngine.Rect cropRect = new UnityEngine.Rect();
            if (imageHeight > imageWidth)
            {
                cropRect.x = 0;
                cropRect.y = poiAvg.y - (imageWidth / 2);
                //if(cropRect.y < 0) { cropRect.y = 0; }
                cropRect.width = imageWidth;
                cropRect.height = imageWidth;

                if(cropRect.y < 0) { cropRect.y = 0; }
                if (cropRect.y + imageWidth > imageHeight) { cropRect.y = imageHeight - imageWidth; }
            }
            else
            {
                cropRect.x = poiAvg.x - (imageHeight / 2);
                cropRect.y = 0;
                cropRect.width = imageHeight;
                cropRect.height = imageHeight;

                if (cropRect.x < 0) { cropRect.x = 0; }
                if (cropRect.x + imageHeight > imageWidth) { cropRect.x = imageWidth - imageHeight; }
            }

            cropRect.x = (int)cropRect.x;
            cropRect.y = (int)cropRect.y;
            cropRect.width = (int)cropRect.width;
            cropRect.height = (int)cropRect.height;

            return cropRect;
		}

        private Texture2D LoadTexture(string filePath)
        {
            Texture2D tex = null;
            byte[] fileData;
            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            }

            return tex;
		}

        private Vector2 GetAvgORBKeypointPosition(Texture2D texture)
        {
            // Convert texture to grayscale mat.
            Mat image = Unity.TextureToMat(texture);
            Mat grayMat = new Mat();
            Cv2.CvtColor(image, grayMat, ColorConversionCodes.BGR2GRAY);

            // Get ORB keypoints.
            ORB orb = ORB.Create(500);
            KeyPoint[] keyPoints = orb.Detect(grayMat);

            // Get x/y avg position from all keypoints.
            List<float> xPos = new List<float>();
            List<float> yPos = new List<float>();

            foreach (var t in keyPoints)
            {
                xPos.Add(t.Pt.X);
                yPos.Add(t.Pt.Y);
            }

            float xAvg = xPos.Average();
            float yAvg = yPos.Average();

            return new Vector2(xAvg, yAvg);
        }

        private Texture2D CropTexture(Texture2D texture, UnityEngine.Rect cropRect)
        {
            cropRect.y = texture.height - (cropRect.y + cropRect.height);

            Texture2D result = new Texture2D((int)cropRect.width, (int)cropRect.height);
            result.SetPixels(texture.GetPixels((int)cropRect.x, (int)cropRect.y, (int)cropRect.width, (int)cropRect.height));
            result.Apply();

            return result;
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