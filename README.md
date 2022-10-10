# AI Image Preprocessor
Unity application that will automatically find the areas of interest in a photograph, use the average of those points as a center point, and crop/resize the images to a given size. Areas of interest are found using OpenCV's ORB. Application is a UI that lets you select an input folder, output folder, and output width/height.

# Unity Packages Prerequisites
[OpenCV plus Unity](https://assetstore.unity.com/packages/tools/integration/opencv-plus-unity-85928)

# TODO
- Set up browse button instead of having to paste in path
- Set up ability to export cropped images or CSV file with crop rect data
- Update processing code to be on own thread so UI can update while processing
