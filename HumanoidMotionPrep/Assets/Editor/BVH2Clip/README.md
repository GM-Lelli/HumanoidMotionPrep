# BVH Converter Documentation

## Overview

This documentation explains how the system for converting BVH files into `AnimationClip`s in Unity works. The system is composed of two main scripts: `BVH2ClipEditor.cs` and `BVHConverter.cs`, which work together to allow users to import BVH files and convert them into usable animations within Unity.

### Main Classes

1. **BVH2ClipEditor**: Provides a Unity Editor UI to configure and launch the conversion process.
2. **BVHConverter**: Handles the actual logic for converting BVH files into `AnimationClip`s.

---

## BVH2ClipEditor Class

The `BVH2ClipEditor` class provides an editor window in Unity to configure and start the conversion of BVH files into `AnimationClip`s. This window allows users to select a folder containing BVH files, specify the output directory, and choose whether to respect the original frame time defined in the BVH file or use a custom frame rate.

- **Description**:
  - Provides an editor window accessible from the menu "Tools > BVH to AnimationClip".
  - Allows selection of a folder containing BVH files and displays the path of the output directory.
  - Offers a choice between using the BVH file's original frame time (`respectBVHTime`) or a user-defined frame rate (`frameRate`).

- **Usage**:
  - To open the BVH converter window, go to "Tools > BVH to AnimationClip" in Unity.
  - After selecting the BVH folder, click the "Convert BVH to AnimationClip" button to start the process.

---

## BVHConverter Class

The `BVHConverter` class manages the logic for converting BVH files into `AnimationClip`s. It receives several parameters from the editor window such as the input BVH path, the output folder, and the frame rate. This class is responsible for creating animation clips and generating position and rotation curves for each joint.

- **Description**:
  - Takes a folder of BVH files and an output directory as input.
  - Converts each BVH file into a usable `AnimationClip` in Unity.
  - Uses the `BVHParser` class to extract the necessary data for the conversion.

- **Main Method**:
```csharp
  public void ConvertBVHToAnimationClip()
````

This method handles reading the BVH files, creating `AnimationClip`s, adding position and rotation curves, and saving the clips to the specified directory.

* **Key Features**:

  * **File paths**: Reads BVH files from the selected folder and saves the resulting animations in the output folder.
  * **Frame conversion**: If `respectBVHTime` is enabled, the frame time from the BVH file is used. Otherwise, a custom frame rate is applied.
  * **Rotations and positions**: Extracted data is used to create local position and rotation curves for each joint.
  * **Saving files**: Once created, each `AnimationClip` is saved using `AssetDatabase.CreateAsset()`.

---

### `GetCurves` Method

The `GetCurves` method is used to generate animation curves for each joint in the character.

* **Description**:

  * Takes as input the joint path, the `BVHBone` node from the parser, and the `AnimationClip` to which the curves will be added.
  * Extracts position and rotation values from the BVH channels and converts them into animation curves to be applied to the clip.

* **Rotation using `fromEulerZXY`**: Rotations are converted using the ZXY order to ensure that BVH rotation data is accurately represented as `Quaternion`s in Unity.

```csharp
private void GetCurves(string path, BVHParser.BVHBone node, AnimationClip clip)
```

This method adds the position and rotation curves for each joint to the `AnimationClip`, allowing for the creation of realistic animations.

---

## Summary

* **User Interface**: The `BVH2ClipEditor` class provides a GUI in the Unity Editor to simplify importing and converting BVH files.
* **Animation Conversion**: The `BVHConverter` class manages the complete process, from reading BVH files to generating and saving animation clips.

Together, these scripts allow users to easily convert BVH files into `AnimationClip`s and use them in their Unity projects, enabling the import of realistic animations from external sources.