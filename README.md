Photo Inspector
===============

Photo Inspector is an example application on how to capture and process high
resolution photos (resolution depends on device hardware).

Capture a photo and slide your finger on the preview to bring up a loupe to zoom
right into pixel perfect details in the photo. Save a lower resolution copy of
the photo to the main Photos gallery while retaining the original maximum
resolution photo in application local storage for later use. Share photos as
lower resolution copies to online services like Facebook and Twitter.

The example has been developed with Silverlight for Windows Phone devices and
tested to work on Nokia Lumia devices with Windows Phone 8.

This example application is hosted in GitHub:
https://github.com/nokia-developer/photo-inspector

For more information on implementation, visit Nokia Lumia Developer's Library:
http://developer.nokia.com/Resources/Library/Lumia/#!imaging/working-with-high-resolution-photos/photo-inspector.html


1. Usage
-------------------------------------------------------------------------------

This is a simple build-and-run solution. See section 5 for instructions on how
to run the application on your Windows Phone 8 device.


2. Prerequisites
-------------------------------------------------------------------------------

* C# basics
* Windows 8
* Microsoft Visual Studio Express for Windows Phone 2012


3. Project structure and implementation
-------------------------------------------------------------------------------

3.1 Folders
-----------

* The root folder contains the project file, the license information and this
  file.
* `MagnifierApp`: Root folder for the implementation files.  
* `Assets`: Graphic assets like icons and tiles.
* `Pages`: Phone application pages.
* `Models`: Photo load and save model.
* `Properties`: Application property files.
* `Resources`: Application resources.
* `Utilities`: Utility classes.

3.2 Important files and classes
-------------------------------

| File | Description |
| ---- | ----------- |
| `Models/PhotoModel.cs` | Saving and loading photos, current photo tombstoning, local storage handling. |
| `Pages/ViewfinderPage.xaml(.cs)` | Simple viewfinder for capturing photos. |
| `Pages/MagnifierPage.xaml(.cs)` | Photo preview and touch-to-zoom photo detail inspection loupe. |
| `Pages/PhotosPage.xaml(.cs)` | Shows locally saved high resolution photos on high resolution capable devices. |
| `Pages/CropPage.xaml(.cs)` | Allows user to crop the photo. Pan & pinch zoom functionality. |
| `Pages/InfoPage.xaml(.cs)` | Displays selected EXIF records for current photo. |


4. Compatibility
-------------------------------------------------------------------------------

Application works on Windows Phone 8.

Tested to work on Nokia Lumia 1020, Nokia Lumia 925, Nokia Lumia 920,
Nokia Lumia 620, Nokia Lumia 520.

Developed with Microsoft Visual Studio Express for Windows Phone 2012.

4.1 Required Capabilities
-------------------------

* `ID_CAP_ISV_CAMERA`
* `ID_CAP_MEDIALIB_PHOTO`

4.2 Known Issues
----------------

None.


5. Building, installing, and running the application
-------------------------------------------------------------------------------

5.1 Preparations
----------------

Make sure you have the following installed:

* Windows 8
* Windows Phone SDK 8.0

5.2 Using the WINDOWS PHONE 8 SDK
---------------------------------

1. Open the SLN file:
   File > Open Project, select the file MagnifierApp.sln
2. Select the target 'Device' and 'ARM'.
3. Press F5 to build the project and run it on the device.

5.3 Deploying to Windows Phone 8
--------------------------------

Please see official documentation for deploying and testing applications on
Windows Phone devices:
http://msdn.microsoft.com/library/windowsphone/develop/ff402565(v=vs.105).aspx


6. License
-------------------------------------------------------------------------------

See the license text file delivered with this project:
https://github.com/nokia-developer/photo-inspector/blob/master/License.txt


7. Related documentation
-------------------------------------------------------------------------------

See http://developer.nokia.com/Resources/Library/Lumia/#!imaging/working-with-high-resolution-photos/photo-inspector.html 
for more information on the project.

See http://developer.nokia.com/Resources/Library/Lumia/#!imaging/working-with-high-resolution-photos.html
for information on how to capture and handle high resolution photos for example on
Nokia Lumia 1020.


8. Version history
-------------------------------------------------------------------------------

* 1.2: Third public release of Photo Inspector
  - Updated to the latest Nokia Imaging SDK
  - Now using Nuget Package Restore for external libraries

* 1.1: Second public release of Photo Inspector
  - Photos can be cropped, and it's possible to either change the framing again or to revert to the original uncropped photo
  - New photo information page for displaying EXIF information
  - Locally saved photos are displayed on all devices, not just high camera resolution devices
  - Camera and locally saved photos can be accessed from the magnification page
  - Fixed issue where pressing the camera HW button did not capture an image if camera was focusing at the same time
  - Locally saved photos page shows a "reframed" indicator on photos that are crops
  - Also included are a number of other minor changes

* 1.0: First public release of Photo Inspector