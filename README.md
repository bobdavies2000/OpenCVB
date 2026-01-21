**January 20, 2026 – Splash Screen, Oak 4D, Align Left to RGB, Disparity, IR Emitters, “NR_” Prefix, and Motion Detection**

-   Over 1500 algorithms are included, averaging 36 lines of code per algorithm.
    -   Over 300 additional obsolete algorithms compiled for reference use.
-   Showing the splash screen is now optional and default is true.
-   Oak3 D camera correctly stops when changing cameras or resolution.
-   The Oak 4D camera is now recognized and supported.
-   Updated snippets with Namespace. Also unused snippets removed.
-   Improved splash screen shows camera and current algorithm.
    -   Splash screen is now optional.
-   Text markups are removed from “Test All” runs to reduce object usage.
    -   This only happens during overnight test runs, not normal usage.
    -   Extensive use on some algorithms can exceed User/GDI objects.
-   All supported cameras now can align the left view and depth.
    -   All previous solutions aligned depth to color.
-   Oak 3D and Oak 4D cameras collect disparity as well as depth.
    -   Whether this is useful has yet to be determined.
-   A small piece of electrical tape can turn off the IR emitter on any camera.
    -   This allows some interesting tests to be done with left/right images.
    -   The featureless regions will have degraded depth when tape is present.
-   The “NR_” category of algorithms are all the algorithms that are not reused.
    -   The algorithms are in the same VB.Net file as before but prefixed “NR_”.
    -   They are still tested in the overnight runs and will still work.
    -   Benefit: the algorithm combo box is much more readable.
    -   To reactivate an algorithm, just remove the “NR_” prefix.
-   An alternative motion detector was added to OpenCVB.
    -   A pixel is “changed” if all 3 colors are different from the accumulated frame.
    -   About 20% fewer pixels pass threshold tests for motion.
    -   This is an experiment and will be reviewed over time.
-   Another alternative for motion detection is to use the left infrared image.
    -   The experimental code for detecting left image motion is in Motion_Left.
    -   This is an experiment and will be reviewed over time.

**January 10, 2026 – Play/Pause, Oak Support, Test All, Exceptions.**

-   Over 1500 algorithms are included, averaging 36 lines of code per algorithm.
    -   Over 300 additional obsolete algorithms compiled for reference use.
-   The play/pause button has been removed – it was not used or needed.
-   Code Review: cursor.ai inspected the code and made recommendations.
-   Switching between cameras had some inconsistencies, now removed.
-   The Oak3-D Pro support now installs examples to allow comparisons.
    -   The Oak3 left image is now the RGB image (grayscale only.)
    -   The Oak3 point cloud is computed in OpenCVB.
    -   Advantage: the depth is aligned to the left image automatically.
-   The Oak4-D Pro support is stalled awaiting a signed USB driver for installation.
-   The “Test All” overnight testing has been improved.
-   Error handling in the camera callback provides 2 ways to handle exceptions:
    -   Default: use Try/Catch to log the error and keep running.
    -   Turn on VS Menu “Debug-\>Windows-\>Exception Settings” to stop run.
-   For “Test All” runs using the Try/Catch Default above may be preferred.
    -   For debugging a new algorithm, the VS Menu solution is preferred.
-   SharpGL algorithms now remember their size and location.

**January 5, 2026 – Oak-D Pro, More Install Fixes.**

-   NOTE: OpenCVB requires VS 2026 and will install OpenCV 4.14.
    -   Install script uses the “.slnx” designation for solutions.
-   Oak D support is restored using the latest release.
    -   The original Oak D cameras are supported and working.
    -   My Oak 4 D Pro camera doesn’t work. USB-C connection failed.
    -   A POE switch has been ordered – arrives tomorrow.
    -   At almost \$1k, the camera is too expensive to order a second.
-   The overnight testing was improved and runs without restarting camera.
-   Splash screen was added and camera startup messages were removed.
-   OpenCVB’s icon now appears in the taskbar.
-   OpenCVB’s SharpGL interface is working.
-   RedColor color management was improved – no repeated colors.
-   

**January 1, 2026 – Oak-D Pro, Install Fixes.**

-   This version of OpenCVB requires VS 2026 and OpenCV 4.14.
    -   Install script uses the “.slnx” designation for solutions.
-   The latest version of CMake is recommended with this version.
-   The installation scripts have been more thoroughly tested.
-   GIFBuilder and UI_Generator are built with the rest of OpenCVB.
    -   They were previously build with the Update_All script.
-   There were some improvements in camera buffer management.
    -   Using locks reduced the number of copies.

**December 30, 2025 – Oak-D Pro Camera Support under construction.**

-   The Oak-D Pro camera .Net interface in OpenCVB used R2 depthai-core.
-   The support for Oak-D is no being restarted with R3 depthai-core.
-   The R2 OpenCVB support needs to be rewritten = different interfaces.
-   The infrastructure for R3 is now in place with this release.

# December 23, 2025 – Install Fixes and Testing.

-   This version of OpenCVB requires VS 2026 and OpenCV 4.14.
    -   Install script uses the “.slnx” designation for solutions.
    -   Start with a clean directory to avoid version confusion.
-   The latest version of CMake is recommended with this version.
-   The installation scripts have been more thoroughly tested.
-   GIFBuilder and UI_Generator are built with the rest of OpenCVB.
    -   They were previously built with the Update_All script.
-   There were some improvements in camera buffer management.
    -   Using locks reduced the number of copies.

# December 23, 2025 – Oak-D Pro Camera support under construction.

-   The Oak-D Pro camera .Net interface in OpenCVB used R2 depthai-core.
-   The support for Oak-D is no being restarted with R3 depthai-core.
-   The R2 OpenCVB support needs to be rewritten = different interfaces.
-   The infrastructure for R3 is now in place with this release.

# December 23, 2025 – Installation and Build Issues.

-   The previous release did not properly install.
-   Most of the issues were related to the switch to Visual Studio 2026
-   VCVarsAll.bat would not be found on systems without VS 2026.
-   The Build configuration was not properly set for X64.
-   Visual Studio 2026 is required for OpenCVB. VS 2022 support is dropped.
-   Visual Studio 2026 Community Edition is free so this is not onerous.

# December 17, 2025 – VS2026, Infrastructure, Reduced Threading, Display FPS, Error-Handling, JSON, Image Sizing.

-   Over 1500 algorithms are included, averaging 36 lines of code per algorithm.
    -   Over 300 additional obsolete algorithms compiled for reference use.
-   OpenCVB is now updated to use Visual Studio Community Edition 2026 (free.)
    -   OpenCVB Python infrastructure was removed – no longer needed.
    -   OpenCVB OpenGL interface replaced with SharpGL package.
-   New tools were used to rebuild OpenCVB and improve the application.
    -   Cursor.ai was used to rebuild the infrastructure for algorithm testing.
    -   The algorithms are unchanged from the previous OpenCVB version.
    -   Better infrastructure means easier debugging and fewer bugs.
-   One key simplification of OpenCVB was *reduced* threading.
    -   There are now just 2 threads in OpenCVB – UI and the camera task.
    -   Threading added complexity to understanding the code.
    -   The algorithm thread UI is the same thread as the UI.
-   The motive for the earlier multi-threading was to remove the cost of display.
    -   Single threading means display will interrupt or hold back processing.
    -   A global option “Display FPS” now limits the cost of displaying images.
        -   It may not be a useful enhancement, but it is worth more study.
-   Cautious error-handling (Try/Catch) is limited in its use in OpenCVB.
    -   OpenCVB is always running in Debug mode with source so “let it fail”.
    -   Check all Windows Debugging Exceptions to stop the code at the error.
    -   The intent is to get as close to the error as possible.
    -   The code is simpler to read with limited impact on debugging.
-   OpenCVB options are now exclusively maintained in JSON.
    -   No GetSetting or SaveSetting calls remain.
    -   JSON settings are available to algorithms through a shared library.
-   The image size of all 4 OpenCVB outputs is completely customizable.
    -   Clicking or drawing on the image uses work resolution coordinates.
-   Algorithm count in the main form caption only reflects active algorithms.
    -   The algorithm count was over 2000 but is now close to 1500.

# November 24, 2025 – Line Matching, Right View Motion

-   Over 2000 algorithms are included, averaging 36 lines of code per algorithm.
    -   Over 400 algorithms are obsolete and are compiled for reference use.
-   OpenCVB is now updated to use Visual Studio Community Edition 2026 (free.)
-   Like many other applications, OpenCVB is being rewritten using modern tools.
    -   Cursor’s tool is assisting in the current rewrite.
    -   The Main.vb is still the default startup project while conversion progresses.
    -   The CVB project can be set to the startup project for testing.
-   LineMatch_Basics algorithm finds the matching line in the previous image.
    -   The algorithm finds the longest lines and their earlier location.
    -   Line angle is used to confirm that the match is the same line.
    -   Further confirmation is needed with many parallel lines.
-   Motion is also found in the right camera image.
    -   Motion_Basics is reused to find motion in the right image.
        -   Not a lot of work was involved in getting this working.
    -   Potential uses for finding motion in the right image are not yet clear.

![A computer screen shot AI-generated content may be incorrect.](media/23d8c048a4cbb9cd85c40044609b8bb5.gif)**LineMatch_Basics :** *Lines are identified for each image - Line_Basics is a ‘task’ algorithm that runs on every frame. Using the center of each line, the match candidates are located within X pixels around the line center. The list of candidate lines is reviewed and the one with an angle within Y degrees of the original is designated the matching line. The bottom left image contains the longest lines for the current frame and the bottom right image shows the lines that match from the previous frame. When the camera is not moving, this algorithm is not challenging but when the camera is moving, this algorithm still works quite well. A toggle shows the matches alone and in the context of the current and previous image.*

# November 13, 2025 – Motion.

-   Over 2000 algorithms are included, averaging 36 lines of code per algorithm.
    -   Over 400 algorithms are obsolete and are compiled for reference use.
-   The Motion_Basics algorithm was reworked to use link 1, link 4, and link 8.
-   Which cells are replaced when a grid rect shows motion?
    -   There are 3 choices – cell, cell + link 4 cells, cell + link 8 cells.
    -   The choice “cell + link 4 cells” works but more testing needed.
-   Motion detection is validated with Motion_Validate (shown below.)
-   Motion detection improves several other algorithms.
    -   Unchanged lines are detected in Line_Basics when using motion detection.
    -   Unchanged edges and lines are detected exactly in EdgeLine_Basics.
    -   RedCloud and RedColor are improved with a motion-updated point cloud.
        -   More testing is required to manage regions with no depth.
-   These improvements are possible by removing slight pixel differences.
    -   Using a motion-updated image allows results to be matched exactly.
-   Motion detection relies on options specified in “Options_Diff”.
    -   There is an option for the size of differences in individual pixel values.
    -   There is also an option for pixel differences in a grid rect.
    -   Motion in the image is confined by the grid rectangles.
-   Motion detection can be turned off (global option “Use Motion Mask”.)
-   The stable gray image (grayStable) contains the motion-updated image.
    -   A motion-updated point cloud is now the default for all algorithms.

**![A screenshot of a computer AI-generated content may be incorrect.](media/77c9976b5a0810a5e2671c395b9ca196.gif)**

**Motion_Validate:** *This algorithm compares the motion-updated image with the camera image. The differences are highlighted in the lower right image. The lower left image is the motion-updated image while the upper right image is the original image from the camera. In the upper left image, the motion mask is shown on the RGB image. The text in the lower right image contains a count of the number of pixels that differ and the number of grid rectangles that contain motion. Note that there are plenty of differences between the current image and the motion-updated image but that these differences are small even where there is motion. The option for “cell + link 4 cells” is visible in the motion mask (upper left.)*

# November 2, 2025 – Global Options, Motion, Coherence.

-   Over 2000 algorithms are included, averaging 36 lines of code per algorithm.
    -   Over 350 obsolete algorithms are compiled just for reference use.
-   The Global Options were reviewed and simplified.
    -   Color and depth difference thresholds will appear only when needed.
    -   Depth colorizer options to show correlations was removed.
        -   Use Brick_CorrelationMap to see the left/right correlations.
    -   RedCloud display to show mean color was removed – not useful.
    -   The option to use Kalman filtering is now shown only when applicable.
-   The motion mask is not typically used for maintaining an RGB image.
    -   Flaws in the motion mask are visible in the accumulated grayscale image.
    -   Line detection can use the motion mask to limit what lines changed.
    -   Similarly, RedCloud algorithms use the motion mask to limit cell changes.
    -   RGB motion may not detect all changes to depth shadow.
        -   Cells in depth shadow are still valid because there was no motion.
        -   Cells exposed by depth shadow changes may be missed.
        -   Further experimentation will resolve this potential problem.
-   RedCC_Basics runs both RedCloud and RedColor on the same image set.
    -   Each cell in RedCC_Basics is coherent in both depth and color.
    -   Use RedCC_CellHistogram to confirm the depth range is singular.
-   RedCC_CellHistogram is used below to confirm each cell’s depth range.
    -   Each histogram has a depth range that is contiguous.
    -   Further experimentation is needed to validate the depth results.
    -   Color can be confirmed using the RGB image in the upper left.

**![A screenshot of a computer AI-generated content may be incorrect.](media/9f61236cc37ed9a3912de5e625010cab.gif)**

**RedCC_CellHistogram:**  *RedCC_CellHistogram displays the results of RedCC_Basics in the lower left. Each RedCC cell is coherent in both depth and color meaning that the cell depth data is in a singular spike in the histogram and the color data is a single class in the reduced color image. The histogram in the lower right is for the selected cell. The selected cell is highlighted in the upper left RGB image. The cell details are in the upper right.*

# October 27, 2025 – OpenCVB Docs, Depth Colorizer, Edge_Line, RedCloud, RedColor, OpenGL updates.

-   Over 2000 algorithms are included, averaging 36 lines of code per algorithm.
    -   350 algorithms were moved to XO.vb – the ‘obsolete’ algorithms.
    -   Obsolete algorithms will compile but are no longer tested.
    -   Obsolete algorithms are for reference and possible reactivation.
-   The OpenCVB documentation has been split into 2 parts:
    -   OpenCVB_Desc: the OpenCVB description and installation instructions.
    -   ReadMe: a log of updates to the OpenCVB source code.
-   The depth colorizer is only run when it is visible in the upper right image.
    -   This saves the cycles needed to prepare the DepthRGB image.
-   The EdgeLine_Basics algorithm is now a “shared” algorithm in the task structure.
    -   Algorithms using edges invoke the constructor to trigger running it.
    -   EdgeLine_Basics results are available to all if one algorithm needs it.
-   The RedCloud (reduced point cloud) algorithm has significantly changed.
    -   The X, Y, and Z channels of the point cloud are reduced separately.
    -   Previously, the product of X and Y channels was reduced.
    -   The new method produces more consistent cells.
    -   “Reduction Target” is dynamically adjusted to maximize use of depth data.
        -   If the reduction target is too low, too many cells are generated.
        -   If it is too high, the larger cells become to unpredictable.
-   The RedColor (reduced color) algorithms were reorganized as well.
    -   Most of the algorithms were moved to the XO.vb – obsolete.
    -   The few that remain contain the essential portions of the algorithm.
    -   RedColor and RedCloud algorithms share the same cell description.
-   An OpenGL presentation of RedCloud cells is available with this version.
    -   The full image is presented as an OpenGL texture.

![](media/ce6b4b56d0edafe998d3f1cdee0d35b4.gif)

**RedCC_Basics:**  *RedCC_Basics displays both the segmented depth data and the segmented color data. The camera is moving left and right in this demonstration. The image in the lower left is the output of the RedCloud_Basics while the lower right is the output of the RedColor_Basics. RedCloud is short-hand for reduced point cloud while RedColor is short-hand for reduced color. Clicking a color segment in the lower right will display the RedColor segment in the lower left while clicking in the lower left will display the RedCloud segment in the lower right. This technique allows studying the overlap of RedColor segments in depth data and the overlap of RedCloud segments in color data.*

# 

# 2025 September 23 – Logical Lines, OpenCVB icon, Better Defaults.

-   Over 1900 algorithms are included, averaging 36 lines of code per algorithm.
-   Logical Lines are RGB lines that are translated into 3D lines.
    -   The assertion is that RGB lines will also have linear depth.
        -   Not always true but often enough to be worth implementing.
        -   Exceptions need to be detected and understood.
    -   The depth for each pixel in a line is linearly connected to its neighbors.
    -   Gaps in depth data are filled in with a computed linear depth.
    -   Depth is the average of available depth pixels in the line.
    -   The line center will be set to have average depth.
    -   Linear increment is the average increment of neighboring depth pixels.
    -   The end points are computed using the linear increment.
    -   Line color is assigned using the grid index of the first point.
-   The generic icon for OpenCVB was corrected (bug in .Net 8.0?)
-   The default value for SharpGL is to use non-linear mode – more realistic.
    -   If SharpGL’s ReadPointCloud is used, linear mode is recommended.
-   It is no longer the default to transform the point cloud using the gravity vector.

![A computer screen shot of a room AI-generated content may be incorrect.](media/b52ca4c4a1e2f8e993985b1ca8518d72.gif)

**GL_LogicalCloud:**  *The point cloud above is enhanced with logical lines.*

# 2025 September 11 – SharpGL, Memory, FPS.

-   Over 1900 algorithms are included, averaging 36 lines of code per algorithm.
-   SharpGL may be run as one of the 4 windows in the main form for OpenCVB.
    -   GL_MainForm demonstrates how to do this and what it costs (see below.)
    -   The cost is the work to move the data in the task thread back to the main form.
    -   There may be an opportunity for improved UI and performance.
    -   Mouse control is the same for both Task and MainForm SharpGL.
-   There are better diagnostics in the OpenCVB log – memory usage and threads.
    -   This change permits finding which algorithm pushed the memory limits.
    -   The change will also flag those algorithms which perform poorly.
-   Main project source tree was reorganized to separate functionality.
    -   GLHandler.vb contains all the code to handle the SharpGL.
    -   The jsonClass.vb file now contains all the code related to json.
    -   The NameSpace “OpenCVB” is now extant throughout the main modules.
-   The mechanism to create “shared” algorithms was implemented in this version.
    -   Shared algorithms improve performance when multiple algorithms need them.
    -   The task structure contains the object – see task.contours or task.features.
    -   Shared algorithms are run once for all algorithms using them.
    -   Prior releases had to make sure algorithms were run once per frame.
-   SharpGL can be rendered while preserving the point cloud coordinate values.
    -   Normally SharpGL uses non-linear coordinates to improve presentation.
    -   With linear mode geometries can be rendered in SharpGL.
-   The default value – linear vs. non-linear mode – is still TBD.

![](media/747013ca4f3229c4862a1bf3437c1f3c.gif)

**GL_MainForm:**  *The image rendered in the main OpenCVB form is pretty small but the real problem with using the main form for SharpGL is that the data that resides in the algorithm task needs to be moved to main form which is in a separate thread.*

![](media/207cdd8cf3f63d0aa1c33a92979b0ae4.gif)

**GL_Basics:** *This shows the SharpGL output in non-linear mode. Non-linear mode is more realistic and allows better panning and zoom than linear mode but linear mode point clouds are more easily processed with ReadPointCloud.*

![](media/98c46a1279cee8236171a31aae153b6d.gif)

**GL_Basics:** *This shows the SharpGL output in linear mode. The dimensions are not the same as non-linear mode where the Z dimension is laid in a non-linear fashion to help render to a 2D surface.*

# 2025 August 20th – SharpGL Testing, Memory Usage, SharpGL GIFs.

-   Over 1900 algorithms are included, averaging 36 lines of code per algorithm.
-   More SharpGL testing and improvements.
    -   GL_Lines algorithm displays only the lines found in the RGB image.
    -   See below image for example usage.
-   Memory usage on overnight runs was spinning out of control at larger resolutions.
    -   SharpGL was created for each algorithm.
    -   Neglected to remove the SharpGL form after each algorithm.
    -   Now SharpGL form is only created for “GL_” algorithms.
-   GIF create process now supports the SharpGL output (instead of OpenGL output.)
    -   It is simpler to create a GIF from a VB.Net form than an external window.

![](media/12d32a7111eb4345058e76fbe94830b5.gif)

**GL_Lines:** *The SharpGL point cloud output for just the lines found in the RGB, ignoring the featureless regions where depth data fluctuates.*

# 2025 August 17th – Solution Changes,

-   Over 1900 algorithms are included, averaging 36 lines of code per algorithm.
-   The OpenCVB solution is simplified. The projects included are:
    -   C\# interface for StereoLabs Gemini cameras - CamZed.
    -   C++ algorithms running native C++ - CPP_Native.
    -   Main project for the user interface (includes the camera task.)
    -   VBCLasses project containing all the VB.Net algorithms.
-   Projects removed from the OpenCVB solution:
    -   OpenGL, Orbbec SDK, C++ camera interfaces for OakD, StereoLabs, Orbbec.
    -   OakD project will return soon – Oak-D Pro 4 (pre-order) not yet working.
-   SharpGL interface to OpenGL is now available and working.
    -   SharpGL is a C\# interface to OpenGL included as NuGet package.
    -   SharpGL replaces the existing interface OpenGL C++ project.
    -   OpenGL C++ project has been removed but the project is still preserved.
    -   The OpenGL algorithms are all still present but now reside in XO.vb (obsolete.)
    -   SharpGL algorithm added to read the point cloud.
-   Orbbec C\# interface memory overflow is now fixed with a garbage collection

![A computer screen shot of a room AI-generated content may be incorrect.](media/87f60f3939519c014790037a7221476f.png)

![A computer screen shot of a room AI-generated content may be incorrect.](media/0171c12d9eb57fa5bef168563b3769f6.png)

**GL_Basics:** *The top image is the RGB image while the bottom image is the SharpGL output.*

# 2025 August 10th – Python support, HomeDir, Brick Points.

-   Over 1900 algorithms are included, averaging 36 lines of code per algorithm.
-   Python support retired to the XO.vb module – obsolete.
-   Home directory now passed in from Visual Studio.
    -   It means that support for standalone execution is removed.
    -   This change was prompted by the Runtime-Identifier change with Net8.0.
    -   OpenCVB’s binary is never invoked by itself – it is always part of Visual Studio.
-   Brick points are now a possible Feature source – see Feature_Basics example below.

![A hand reaching out to a room with yellow dots AI-generated content may be incorrect.](media/1c33c86ceee205402f23c9035987ebad.gif)

**Feature_Basics:** *Essentially this is just Sobel output but on a “brick” basis where the bricks define a grid covering the entire image. The highlighted points are the location that has the highest intensity in the Sobel output for that brick.*

# 2025 August 2nd – Camera Lineup, Parallel Lines, Angle Property.

-   Over 1800 algorithms are included, averaging 36 lines of code per algorithm.
-   OpenCVB supports the following cameras:
    -   Intel RealSense cameras – D435i and D455.
    -   StereoLabs ZED 2/2i cameras
    -   Orbbec Gemini series – Gemini 335 and Gemini 336L
-   Support is dropped for the following cameras:
    -   Kinect for Azure – no left/right images and Microsoft stopped production.
    -   Orbbec’s Femto series because they lack left and right images.
    -   Mynt cameras – went out of production and SDK is no longer working.
    -   Intel produced several other cameras that lack an IMU.
-   Luxonis Oak D cameras are in development and should be supported again soon.
-   The TreeView would update rapidly when options changed. The correction is in place.
-   Detecting whether a line is parallel to others in the image is a useful property.
    -   Finding lines which are not parallel to any other is also potentially useful.
    -   The value of such “unparallel” lines is different from those that are.
    -   See “Line_Parallel” for more on how to group lines based on their parallelism.
-   The “angle” property of lines varies from -90 degrees to 90 degrees.
    -   All lines can be mapped into a linear spectrum based on their angle.
    -   Negative angles correspond to negative slopes and positive to positive slopes.
    -   Slope is quite useful but non-linear as a classifier in comparison.

![A hand reaching out to a shelf AI-generated content may be incorrect.](media/122a37f63e6a1e574e5117f07d87a2bd.jpeg)

**Line_Parallel:** *Each group of parallel lines has the same color and group ID (the text at the center of the line.) The “0” group designation is for “unparallel” lines that are of different value because no other lines are parallel to them. Lines that are close together and parallel may have more value. The group 4 lines in blue above are one such example but there are several other examples in the image as well.*

# 2025 July 26th – Longest Line, 100 FPS

-   Over 1800 algorithms are included, averaging 38 lines of code per algorithm.
-   The algorithm for the gravity vector, horizon vector, and longest vector changed.
    -   Longest vector is found for every frame (more work still needed.)
    -   If the longest vector changes, the gravity and horizon vectors change as well.
    -   Otherwise, the gravity and horizon vectors are unchanged, eliminating wobble.
    -   Only the IMU controls the gravity vector.
    -   Tracking the longest line is visualized below.
        -   But running Line_Basics provides a more realistic demonstration.
-   GIF image creation was reviewed and improved for single “dst” image captures.
-   Support for 100 fps on the StereoLabs cameras was restored.
-   Camera intrinsics were overlooked with the Net8.0 conversion – now corrected.
    -   Missing intrinsics data doesn’t impact overnight testing. Zeros work too.
-   Switching cameras during a run is simpler and faster and hopefully bug-free.
-   Oak 4D Pro camera (released June 2025) is not working for me – device not found.
    -   Any suggestions would be gratefully received. Even better: a pull request.

![A shelf and a shelf AI-generated content may be incorrect.](media/fb9e21f35bdf318623a18eb0598fa9c5.gif)

**Line_Basics:** *The longest line in each frame is presented whenever the crosshairs are requested. The result is shown in the upper left image (dst0 or task.color.) If the longest line is unchanged, the gravity and horizon vectors are also unchanged.*

# 2025 July 20th – Gravity Vector, Net8.0 changes, CamZed, Update_All.bat, and Menus.

-   Over 1800 algorithms are included, averaging 37 lines of code per algorithm.
-   The gravity RGB vector is always shown full-length to allow accurate comparisons.
-   The main form for OpenCVB was rebuilt using .Net 8.0.
    -   All the libraries and interfaces were rebuilt as well
    -   This change required a complete review of the OpenCVB’s infrastructure.
-   GIFBuilder has been converted to a .Net 8.0 console application. Update_All.bat fixed.
-   UI_Generator converted to .Net 8.0 console application. VBClasses project updated.
-   The ZED camera support is using the NuGet package instead of a custom VB interface.
    -   The new CamZed C\# class library was able to use NuGet but VB.Net could not.
-   Install script (Update_All.bat) is a one-page file for the SDK and OpenCV downloads.
    -   No more complicated .Net Framework or StereoLabs requirements.
-   GIF algorithms are skipped during overnight testing – only leaf code for visualization.
-   Better default setting for Histogram Bins with an override mechanism if needed.
-   The main form will no longer have menus – they were never being used.
-   For that first build after a download, build the UI_Generate project by itself.
    -   The VBClasses build process uses UI_Generate in a pre-build event.
    -   Alternatively, just build that first build again and UI_Generate will work.

# 2025 July 11th – RGB Line Tracking, Options Close, EdgeLine, DepthRGB, Hull Lines, OpenGL Testing, and Options Changes.

-   Over 1800 algorithms are included, averaging 37 lines of code per algorithm.
-   The “Gravity RGB Vector” is identified and tracked in the RGB image
    -   The line is parallel to the gravity vector if one is available.
    -   The gravity RGB vector is also used to stabilize the gravity IMU vector.
    -   With door or picture frames, nearly identical lines may show instability.
-   OpenCVB will close the app if the algorithm options container is closed.
    -   This was just a convenience feature that felt correct.
-   EdgeLine was reworked to keep track of individual lines and edges.
    -   Lines and edges are identified and loosely tracked.
-   Improved DepthRGB display in dst1 – Depth is shown for the grid rect.
    -   Mouse over DepthRGB (dst1) to see the benefit.
    -   DepthRGB may be displayed as “Colorized Depth” and “Depth Correlations”.
-   Hull lines are lines defined by the edges of a hull.
    -   Simple way to get more feature lines for testing.
-   OpenGL algorithms are exempted from the overnight testing.
    -   Every OpenGL algorithm is a leaf – no algorithms depend on its output.
    -   The goal of the algorithm is simply to visualize the data involved.
    -   Other interactive applications can run during testing runs.
-   RedCloud options are no longer always visible in the algorithm options
    -   The same options appear in algorithm specific forms (typically offset.)
-   StereoLabs support was always present by default but needn’t be.
    -   Use CameraDefines.h to enable support for StereoLabs cameras.
-   Support for Orbbec Gemini 335L is present but Gemini 335 needed an update.
-   OpenCV 4.13 (the current version) was added (PragmaLibs.h updated.)
-   Kinect 4 Azure camera interface dropped – no longer needed.

![A collage of images of a room AI-generated content may be incorrect.](media/62150eb1904b9faf9d77b56958341e17.png)

**Line_Basics:** *The presentation of the gravity vector has been updated. In the upper left image, the longest RGB line - the “Gravity RGB Vector” - is shown in yellow. The longest line parallel to gravity is preferred if available. The lower right image shows all the lines detected in the image while the lower left image shows the longest RGB lines and their age in frames. The upper right image shows the DepthRGB with the depth and depth range under the mouse.*

# 2025 June (1) – FindContours, ReadMe.md, RedCloud Review, Gravity Vector, and Improved Line_Info.

-   Over 1800 algorithms are included, averaging 37 lines of code per algorithm.
-   Each retrieval mode in FindContours now has its own algorithm.
    -   The default retrieval mode is “List” since it has stable results and is fast.
    -   A general purpose Contour_Basics allows selecting all retrieval modes.
-   The “OpenCVB Layout” section of this document was completely rewritten.
    -   Accumulated changes to the user interface were updated and explained.
    -   There are fewer icons in the main toolbar – only the high-use icons remain.
    -   The ‘Recent’ button in the toolbar makes it easy to switch between algorithms.
    -   The ‘A-Z’ button is faster than navigating a combo box with so many entries.
-   Backprojection algorithms were reviewed and updated.
    -   BackProject_Basics_Depth shows each of the depth levels in the histogram.
-   The “RedCloud” algorithms segment an image using a reduced point cloud.
    -   “RedColor” algorithms segment an image using the RGB data.
    -   RedCloud algorithms define segments that have cohesive XY values.
-   The gravity vector from the IMU acceleration vector is slightly unstable.
    -   The first attempt at stabilizing was to use a Kalman filter. Results: not bad.
    -   The second attempt uses the longest RGB line as a proxy for gravity.
        -   If the RGB line end points are not stable, capture the IMU gravity vector.
        -   If the RGB line end points are stable, the gravity vector is unchanged.
    -   The second attempt is currently the active method on each frame.
-   The lpMap (line pointer map) was removed – lines are not easily clickable.
    -   Instead, use the global option debug slider to identify a line for display.
    -   See the “Line_Info” algorithm to display the characteristics of a line.

**![A red and yellow lines on a black background AI-generated content may be incorrect.](media/58382b8918581bfb06ad7ea4a17fd803.gif)Gravity Vector:** *This output shows the typical subtle jitter for the gravity vector. The camera was not moving during this test and shows that the IMU captures the gravity vector but with slight variations from frame to frame. The new Gravity_Basics algorithm in OpenCVB uses the longest line in the RGB image to remove this variability. If the RGB line shows motion, the IMU gravity values are used.*

# 2025 May (1) – Experimental Inputs, Logical Depth in OpenGL, Bricks, Periphery, and RealSense Support.

-   Over 1800 algorithms are included, averaging 38 lines of code per algorithm.
-   A variety of alternative inputs were added for grayscale and color.
    -   Any algorithm can be tested with different input using one click.
    -   Working on edge detection? Prep the input as a sharpened image.
    -   Working on RGB images? Test the algorithm again with HSV data.
    -   Successful experiments would implement the alternative by default.
    -   “Feature Options” (behind the global options) has the complete list.
-   The option for a random palette for all OpenCVB algorithms was removed.
    -   The fixed palette is always preferred and simplifies that user interface.
-   Logical depth is now visualized in OpenGL for comparison with raw data.
    -   Currently, the logical depth is not as good as the original data.
    -   But the potential is there for improvement.
    -   Gradient_Line smoothly applies a linear function to depth data.
-   Overnight testing could fail when image jumped in size – now corrected.
-   Removed the ‘Advice’ scheme – not needed when source is available.
    -   Also, it was too much work to keep up to date.
-   OpenCVB’s matrix of squares (bricks) covers the entire image.
    -   Depth, left/right correlation, features can categorize bricks into groups.
    -   Any problem can be made more manageable when working with bricks.
-   Features of Delaunay polygons that extend beyond the image define a periphery.
    -   The complementary interior defines a fully connected and visible region.
    -   The Feature Coordinate System (FCS) also labels undefined regions.
-   Intel RealSense support now handles the mapping of RGB to left/right images.
    -   Support for multiple Intel cameras broke (name change?). Now fixed.
        -   Multiple cameras can be attached but only one camera will be used.
    -   The Oak-D camera support is still TBD – Oak-D Pro 4 camera not here yet.

![A screenshot of a computer AI-generated content may be incorrect.](media/79f6e36c9e119263741e7b3774c34c3e.png)

**Brick_LeftRightMouse:** *This algorithm translates path of the mouse movement in the color image to the left camera image (below left.) The bricks in the lower left are then translated to the right image (below right.) The reason this was difficult is that the RealSense cameras don’t align the left image with the color image automatically. Only the RealSense cameras require this 2-step translation.*

![A collage of images of hands typing on a computer AI-generated content may be incorrect.](media/136a1ba998770c8ce43cb4b2fd8c0080.png)

**Brick_LeftRightMouse:** *This algorithm is the same as the one above but uses the StereoLabs ZED 2 camera. Here the color image is aligned with the left image while the RealSense example shows that the left camera is not aligned with the color image. For both sample outputs, the lower right image shows the selected cells that match those in the lower left confirming that the translation between left and right cameras is working properly. There are other considerations though but that is TBD.*

# 2025 April (2) – Camera Support, Feature Options, FCS, Logical Depth, and RGB Lines.

-   Over 1800 algorithms are included, averaging 38 lines of code per algorithm.
-   Support added for some new cameras: Orbbec Gemini 335, Orbbec Gemini 336L
-   UPDATE: Camera support for color and left aligned images:
    -   StereoLabs ZED – working – camera aligns color and left view.
    -   Orbbec Gemini 335L – working – camera aligns color and left view.
    -   Orbbec Gemini 336L – working – camera aligns color and left view.
    -   Orbbec Gemini 335 – working - camera aligns color and left view.
    -   Intel RealSense – not working – camera does not align left and color views.
        -   Manual alternative is not working either.
        -   Translation and rotation parameters from the camera look incorrect.
    -   Oak-D Pro 4 – new camera coming – status is TBD.
-   Important feature options are presented the same way as Global and RedCloud.
    -   Feature options also include some options for edges and lines.
-   Feature coordinate system (FCS) interface was reworked with better colors and aging.
-   Included in this version of OpenCVB is the notion of “LogicalDepth”.
    -   LogicalDepth.vb code shows how lines in depth can enhance depth data.
    -   Lines in depth indicate depth that is logically connected and linear.
    -   How to get lines in depth? Structured.vb algorithms have long provided it.
    -   Similar to structured light, structured.vb finds lines in X and Y directions.
    -   Below is an example of showing how lines are found in structured depth.
-   RGB lines can be used in a similar manner to depth lines but with a difference.
    -   RGB lines will not have consistent depth on both sides of the line.
    -   However, an edge in RGB suggests a disparity in depth.
        -   Lines on a wall and painting frame will have similar depth values.
    -   Lines in depth should have coherent depth for the length of the line.
    -   Optical illusions are examples of when lines are not coherent, hopefully rare.

![A collage of images of different colors AI-generated content may be incorrect.](media/8ecc1cc1682ce29ec4ba0dd0fdd6d9db.gif)

**Structured_Core:** *The Structured_Core algorithm is similar in concept to structured light without additional hardware. All RGBD cameras can produce depth lines. By slicing through the point cloud with various increments, this example produces edges which line detection can use to find lines. Note how lines change from one depth image to the next. Depth data is fairly chaotic pixel by pixel. Depth lines should be coherent and will form the basis of ‘Logical Depth’.*

# 2025 April (1) – Camera Support, Depth Edges, FitLine, LineTrack, Gravity/Horizon, TreeView.

-   Over 1800 algorithms are included, averaging 38 lines of code per algorithm.
-   Camera support for color and left aligned images is still under development.
    -   StereoLabs ZED – working – camera aligns color and left view.
    -   Orbbec Gemini 335L – working – camera aligns color and left view.
    -   Intel RealSense – not working – camera does not align left and color views.
        -   Manual alternative is not working either.
        -   Translation and rotation parameters from the camera look incorrect.
    -   Oak-D Pro 4 – new camera coming – status is TBD.
-   Depth data is filtered with HighVis_Basics to produce higher quality values.
    -   Grid cells can’t be neighbors if disparities are significantly different.
    -   Disparity differences can define coarse edges in depth data.
    -   Grid cells with a large range of depth values also define depth edges.
    -   GIF below demonstrates that poor depth data is trimmed carefully.
-   Disparity differences can also be too close – left and right can’t be the same.
    -   If the change is less than 1 pixel, the depth data is not reliable.
    -   Left and right images are unlikely to have identical locations.
-   FitLine_Basics was redone and is now more accurate.
    -   Fit the points with an ellipse and use the ellipse center line.
    -   The result is better than either FitLine or Eigen functions.
-   Line tracking algorithms were reworked and simplified.
    -   RedColor is used to identify the lines and track their movement.
-   Gravity and Horizon were incorrect at the highest resolution.
    -   FindNonZero found plenty of pixels but all in row zero. Problem fixed.
-   The TreeView output was updated to show detail of algorithms \< 1% utilization.

![A collage of images of people and objects AI-generated content may be incorrect.](media/ef742dde619d31abd091b68a09c09191.gif)

**GridCell_Info :** *This algorithm displays the contents of the grid cell under the mouse cursor but the goal here is to show the impact of removing cells that have a high difference from the disparity in the cell’s neighbor. Essentially, this is edge detection in depth and removes the cells with unreliable depth data – visible in the upper right image..*

# 2025 March (4) – Azure Support, FitLine, Eigen, Feature, XO, Depth Correlation Updates.

-   Over 1700 algorithms are included, averaging 38 lines of code per algorithm.
-   Support for the Azure Kinect 4K is present but \#ifdef’d out
    -   With no left/right image, the support required too much special handling.
    -   Look for the “AZURE_SUPPORT” \#define to reenable support.
    -   “Update_All.bat” install script has commented the Azure-related commands.
    -   The camera order has changed in the OpenCVB options.
        -   Stereolabs’ camera is the preferred camera at the top of the list.
        -   Orbbec Gemini 335L next best.
        -   Then Oak-D because Intel cameras are no longer available.
        -   Mynt is still there in case anyone still has their camera.
-   The FitLine and Eigen algorithms were reviewed and updated.
    -   There are 2 ways to fit a line to raw data but FitLine looks a little better.
-   All the Feature methods use the motion mask to manage the features.
    -   Feature points are stable across frames if there is no motion nearby.
-   Algorithms starting with “XO_” are obsolete and not included in overnight testing.
    -   The obsolete examples should still run when selected manually.
    -   The obsolete examples can be useful for exploring OpenCV API’s.
-   The “Depth Correlation” view includes the depth shadow – see below.
    -   This change is motivated by the difficulty of tracking motion in depth.
        -   There is simply too much volatility in regions with zero depth.
        -   Grid cells impacted by RGB motion get a full update.
        -   And so do grid cells with low depth correlation (unreliable depth.)
    -   RGB motion cannot cover changes in depth shadow from foreground objects.

![A screenshot of a computer AI-generated content may be incorrect.](media/5411c8981b86e7bc4779ffc60a1a8309.gif)

**RedColor_Basics :** *The same algorithm as the previous update but with the improved representation of the “Depth Correlation”. The upper right image is switching between the previous view of the depth and the current one that includes the regions with zero depth.*

# 2025 March (3) – RedColor, Color8U, Left/Right Mean Subtraction, Gravity/Horizon, and the XO algorithms.

-   Over 1700 algorithms are included, averaging 38 lines of code per algorithm.
-   For RedCloud output, a coloring scheme may be selected in the RedCloud options.
    -   Color a cell with its mean color, tracking color, or depth color.
    -   The different color schemes are explained in the first GIF below.
    -   Background cells are unchanged even with hand motion.
    -   Only cells within the motion mask need to be updated.
-   This image stability is visible in all of the Color8U algorithms input to RedColor.
-   All the Color8U algorithms (currently 10) use motion to increase image stability.
    -   BackProject_Full – a histogram setting determines the classifications.
    -   Bin4Way_Regions – 4 color categories based on brightness.
    -   Binarize_DepthTiers – 1-meter categories of depth.
    -   EdgeLine_Basics – (default) uses lines and edges to segment the image.
    -   Hist3DColor_Basics – categorize color pixels with a 3D histogram.
    -   KMeans_Basics – a standard K-Means approach to segmentation.
    -   LUT_Basics – use a look-up-table (LUT) to categorize each pixel.
    -   Reduction_Basics – reduce each pixel mechanically to group similar pixels.
    -   PCA_NColor_CPP – principal component analysis on the entire image.
    -   MeanSubtraction_Gray – grayscale separation into 2 classes.
-   Mean subtraction of the left and right images may show depth correlation better.
    -   The first GIF shows the previous version with no mean subtraction.
    -   The second GIF below alternates with and without mean subtraction.
    -   Check global option ‘Mean Subtraction on Left/Right Images’ to see the impact.
    -   Mean subtraction of left and right images finds better correlation values.
    -   Much more testing is needed to confirm the value of using this approach.
-   The gravity algorithms were reworked to use OpenCV’s FindNonZero.
    -   The gravity vector is defined in image coordinates to help find vertical lines.
    -   Similarly, the horizon vector is used to find horizontal lines algebraically.
    -   The gravity vector can be smoothed with Kalman. See Gravity_Basics.
        -   But it is too slow on some configurations with a moving camera.
-   The XO algorithms are experimental or obsolete algorithms that are not active.
    -   Separating inactive code is simpler and preserves the possibility of later use.
    -   Obsolete gravity and horizon algorithms prompted XO’s creation.
    -   More and more algorithms will migrate there to simplify the source tree.

![A collage of images of a person in a room AI-generated content may be incorrect.](media/101f64619a75aba05208e888ad0a7c16.gif)

**RedColor_Basics :** *The different display options for RedCloud output are shown above. The radio button insert shows the current selection. The 3 options are to display the mean color of the cell, a tracking color, and the depth color. The tracking color is a highly visible color that is randomly assigned to a cell so that it may be visually tracked from frame to frame.*

*![A collage of a room with a door and a room with a computer screen AI-generated content may be incorrect.](media/44e21adc20afb2490256ae9462af7c56.gif)*

**Mean Subtraction :** *An OpenCVB global option enables using mean subtraction of the left and right images as input to the depth correlation. The featureless regions are highlighted in the output with solid red regions. The improved output is available to all algorithms in the DepthRGB Mat. More testing is needed to confirm the value of mean subtraction.*

# 2025 March (2) – GridCell and EdgeLine Updates.

-   Over 1700 algorithms are included, averaging 38 lines of code per algorithm.
-   GridCell_Regions expands the horizontal and vertical rectangles.
    -   Grid cells with consistent depth are combined to build contours.
    -   The contours define and track the objects in the image – see below.
-   EdgeLine algorithms: combine edge and line detection to form ‘edgelines’.
    -   Previously labeled EdgeDraw after OpenCV’s EdgeDrawing API’s.
    -   EdgeDrawing names are used to access OpenCV C++ code.
    -   EdgeLine may be a better name to encapsulate how it works.
-   EdgeLine algorithms update the edgeline output only where motion occurs.
    -   Algorithms using EdgeLine_Basics see more consistent output.
    -   Motion-filtered RGB input to EdgeDrawing API’s are not consistent.
    -   Motion-filtering the edgeline output solves the problem.
    -   Motion-filtering EdgeLine output impacts many other algorithms.
-   RedColor_Basics updates only cells that intersect with the motion mask.
    -   Result for all ‘RedC’ algorithms appears much more stable.
    -   ‘RedC’ algorithms are the first to benefit from EdgeLine improvements.
-   Selecting ‘Display Cell Stats’ RedCloud option can be toggled on and off.
-   OpenCVB option to use a fixed or random palette is available.
    -   A fixed palette produces consistent colors across different runs.
    -   Random palettes will be consistently used with all algorithms in a run.
-   Feature_Agast options improve the quality of feature points found.
-   ShowPalette no longer requires normalizing – saves a matrix multiply.
-   Emax_Basics example builds a special set of grid rectangles for its use. ![A collage of a person sitting in a chair AI-generated content may be incorrect.](media/f1f77b330a4a33ef4872f2d85b188b32.gif)

**Connected_Contours :** *Each region contains grid cells and neighbors which are at approximately the same depth. For instance, the green cell in the lower left consistently defines and tracks the person seated at the desk (our humble programmer.) The wall and painting are tracked as well. The lower right image confirms this by showing the color image and the contours using OpenCV’s AddWeighted method. The tracking color of the wall changes because it is shrinking in size as the contours are colored by order of size in this version.*

# 2025 March (1) – GridCell Updates and Task Algorithms.

-   Over 1700 algorithms are included, averaging 38 lines of code per algorithm.
-   The GridCell algorithms continue to grow with this version of OpenCVB.
    -   GridCell_Basics is run on every frame – it is a “task algorithm”.
    -   Grid cells are available to all algorithms with the task.iddList variable.
    -   The depth between neighboring grid cells defines a connection.
    -   Connections are made both vertically and horizontally = see below.
    -   The depth difference option is available in the global algorithm options.
        -   “Depth Difference Threshold” is specified in centimeters.
-   The complete list of task algorithms:
    -   Depth_Colorizer – to build the depth RGB image.
    -   Gravity_Horizon – to provide the gravity and horizon lines.
    -   Grid_Basics – to define the current grid cells and size.
    -   GridCell_Basics – to get depth correlations for grid cells.
    -   IMU_Basics – to gather the current IMU data.
    -   IMU_GMatrix – define gravity conversion matrix for the point cloud.
    -   Motion_Basics – to build the motion mask for the current frame.
-   The PixelViewer form can be closed by both the caption box and ![A blue microscope with black outline AI-generated content may be incorrect.](media/0e674efad2384a95fc65c5a030a951fd.png)button.
-   The AddWeighted_Basics algorithm was moved into a function – reduced code.

![A screenshot of a computer AI-generated content may be incorrect.](media/2afba4eb6b4ad7bf708ac64a8673a002.png)

**GridCell_Connected:** *Grid cells in the lower left image are combined horizontally if their depth is within X centimeters. The same grid cells are combined vertically in the lower right image. Note that many grid cells are not combined vertically or horizontally. Their depth is not close to any of their neighbors. Cells with no depth can still be combined. An example is the entire vertical column at the left of the image where there are no depth values.*

# 2025 February (3) – Azure Kinect Support, Depth Views, Grid Cells, and Motion Detection Compromise.

-   Over 1700 algorithms are included, averaging 38 lines of code per algorithm.
-   The Azure Kinect camera updated with access to extrinsics but it is limited.
    -   K4A is a TOF (time of flight) camera and does not use disparity to get distance.
    -   Confirming depth with correlation is not possible with RGB/Left images.
-   For all other cameras correlations are possible with left and right images.
    -   Grid cells define a region which is used to confirm depth quality.
    -   Correlations measure the quality of the depth data in each grid cell.
    -   Some cameras provide left images already aligned with the RGB image.
    -   But some need calibration parameters to connect RGB and left grid cells.
        -   See below where the Intel D435i RGB is mapped into the left image.
    -   The mouse cursor displays the correlation coefficient and depth pixel density.
    -   Oak-D correlations are under development – new camera coming.
-   A reworked Motion_Basics uses grid cells to manage the motion rectangles.
    -   Motion detection is a compromise that successfully removes artifacts.
        -   Motion_BasicsValidate shows the small size of any differences.
    -   The default setting is to update pixels only where motion is detected.
        -   Motion-filtered RGB means no heartbeat update to the RGB image.
    -   Pixels undisturbed by motion provide algorithm results that are more stable.
    -   The “Depth Correlation” view is a good example of motion detection usage.
        -   The upper right image (below) shows the red grid cells are stable.
    -   Motion detection is a compromise that preserves grid cell color.

![A screenshot of a computer AI-generated content may be incorrect.](media/ba9fd91a6d76b04326a7171aad5eb8ba.gif)

**GridCell_LeftToColor :** *The upper right image rotates between the 3 different representations of the depth data. The correlation coefficients are highlighted in red for grid cells that have 90%+ correlation between the left and right images, indicating that the grid cell is highly visible to both the left and right and is likely to have excellent depth data. The lower left image shows the corresponding points for the RGB data (upper left.) The camera is the Intel RealSense D435i and the left image is grayscale. The lower right image has the same pixels highlighted as the lower left image but is more readable. Use the mouse cursor to display the correlation coefficient and pixel count percentage for the grid cell under the cursor.*

# 2025 February (2) – More ‘QuadDepth’ improvements, OpenGL display changes, Extrinsics/Intrinsics, and Connected Depth Cells

-   Over 1700 algorithms are included, averaging 38 lines of code per algorithm.
-   The ‘QuadDepth’ display was configured to show OpenGL quad’s or rectangles.
    -   Neighboring quads are now connected if they are close in depth.
    -   The depth difference between cells is controlled with a global option.
    -   Neighboring cells are connected both vertically and horizontally.
    -   Cursor movements display the depth of any cell under the cursor.
    -   Debug feature: cell depth is displayed even when the algorithm is paused.
    -   NOTE: right-click the mouse to avoid updating the mouse move value.
        -   Move off the screen while holding the right-click to debug.
-   OpenGL algorithms typically display raw point cloud data.
    -   OpenGL_QuadCompare algorithm displays multiple views.
    -   Options include raw point cloud, flat or connected depth cells.
    -   Connected depth cells remove many artifacts or floating points.
    -   Horizontal and vertical connections build a solid OpenGL ‘weave’.
-   The fx, fy, ppx, and ppy intrinsics were reviewed and tested.
    -   More testing is needed to handle cameras without RGB=Left View.
    -   All camera parameters are adjusted using the ratio of working to capture size.
-   DepthCell.vb has a correlation map showing a cell’s left to right image correlation.
    -   A mask of the heat map can be thresholded to isolate high quality cells.
    -   The correlation relies on intrinsics and extrinsics for the left and RGB.
        -   And the corresponding values for the left and right images.
        -   For the StereoLabs Zed 2/2i cameras, the RGB equals the left image.
-   The MYNT EYE camera support is not working. Rebuilding the SDK doesn’t work.
    -   This camera is highly desirable because the RGB and left cameras are the same.
    -   Any MYNT developer’s pull request would be gratefully received.
-   The upper right image (DepthRGB) shows the % depth pixels that are present.

![A room with a door and a room with a door and a room with a door and a room with a door and a room with a door and a room with a door and a room AI-generated content may be incorrect.](media/3f3ecad62a60b44ad5d6a8aefa22c148.gif)

**OpenGL_QuadCompare :** *The 3 different OpenGL display formats are shown above – raw point cloud, flat depth cells, and connected depth cells. The flat and connected depth cells are OpenGL quads, not points. Note that the connected depth cells remove some of the floating artifacts. A cell is connected to its neighbors if their depths are within X centimeters (controlled with an option.) The depth cells are then connected in vertical and horizontal directions to produce a solid appearance.*

# 2025 February (2) – Improved Depth Display, QuadDepth OpenGL display, and Left/Right Cameras.

-   Over 1700 algorithms are included, averaging 38 lines of code per algorithm.
-   The least used OpenCVB image is the “DepthRGB” in the upper right.
    -   The option to use an alternative “QuadDepth” view is now the default.
    -   The QuadDepth image is built using the motion mask in OpenCVB.
        -   Depth data does not change unless there is motion in that cell.
    -   The conventional display of the depth is still optionally available.
    -   Mouse movement in the OpenCVB app will display a location’s depth.
    -   The first GIF image below shows what the output looks like.
        -   The depth histogram also shown is for the selected cell.
        -   See the “Ideal_CellPlot” algorithm for more information.
-   OpenCVB’s OpenGL interface uses ‘quads’ to display rectangles for the scene.
    -   Each quad has depth and color and the size is controlled with an option.
    -   See the second GIF display below to understand this further.
-   The left and right camera images are now always provided in grayscale.
    -   Some cameras could not provide color left and right images.
    -   The limited use of left/right images didn’t require color values.
        -   Future uses may require color so some cameras may be limited.
    -   Using grayscale images is more efficient for now.

![A collage of a person sitting in a chair AI-generated content may be incorrect.](media/d86045cbdc8eec1c2a5fe6965baf507e.gif)

**Depth Display:** *The option to use the “QuadDepth” display is shown in the upper right image. In this example, the “Ideal_CellPlot” algorithm shows the histogram of the cell’s depth data at the selected location while the mean depth for the cell is shown in the upper right image. The mouse controls which histogram and mean depth are shown. Mouse movements in all algorithms will show the mean depth in the upper right image for the cell under the mouse.*

![A person sitting in a room AI-generated content may be incorrect.](media/638f97600747b1130688e8061ae8af2a.gif)

**OpenGL_QuadDepth:** *The “QuadDepth” data that is displayed in the upper right image of the output for all the algorithms can also be displayed in OpenGL. Each cell is provided to OpenGL as a quad that is always filled with the mean color for the cell. In the sequence above the last image is zoomed sufficiently to show that each cell is a rectangle, not a set of points.*

# 2025 February (1) – Ideal Depth, OpenGL Triangles and Quads.

-   Over 1700 algorithms are included, averaging 38 lines of code per algorithm.
-   Ideal depth is a grid of cells that each contain a healthy amount of depth data.
    -   The cell’s pixels are clearly visible in both left and right camera views.
    -   The cell contents enable the census algorithm to match pixels.
    -   The more pixels that are matched the better the estimate of depth.
    -   Ideal_Basics matches left and right cells converting depth to disparity.
        -   disparity = baseLine \* (focal length) / depth.
-   The OpenGL interface to OpenCVB has improved triangle and quad support.
    -   Triangles used in OpenGL can be shaped – see OpenGL_IdealShapes.
    -   OpenGL Quads can better represent the depth data – OpenGL_QuadIdeal.
-   OpenCVB’s OpenGL interface is improved with multiple point cloud buffers.
    -   3D cameras produce point clouds that are always slightly different.
    -   Combining them produces a more solid appearance to the 3D model.
    -   Below is an example showing the cosmetic difference.![A screen shot of a computer screen Description automatically generated](media/9f00f342149e24119ff7d554df58f31b.png)  ![A screenshot of a computer screen Description automatically generated](media/ed0ab23749b244a50928420654c93c93.png) **OpenGL Multiple Buffers:** *Since the point cloud is different on every frame (depth is only an approximation), using multiple buffers allows a cosmetic improvement to the appearance of the point cloud. The first point cloud is what one buffer looks like while the next uses the last 10 frames. The frame rate for this example was 60 fps at 320x240 with significant magnification (approximately 4X.)*

# 2025 January (2) – GifBuilder updates, RGBFilter, Ideal Depth.

-   Over 1700 algorithms are included, averaging 38 lines of code per algorithm.
-   GifBuilder was reworked to capture the screen and use AnimatedGif.
-   RGBFilter available once again – filter the RGB input with any of the following:
    -   Blur, Brightness, Contrast, Dilate, Erode, Laplacian,
    -   Mean Subtraction, Sharpen, or White Balance.
    -   RGBFilter is another task algorithm – run without explicit invocation.
-   Cells with “ideal” depth are those filled with depth pixels.
    -   Ideal depth is clearly visible from both the left and right cameras.
    -   See example output below showing cells covering the depth image.
-   TreeView form was moved into the VBClasses where the data is produced.
    -   Allowed code to be simpler in the Main form.
    -   TreeView button is no longer needed in Main

**![A collage of a person using a computer Description automatically generated](media/1c85b62195b05f1695bd32287f38eac8.gif)Depth_Ideal :** *The cells marked in the lower left image have ideal depth data with a high percentage of the cell’s pixels containing a depth value. By definition, they are the cells which are fully visible in both the left and right cameras. The lower right image is the point cloud containing only the cells that have ideal depth. The lower right image is filtered by motion – only the cells in the motion mask are updated on each frame.*

# 2025 January (1) – Task Algorithms, Upgraded Toolbar, and Translation Changes

-   Over 1700 algorithms are included, averaging 38 lines of code per algorithm.
-   ‘Task algorithms’ are algorithms that are present on every run.
    -   Task algorithms provide IMU, motion, horizon, lines or grid elements.
    -   Task algorithms appear in the TreeView output (previously hidden.)
    -   Click on any TreeView element to see the algorithm’s contribution.
        -   See the GIF image below to see how this works.
    -   Overhead for each algorithm is also shown along with wait times.
    -   Inactive algorithms, when selected, display the ‘inactive’ message.
    -   Selecting a TreeView entry shows algorithm’s images, labels, and TrueText.
-   OpenCVB now uses OpenCV 4.120, the latest version of OpenCV.
    -   Delete the opencv directory and run ‘Update_All.bat’ to upgrade.
    -   To remain on an older version, update the ‘PragmaLibs.h’.
-   OpenCVB’s focus is VB.Net as translation to C\# and C++ has been demonstrated.
    -   If a C\# or C++ version is needed, use the CodeConvert.ai to translate.
    -   OpenCVB’s interface to CodeConvert.ai has been removed.
        -   CodeConvert.ai reworked their website and it stopped working.
    -   Drop in algorithm count is due to removing translations.
-   Options derive from ‘OptionParent’ and no longer count as algorithms.
    -   Options previously derived from TaskParent and added to the count.
    -   Moving to OptionParent means options do not add to the algorithm count.
-   The Line.vb algorithms were reviewed and improved.
    -   Lines, once computed, are available to all algorithms in task.lpList.
    -   Lines are now collected across frames using the motion mask.
        -   Existing lines in regions with motion are tossed.
        -   New lines in the motion mask are all added.
-   Lines are built from the left and right images with binocular cameras.
    -   Lines provide the ability to confirm distance with uniform depth patterns.
-   Main form toolbar is simplified – low use buttons removed or hidden.

    ![A screenshot of a computer Description automatically generated](media/735c5a18926512d0a5a4d53d3208e5da.png)

-   The “Blue +” button to create new algorithms is limited to VB.Net, C++, and OpenGL.
-   Left and Right camera images are now always grayscale (some were color.)

![A screenshot of a computer Description automatically generated](media/893013652590bac4fa557520f5e13e4a.gif)

**RedColor_Basics:** *OpenCVB’s TreeView (at right above) shows all the algorithms that contribute to the output of the requested algorithm – in this case RedColor_Basics. As each algorithm in the tree is selected, it is highlighted (albeit faintly here) and the algorithm’s output is shown (now including labels and TrueText.) This capability enables a further understanding of how the algorithm was constructed. Task algorithms are run on every frame and are included in the TreeView even though they are not explicitly called by the algorithm. The frame rate of 99 fps is not a mistake. The Stereolabs Gemini camera runs at that rate when capturing 640x480 images.*

# 2024 December - Recent Changes

-   Over 3800 algorithms are included, averaging 33 lines of code per algorithm.
-   RedCloud algorithm default is the featureless option (see below.)
    -   RedCloud results are validated using the tracking color and age.
-   The OpenCVSharp NuGet packages updated to November 2024 release.
-   Install batch file was updated to accommodate .Net 3.5 optional install.
    -   The prompt to confirm reading the instructions now works.
    -   Any install problems are high priority given these recent changes.
-   Features are widely used and are prepared for each frame automatically.
    -   Feature points are provided in both floating point and integer form.
-   Added a new version of KNN that normalizes the input data – KNNorm.vb.
-   Motion mask is used in more algorithms.
    -   Every frame is optionally updated only where motion occurred.
    -   Motion-filtered images for depth and RGB are provided by default.
    -   Motion-filtered depth is not robust but RGB images are.
-   Switching to the C++ versions of the camera interfaces is simplified.
    -   Toggle which camera interface is active using comments in getCamera.
-   A global option determines if the depth is truncated at ‘maxDepth’.
    -   The default is to use all data but it can optionally truncate at X meters.
-   The ‘A-Z’ group selection button is moved next to the algorithm combo box.
    -   See the toolbar in the latest screen shot below for RedCloud_Basics.
-   Managed C++ code is disabled for now – not much value there for now.

![A collage of images of a room Description automatically generated](media/928bcaa46cf6abb063c6824e9086236e.png)

**RedCloud_Basics:** *All the RedCloud algorithms were reviewed and updated with the best segmentation approach – featureless regions built with Edge_Draw. Edge_Draw is an OpenCV user-contribution and is better suited to detect edges than conventional alternatives like Canny or Sobel. The lower left image uses the mean color of the pixels to paint the entire cell while the lower right image uses a random ‘tracking’ color which changes whenever the cell is split or lost.*

# 2024 November - Recent Changes

-   Over 3800 algorithms are included, averaging 33 lines of code per algorithm.
-   The “A-Z” toolbar button in the main OpenCVB form allows speedy group access.

    ![A screenshot of a computer Description automatically generated](media/8bd14069867788f1015665e05437d1b9.png)

    -   There are almost 250 algorithm groups in OpenCVB – now one click away.
    -   Clicking in the grid will land at the first algorithm in that group.
    -   Faster than scrolling through the entire list of available algorithms.
-   **Line_VerticalHorizontal** identifies gravity and the horizon in the color image.
    -   Combined with scene motion, identified lines are retained across images.
-   Camera Motion can be identified in the color, left image, and right image.
    -   With 3 votes, camera motion can be verified.
-   Horizon can move below the plane causing pointcloud Y-values above/below zero.
    -   Horizon is now also computed as the perpendicular of the gravity vector.
-   This update includes the Feature Coordinate System – see FCS_Basics.
    -   A Delaunay map is created using the features or lines.
    -   The map allows tracking the area even as the features come and go.

![A screenshot of a computer AI-generated content may be incorrect.](media/baa4fe87e03b08a9288be72cdb139c41.png)

**“A-Z” Toolbar Button:** *There are almost 250 algorithm groups in OpenCVB, and the “A-Z” toolbar button allows speedy access to any of the groups. Clicking on any of grid entries will land the user at the first algorithm in the group in one click. Accessing a specific algorithm in that group is a click away in the pulldown of the list of available algorithms.*

# 2024 October - Recent Changes

-   Over 3800 algorithms are included, averaging 33 lines of code per algorithm.
-   Motion_Basics was replaced with another motion detection algorithm.
    -   The mean color of each cell in the grid is compared to previous values.
        -   A 3D distance in color value is used to compare cells.
    -   If color distance is more than a fixed value, the grid cell has motion.
        -   Computation is low-cost and could have been done decades ago.
    -   Rather than more resolution, low resolution proved beneficial.
    -   Often just individual pixels are different – Motion_Basics output below.
    -   Depth, color, and left may be constructed using motion (not right image.)
    -   Motion_Basics works well across all cameras at all resolutions.
        -   Grid cell size is approximately the same in all cases by default.
    -   A motion mask allows other algorithms to simplify their actions.
-   An OpenCVB global option determines using raw or motion-constructed images.
-   The C++ camera interface for StereoLabs ZED 2i cameras was restored.
    -   The VB.Net interface was not getting 100 FPS initially.
    -   After the restored C++ interface got 100 FPS, the VB.Net interface did too.
    -   Both interfaces are available in the OpenCVB options list of cameras.
-   VB.Net version for Kinect 4 Azure camera support is under development.
    -   Existing Kinect 4 Azure camera support is still available (UI updated).
-   A long-term heartbeat is now available in the task structure.
    -   Images can now be refreshed on a heartbeat or heartbeatLT (long term).
    -   The current heartbeat is 1 second while heartbeatLT is X seconds.

![A screenshot of a computer Description automatically generated](media/9ecba468726fbd244063f9aa06417b68.gif)

**Motion_Basics:** *This motion detection algorithm uses low resolution mean values to find areas that contain motion. The top left image is the original color image (optionally overlaid with cells where motion was detected) while the image below left was constructed from an earlier image (often seconds earlier) updated with cells containing motion. The depth data in the upper right is also a composite of an earlier image and the latest depth where motion was found. The image in the lower right is the difference between the current color image and the image in the lower left. The implication is that almost all motion has been detected and no artifacts have been generated in the color image. Depth data has visible artifacts and will require more work because of shadow.*

# 2024 October - Recent Changes

-   Over 3700 algorithms are included, averaging 33 lines of code per algorithm.
-   A magnifying glass button was added to the OpenCVB toolbar.
    -   It can provide more detailed images of specific areas.
    -   The detailed image is dynamic – it will update with each frame.
    -   Mouse movement can provide additional flexibility while drawing.
    -   Magnifying glass works with static images while OpenCVB is paused.
    -   All 4 images show the rectangle being drawn.
    -   See the image below to help find the new button and see sample output
-   Contrast: the “Microscope” button (next button to the left) provides more detail.
    -   The “Microscope” button produces hex values.
-   OpenCV’s machine learning algorithms are all available in a single algorithm.
    -   See the second sample image below; image segmented by features.
    -   ML_Basics mimics the functionality of OpenCV’s Points Classifier example.
    -   Feature detected is the output of the Laplacian 2nd derivative.
        -   Laplacian was more accurate in finding the edge pixels for use with ML.
-   C\# algorithms are now always configured in Release mode.
    -   Makes it easier to determine the value of optimizing any VB.Net algorithm.
-   Orbbec Gemini 335L is now working at 30 FPS.
    -   Care must be taken to make sure it is on a USB-C port.

![A screenshot of a computer Description automatically generated](media/a4ade72e34b35bccb590db3772e53c01.png)

**Magnifying Button on Toolbar:** *First draw a rectangle in any of the 4 images then click the magnifying button to see a 5X copy of the region. The magnified image will reflect the image contents where the rectangle is drawn but all 4 images while outline the rectangle.*

![A collage of images of a person Description automatically generated](media/5ae98b4c9bad297ea31fd84e9da6369a.png)

**LowRes_MLColorDepth:** *ML is used to segment the image between cells with features (Laplacian edges) and featureless regions. The lower left image shows all the cells with featureless areas while the lower right image shows the more work is required for complete segmentation. The ML input is color and depth.*

# 2024 September (2) - Recent Changes

-   Over 3700 algorithms are included, averaging 33 lines of code per algorithm.
-   This OpenCVB update is focused almost exclusively on the camera interfaces.
-   More camera interfaces are using only VB.Net to capture image and IMU data.
    -   The C\# wrappers require the .Net Framework 3.5 – install had to change.
    -   The install script now prompts to install .Net Framework 3.5.
        -   Update_All.bat script will fail if 3.5 is not present on the system
    -   In addition, the StereoLabs C\# interface needed to be reworked.
        -   The same name is used for a struct, an enum, and a variable.
        -   VB.Net does not allow this kind of overlapping names.
        -   The replacement C\# code is now included in OpenCVB.
-   StereoLabs Zed 2i, Intel RealSense D455/D435, Orbbec Gemini 335L updated.
    -   VB.Net camera interfaces are much easier to debug.
    -   The Oak-D interface was rewritten but in C++
        -   Oak-D has no C\# interface (resource constraint at Luxonis)
        -   The OpenCVB interface is no longer troublesome.
    -   All previous C++ interfaces are still there and toggled with \#ifdef
-   All the camera interfaces collect all image and IMU data.
    -   Color, Left, Right, Point Cloud, and IMU acceleration/angularvelocity.
    -   OpenCVB is focused on using only the image and IMU data.
-   The StereoLabs camera “tearing” problem is not a software issue.
    -   The cable provided by StereoLabs needed to be replaced.
-   The Mynt camera interface is unchanged. Company went out-of-business.
-   The Kinect for Azure camera is also unchanged – VB.Net version is coming soon.
    -   The K4A camera doesn’t use the same technique as all the other cameras.
        -   No left and right images are available.
    -   K4A is useful and it is the most accurate at close range.
-   Microsoft has discontinued their K4A camera.
    -   The K4A equivalent camera is available as Orbbec Femto Bolt.
-   Other news: another way to capture motion is in Motion_FromEdgeColorize
    -   Blue is motion, Red is not (see below.)

![A collage of images of people sitting in a chair Description automatically generated](media/3b7fcfc4ec5dcb8a1619770024131824.png)

**Motion_FromEdgeColorize:** *This algorithm uses the palette to identify motion. Motion is blue while red is not.*

# 2024 September (1) - Recent Changes

-   Over 3700 algorithms are included, averaging 33 lines of code per algorithm.
-   Visual Studio Community Edition upgraded to 17.11.
    -   Better debugger, improved GC, async programming, C\# 11.0.
    -   Visual Studio 2022 Version 17.8 or later is now required.
        -   In Visual Studio, click Help/Check for Updates to get updated.
    -   Translation (see below) needed to use the latest web control.
    -   ComboBox control has improved navigation through the list of algorithms.
-   Translating algorithms to C\#, C++, or VB.Net is streamlined and improved.
    -   Translation is now integrated into the main app – not a separate .exe.
    -   CodeConvert.ai is used but the process is now more automated.
    -   CodeConvert.ai is free for 10 conversions per month. Otherwise, \$10/month.
-   “First make it work, then make it better.” C\# support shares more VB.Net code.
    -   The interoperability of C\# and VB.Net is nearly invisible.
    -   The CPP_Managed project is now a C++/CLR (managed code.)
        -   The translation from C\# to C++ is working but largely untested.
    -   The previous C++ algorithms are in a project called CPP_Native.
-   CPP_Managed is a C++/CLR Visual Studio project.
    -   C++/CLR or Managed C++ is rarely encountered but suitable for OpenCVB.
-   The breakdown of the suffix for any algorithm is as follows:
    -   AddWeighted_Basics – no suffix means it is VB.Net code only.
    -   AddWeighted_Basics_CS – “_CS” suffix means it is C\# code only.
    -   AddWeighted_Basics_CPP_VB – native C++ code with a VB.Net wrapper.
    -   AddWeighted_Basics_CPP_CS – native C++ code with a C\# wrapper.
    -   AddWeighted_Basics_CPP – “_CPP” suffix means C++/CLR (managed) code.
    -   AddWeighted_Basics_CC – “_CC” suffix means it is Native C++ code.
        -   “_CC” algorithms are called using PInvoke to native dll’s.
    -   Python examples end in .py. PyStream algorithms end in …_PS.py.
-   Install directory with spaces now supported. Resolved – September 5th.

![A screenshot of a computer AI-generated content may be incorrect.](media/f6ba222f6e73f72f51d0b393a6b64bae.png)

**Code Translator:** *The user interface for the code translator is shown above with the results shown at the bottom. It is invoked in OpenCVB using the ![](media/8b48ec3d1b9bd1ac4814aa20cb031b96.png) button in the main panel. The web page for CodeConvert.ai is contained in a WebView2 control. The ComboBox and buttons at the top provide a 3-step process to translate the code. Here AddWeighted_CS, a C\# algorithm, is translated to C++. The formatting of the results is corrected when the code is pasted into CPP_Managed.cpp.*

# 2024 August - Recent Changes

-   Over 3700 algorithms are included, averaging 37 lines of code per algorithm.
-   OpenCVB has been upgraded to the latest version of OpenCVSharp.
    -   Mat’s initialized with a data pointer now use “FromPixelData”.
    -   Mat’s initialized with a Scalar now use “cv.Scalar.All(\<value\>)”.
    -   It was a lot of typing and testing to make this change.
-   The breakdown of the suffix for any algorithm is as follows:
    -   AddWeighted_Basics – no suffix means it is VB.Net code only.
    -   AddWeighted_Basics_CS – “_CS” suffix means it is C\# code only.
    -   AddWeighted_Basics_CPP_VB – C++ code with a VB.Net interface.
    -   AddWeighted_Basics_CPP_CS – C++ code with a C\# interface.
    -   AddWeighted_Basics_MT_CPP – a multi-threaded C++ algorithm.
    -   AddWeighted_Basics_CPP – “_CPP” suffix means it is C++ code.
        -   For “_CPP” the call to C++ is from the VB.Net infrastructure.
        -   A deprecated AI-generated mechanism was used to build these.
        -   This interface will be reworked soon to make it more general.
    -   Python examples end in .py. PyStream algorithms end in …_PS.py.
-   The jump in algorithm count is due to the addition of AI-generated C\# algorithms.
    -   Almost all the VB.Net algorithms have been converted to C\#.
        -   Fewer C\# algorithms than VB.Net because Options are VB-only.
    -   The average lines per algorithm jumped as well from 31 lines to 37.
-   Microsoft’s CodeConverter.ai translated the VB.Net algorithms to C\#.
    -   With such small algorithms, AI translation is feasible.
    -   The VB.Net code was improved because of translation to C\#.
-   C\# is now a peer to VB.Net when developing algorithms in OpenCVB.
    -   A new snippet version for C\# is equivalent to the VB.Net snippet.
    -   Performance measurement for C\# works the same as that for VB.Net
    -   Existing manually created C\# algorithms are in CS_Non_AI.cs.
-   Existing VB.Net infrastructure is reused for C\# algorithms.
-   OpenCVB’s Touchup.exe simplifies necessary small changes after AI translation.
    -   The Touchup application is invoked from OpenCVB’s main toolbar.
    -   Most algorithms were converted to C\# in a few minutes.
    -   Longer conversions required the improved VB.Net infrastructure.
-   Previous Releases had a discontinued C++ AI translation process. It will return.

![A collage of images of a room Description automatically generated](media/5eb5c074fbaeaf5aa017addc04b8710d.png)

**AddWeighted_Basics_CS:** *The C\# version of the AddWeighted_Basics algorithm is shown above. All the C\# algorithms end with “_CS” to distinguish them from the VB.Net version. If the algorithm uses both C++ and C\#, the name ends with “_CPP_CS”.*

![A screenshot of a computer program Description automatically generated](media/cb0b14073ca049ecd641450e21bb739e.png)

![A screenshot of a computer program Description automatically generated](media/b2dafcc91cee598b2abfca8ce921a5b4.png)

**Performance Comparison:** *The top image was captured when running the VB.Net version of Annealing_MultiThreaded_CPP_VB. The bottom image was taken from the C\# version. There are some differences in layout but the critical numbers are present and look correct. More testing is needed. The performance metrics are provided in the VB.Net infrastructure code.*

# 2024 July - Recent Changes

-   Over 2400 algorithms are included, averaging 31 lines of code per algorithm.
-   Please Note: the June version of OpenCVSharp has compile-time issues in VB.Net.
    -   It will be addressed soon and it is only a problem to VB.Net.
    -   In the meantime, please use the January version: 4.9.0.20240103
    -   The January version is present by default but if you upgrade to June…
-   The jump in algorithm count is due to the addition of AI-generated C\# algorithms.
    -   Only about 400 of the 1800 VB.Net algorithms have been converted to C\#.
    -   The remaining algorithms will continue to be translated in future releases.
-   Microsoft’s CodeConverter.ai translated the VB.Net algorithms to C\#.
    -   With such small algorithms, AI translation is feasible.
    -   The VB.Net code was improved because of translation to C\#.
-   C\# is now a peer to VB.Net when developing algorithms in OpenCVB.
    -   A new snippet version for C\# is equivalent to the VB.Net snippet.
    -   Performance measurement for C\# works the same as that for VB.Net
    -   All existing manually created C\# algorithms are in CS_Non_AI.cs.
-   Existing VB.Net infrastructure is reused for C\# algorithms.
-   OpenCVB’s Touchup.exe simplifies necessary small changes after AI translation.
    -   The Touchup application is invoked from OpenCVB’s main toolbar.
    -   Most algorithms were converted to C\# in a few minutes.
    -   Longer conversions required the improved VB.Net infrastructure.
-   Previous Releases had a discontinued C++ AI translation process. It will return.

![A screenshot of a computer Description automatically generated](media/5dd47a048cd220e4dd856894c3d6bac7.png)

**CS_AddWeighted_Basics:** *The C\# version of the AddWeighted_Basics algorithm is shown above. All the C\# algorithms start with “CS_” to distinguish them from the VB.Net version.*

![A screenshot of a computer program Description automatically generated](media/498a747eed8b64cf8e4aab79d498d0c7.png)

# 2024 June - Recent Changes

-   Over 2000 algorithms are included, averaging 31 lines of code per algorithm.
-   Support for the Orbbec Gemini 335L camera was added but there are limitations.
    -   Only 5 frames per second to keep depth in sync with RGB image.
    -   Supported image sizes are 1280x720 and 640x480.
        -   Other image sizes are present but not commonly used.
    -   The Orbbec Gemini 335L arrived May 2024. Firmware will likely improve.
    -   OpenCVB will work with any camera that has RGB, cloud, left, and right images.
        -   IMU is needed as well to find gravity and orient the image in 3D.
    -   There are now 7 cameras supported in OpenCVB.

        ![A screenshot of a computer Description automatically generated](media/87473380f7ef7669aac59b0e2568b664.png)

        **OpenCVB Global Settings:** *There are 7 cameras supported in OpenCVB. Cameras that were not found in initialization are disabled. Switching to another camera will change the resolutions that are available.*

    -   The Orbbec SDK is automatically installed with the Update_All.bat install run.
    -   It is a small thrill to see all 2000 algorithms suddenly working with the 335L.
-   Python_Classes added to separate Python algorithms from the VBClasses
    -   Default group name is \<All VB.Net\> to get only VB.Net classes
    -   Other group names added were: “\<All Python\>” and “\<All C\#\>” classes.
-   The code to find and track features was reorganized. New features added for ROI’s.
    -   Feature_Basics now uses correlation coefficients to track RGB features.
-   Color8U is the new name for algorithms converting RGB to CV_8U format.
-   New modules for C\# interface were added but are not yet in use.

![A screenshot of a computer Description automatically generated](media/c8c7bce7087510e62ffc160f82e12e07.png)

**LeftRight_Basics:** *This output shows all 4 images provided by the Orbbec Gemini 335L camera – RGB, depth, left, and right images. The depth image is the 3rd channel of the point cloud data colorized by OpenCVB’s* Depth_Colorizer_CPP *algorithm. The point cloud is shown below.*

![A room with a white door and a blue line Description automatically generated](media/0f3868fe21a923bc219e81fb71aaabec.gif)

**OpenGL_Basics:** *This output shows the Orbbec Gemini 335L camera while toggling the checkbox to adjust the 3D point cloud with and without correcting for gravity. The same algorithm to adjust for gravity works with all the cameras supported by OpenCVB. Similarly, all 2000+ algorithms in OpenCVB work for the 335L camera after adding an interface to the Orbbec SDK.*

# 2024 May - Recent Changes

-   Over 2000 algorithms are included, averaging 32 lines of code per algorithm.
-   Last month’s horizon and gravity vectors are now faster and more robust.
    -   See last month’s update at the bottom of this document.
-   RedCloud cell stats and depth histogram can be displayed at any time.
    -   See the RedCloud option “Display Cell Stats” whenever RedCloud is active.
    -   An example of “Display Cell Stats" is shown below in “Bin3Way_RedCloud”.
-   LinearRegression.vb – simple linear regression – was added with several demos.
-   RedCloud output can be natural colors – computed from the cell’s RGB data.
-   RedCloud 3D cell data can be shown in OpenGL – see OpenGL_ColorBin4Way.
-   Global options control whether RedCloud cells are highlighted and identified.
-   Features_LeftRight finds “good” features the left and right images.
-   FeatureLess_Basics was added to the list of possible inputs to RedCloud.
    -   Each region without features is isolated and identified to RedCloud.
-   Cell_Generate reuses cell features for exact matches – less work, same result.
-   Cells with motion are now identified providing another way to detect motion.

![A collage of images of a person in a room Description automatically generated](media/a18bd533a52c859d195439cb4fabc6f1.png)

**Bin4Way_Basics:** *The objective of this algorithm is to break down the various brightness levels. The lower left frame shows 4 levels – darkest to lightest. The upper right frame shows a grid layout of the selected brightness – darkest to lightest. The currently selected frame contains all the pixels that are the darkest. The lower right frame breaks down each of the 4 brightness levels in the selected grid element (highlighted with a rectangle) in the upper right frame. The number of pixels, contours, brightness level, and a measure of volatility are displayed with each breakdown of the grid element in the lower right frame.*

![A collage of images of a room Description automatically generated](media/41d5af9a9a6b649c81cbb837cb95dab4.png)

**Bin3Way_RedCloud:** *RedCloud is run against the darkest and the lightest frames in the Bin3Way_Basics algorithm. While there is more unclassified space in the lower right image, the cells identified are more consistently present than in other RedCloud algorithms that attempt to classify each pixel in the image. This algorithm produces fewer cells but they are more robust and stable. This sample output also shows the output for the global options to “Display Cell Stats”. The upper right frame shows the histogram of the depth for the selected cell while the lower right frame shows the statistics for the selected cell.*

# 2024 April - Recent Changes

-   Over 1900 algorithms are included, averaging 31 lines of code per algorithm.
-   Recently used algorithms are now accessible with the main toolbar “Recent” button.
-   A log of previous changes is included at the bottom of this document.

![A collage of images of a room Description automatically generated](media/36150778314078d8e16dfcde622cec21.png)

**Line_Gravity:** *The bottom right image shows all the lines detected in the image. The bottom left image shows the vertical lines in yellow and the horizontal lines in red. The vertical lines are aligned to the gravity vector and the horizontal lines are aligned with the horizon vector. The method to find the gravity vector is to locate two points where the X-values in the point cloud transition from negative to positive. Similarly, the horizon vector is defined by 2 points where the Y-values in the point cloud transition from negative to positive. Because the point cloud is aligned with the color image, the horizon and gravity vectors are defined in the image coordinate system. The camera is deliberately tilted for this example but both vectors move as the camera moves.*

# 2024 March - Recent Changes

-   Over 1900 algorithms are included, averaging 31 lines of code per algorithm.
-   Gravity and horizon vectors are now available in the image coordinates.
    -   See example below to see what it looks like.
    -   The IMU code is now simplified and more responsive.
-   RedCloud_Basics and Flood_Basics are now more stable and accurate.
    -   Removing contours before flooding helped isolate cells better.
    -   RedCloud cell statistics can now be shown any time.
        -   See global option labelled ‘Display Cell stats’.
    -   The rcData structure has been stripped of many low-use variables.
-   Projection_Basics provides the distance to each object in the image.
    -   Top X objects are found without thresholds – sorted by size.
    -   See Projection_Top/Side for distance and size of objects.
-   Flood.vb algorithms were improved and some algorithms were removed.
-   Several methods to identify peaks and valleys in histograms were tested.
    -   See any algorithms starting with ‘HistValley’.
-   EdgeToEdgeLine function in pointPair structure was added.
    -   Line is defined in terms of an edge-to-edge pair of points.

![A collage of images of a room Description automatically generated](media/36150778314078d8e16dfcde622cec21.png)

**Line_Gravity:** *The bottom right image shows all the lines detected in the image. The bottom left image shows the vertical lines in yellow and the horizontal lines in red. The vertical lines are aligned to the gravity vector and the horizontal lines are aligned with the horizon vector. The method to find the gravity vector is to locate two points where the X-values in the point cloud transition from negative to positive. Similarly, the horizon vector is defined by 2 points where the Y-values in the point cloud transition from negative to positive. Because the point cloud is aligned with the color image, the horizon and gravity vectors are defined in the image coordinate system. The camera is deliberately tilted for this example but both vectors move as the camera moves.*

# 2024 February - Recent Changes

-   Over 1800 algorithms are included, averaging 32 lines of code per algorithm.
    -   Average went up because counting hadn’t included some of the new C++ code.
-   Motion-filtered color and cloud images are controlled through a global option.
    -   Example below shows the improvements to the RedCloud output that result.
-   Microsoft’s Copilot can provide VB.Net to C++ translation as well.
    -   It costs to get the Pro version with full translation support.
    -   The free version limits the number of characters for translation.
-   Both Bard and Microsoft use EMGU when C++ is translated to VB.Net.
    -   OpenCVB doesn’t need EMGU – and there are OpenCVSharp conflicts.
    -   Use the OpenCVSharp equivalent instead of the EMGU API.
-   OpenCV 4.9 was available and OpenCVB has switched to using it by default.
-   OpenCVSharp 4.9 similarly is now the default for use in OpenCVB.
-   The OpenCV Samples provide an excellent resource but may be hard to run.
    -   Too often parameters are required which make it challenging to use.
    -   OpenCVB has started migrating the OpenCV samples to VB.Net.
    -   As with all OpenCVB algorithms, each will work when clicked.
-   A new group name “\<All Reused and Callees\>” shows algorithms reuse.
    -   Removes one-off experiments and cleans up the list of algorithms.
    -   It is a further refinement of the user interface to help active development.
    -   It also provides a useful way for beginners to find the better algorithms.

![A screenshot of a computer screen Description automatically generated](media/da280b898b238ab7490bfca8fc4abbfa.gif)

**RedCloud_BasicsColor:** *The color input for RedCloud_BasicsColor and any other OpenCVB algorithm can be motion-filtered using a global option. The frame is only processed if there is scene motion. The objective is to improve the consistency of the cells produced which can be seen in the cells away from the motion – look to the right side of the image. Cells without motion are updated on a heartbeat (once a second.) There is little benefit to capturing cell perturbations when there is no motion in the color image for that cell. Motion-filtered color images often display artifacts from a previous frame but when the image data is already so variable from frame to frame, there is little downside to motion-filtering for image segmentation using depth and color. A new global option allows the motion rectangle to be displayed in the upper left image (in white.)*

# 2024 January (2) - Recent Changes

-   Over 1800 algorithms are included, averaging 29 lines of code per algorithm.
-   The jump in the algorithm count is due to the AI generated C++ copies.
-   See “CPP_AI_Generated.h” for the examples of the AI generated C++ versions.
    -   Any VB.Net algorithm can be translated to C++ using AI.
    -   A step-by-step method is available in OpenCVB to translate VB.Net code.
    -   The OpenCVB translator has been rewritten to use Google’s Bard AI engine.
    -   The “T” for translate button is available in OpenCVB’s main toolbar:

![A screen shot of a computer Description automatically generated](media/86bab3338bba640ac3c8fa4505296c86.png)

-   OpenCVB’s short algorithms are ideal tests for Bard’s translator.
    -   Bard is aware of OpenCV and the differences in the API’s from VB.Net to C++.
-   But every translation requires a few touch-ups to get the C++ version working.
-   A tutorial shows how to create a C++ version of an OpenCVB algorithm.
    -   All translated algorithms are in an “include only” format – no library needed.
    -   Just add the CPP_AI_Generated.cpp file in your (non-OpenCVB) C++ applications.
    -   See the tutorial titled “(7) AI Generated C++” in the OpenCVB tree.
-   The review of the C++ algorithms prompted a reorganization of the C++ code.
    -   The reduced number of files should make it easier to reuse the code elsewhere.
-   Why not put everything in C++? Answer: Bard is not that good and it is real work!
    -   Translators make assumptions and short algorithms make fewer of them.
-   Translating an algorithm is an excellent way to review the code.
    -   Often improvements become clear when implementing the C++ version.
    -   Translation back to VB.Net keeps the 2 trees in sync (Bard can do that too.)
-   This version also introduces another RedCloud color source: Binarize_FourWay.
    -   Binarize an image and binarize each half to classify each pixel.
    -   Below is an example of the output from RedCloud_BinarizeColor.

![A collage of images of people in different colors Description automatically generated](media/9b95f50d8a35d8ee7156f9fb857f4557.jpeg)

**RedCloud_BinarizeFourWay:** *The image pixels are classified into four categories based on their brightness. The grayscale image is binarized, and each half is binarized again to produce four classifications of pixels. The image in the lower right is the colorized version of the pixels after classification with the Binarize_FourWay algorithm. RedCloud is then used to identify each resulting regions and produce the image in the lower left image.*

# 2024 January (1) - Recent Changes

-   Over 1700 algorithms are included, averaging 31 lines of code per algorithm.
-   Algorithm complexity can now be visualized with OpenCVB.
    -   A new button will collect algorithm performance at all available resolutions.
    -   The new button is shorthand for O(n), typically used to represent complexity.
    -   The sample output below highlights the complexity icon in OpenCVB.

![](media/11e21246b61a18500926fa8d55db2d0b.png)

-   Also, note the presence of another new icon to the right of the Complexity icon.
    -   The ‘Advice’ icon will display any advice associated with the algorithm.
    -   Advice is usually just a list of options that impact the current algorithm.
    -   Only a few algorithms include the advice feature but more are coming.
-   Default options were reduced – overloaded and too detailed.
    -   Algorithms that need all options use \<Algorithm Name\>WithOptions.
-   Complementary problem with hidden important algorithm-specific options
    -   Options can override the default to hide the option form at the side.
    -   See Gif_Basics for an example of overriding the default to hide the form.
-   Foreground in depth can be found using several methods in Foreground.vb.

![A screen shot of a computer Description automatically generated](media/bf94edf2ee5f261622a2e31f34db3d51.png)

**Complexity_Basics:** *To collect complexity data, select any OpenCVB algorithm and click the ‘O’ button in the toolbar. This will run the algorithm for 30 seconds at each of the available resolutions – click the same button to stop data collection. After the data has been collected, use the “Complexity_Basics” algorithm to review the data. The right side of the image above shows all the algorithms that have data in the directory. The plot on the left side shows the plot for the algorithm selected in the options using the same scale. By default, the selected algorithm is the last one collected but a set of radio buttons in the options allows the data for other algorithms to be selected.*

# 2023 December - Recent Changes

-   Over 1700 algorithms are included, averaging 31 lines of code per algorithm.
-   The 3D histogram improves the point cloud “blowback” pixels.
    -   See the OpenGL_Filtered3D algorithm and images below.
-   All stable RedCloud cells are identified in color and depth.
    -   See Cell_Stable for an alternative way to match cells.
-   This version introduces the concept of spectrum range for a cell.
    -   Spectrum: values for depth or color that form a continuous range.
-   Spectrum algorithms will find the ranges of color/depth in RedCloud cells.
    -   RedCloud cells already cluster color and depth samples.
    -   Spectrum isolates outliers in color and depth to facilitate their removal.
    -   Spectrum options define the gap sizes for depth and color.
-   Is there a simple way to define foreground and background automatically?
    -   Use KMeans with depth input and k=2. See KMeans_Depth algorithm.
    -   To see how to use foreground/background with GrabCut: GrabCut_Basics.
-   GifBuilder WPF warnings now gone – reworked it as a Windows Form application.
-   RedCloud depth ranges are more accurate thanks to the new rc.depthMask.
-   Contour masks for depth and ‘no depth’ were added to task structure.
    -   Depth contour defines a boundary between depth and ‘no depth’.
-   New heartbeats were added at quarter second intervals.
    -   All heartbeats are now time-based only (not FPS based.)
-   HistValley_Depth finds histogram valleys and separates depth into tiers.
-   Added RedMin algorithms to find a minimalist approach to RedCloud cells.
    -   No requirement for a dummy cell at location 0, 0.

![A computer generated image of a building Description automatically generated](media/7e883a32a7ee8faaf76107f24eea917a.gif)

**OpenGL_Filtered3D:** *The histogram interface in OpenCV supports 3D point clouds where the bins can be thought of as 3D bricks in the 3D point cloud. The ‘Histogram Bins’ slider controls a threshold that is used to zero out bricks that have fewer samples than the threshold. When the slider is set to zero, all the blowback pixels appear and extend behind the wall in this side angle view in OpenGL. Bins with less than the specified threshold are set to zero and the backprojection creates a mask that reduces the blowback. The camera used in this example is the Intel D455. The Microsoft Kinect for Azure camera is more accurate and does not have much blowback.*

# 2023 November - Recent Changes

-   Over 1700 algorithms are included, averaging 31 lines of code per algorithm.
-   RedCloud and RedColor algorithms were reorganized and reviewed.
    -   RedCloud/RedColor algorithms include depth and color by default.
    -   RedCloud algorithms needed a custom options form – OptionsRedCloud.vb.
    -   “redOptions” includes RedCloud and related options and is always present.
-   RedCloud algorithms were consolidated and are now all in RedCloud.vb.
    -   Each algorithm can use guided backprojection or reduction to create cells.
    -   Pointcloud reduction is now controlled by the slider in RedCloud_Core.
        -   Needed to be separate from reduction slider.
    -   Each algorithm can use different color sources for cells with no depth.
-   RedColor algorithms now supplement RedCloud algorithms with color data.
    -   Color source is defined in the RedCloud options form.
-   MSER algorithms were improved with better image segmentation.
    -   It provides an alternative segmentation method for color images.
-   Neighbor cells are now easily accessible to RedCloud and RedColor algorithms.
    -   Core technique to find neighbors (example below) is in Neighbors.vb.
-   The TreeView display of performance times now shows the data in tree order.
    -   Easier to identify where the overhead is in the algorithm.

![A collage of images of different colors Description automatically generated](media/aa767d146879de432a3a0208b65b6eca.gif)

**RedCloud_Neighbors:** *The neighbors for each cell can be included in the cell information. Here the neighbors of the highlighted cell were requested and are shown in the lower right image.*

# 2023 October - Recent Changes

-   Over 1700 algorithms are included, averaging 30 lines of code per algorithm.
-   Improvements to image segmentation now classify all pixels in the image.
-   RedCloud_Motion detects image motion by comparing RedCloud cells.
    -   Pixel differences identify cells that have changed (as usual).
    -   The “motionRect” variable is the union of current and previous rect.
        -   Motion is the combination of where the cell was with where it is now
-   RedCloud_Motion uses the latest version of RedCloud_Basics to detect motion
    -   A “motionRect” variable describes the union of the previous and current rect.
    -   It is not flawless. Test the RGB image quality with RedCloud_MotionTest.
    -   Option automation is used to define the lowest pixel difference threshold.
-   Improved PixelViewer support – better form placement and pixel layout.
-   Plots over time are much more reactive to the changes in the input data.
-   MSER algorithms were reviewed and improved.
-   Accord and Dlib algorithms were removed (about 35 algorithms)
    -   They were not getting reused by other algorithms.
    -   They added a lot NuGet packages that complicated installation.
-   The OpenCVB git repository was reset – the repo was bigger than the tree.

![A colorful pattern with dots Description automatically generated with medium confidence](media/db52b0115273726a6ff2d1aa986c0817.gif)

**RedCloud_Basics:** *What’s different? The latest version of the image segmentation algorithm is similar to the previous version below but has classified* **ALL** *of the pixels. Small cells were tossed in the example below yielding holes (represented as black segments below) while here the small cells are consolidated using a grid that covers the entire image. As before, if a cell’s color is consistent, it has been matched with a cell from the previous frame.*

![A colorful squares and lines Description automatically generated with medium confidence](media/c4eed0d963820c627ec5b94291a36c4d.gif)

**RedCloud_Basics** *(This is the previous version of RedCloud_Basics from September 2023.) This image segmentation algorithm uses both the point cloud and color to identify cells. RedCloud algorithms typically reduce the point cloud resolution in X and Y to produce cells that describe regions in the image. This algorithm also uses the reduced point cloud but has added cells based on color for regions that have no depth. Because both color and the point cloud are used, the whole image is segmented instead of just that portion with depth. When a cell’s color is consistent, it has been matched to a cell in the previous frame.*

# 2023 September - Recent Changes

-   Over 1700 algorithms are included, averaging 30 lines of code per algorithm.
-   Guided back projection assists in image segmentation.
    -   Guided means the histogram is doctored before back projection.
    -   Updated histogram entries define segments in the back projection.
    -   See example below for the combined color and cloud results.
-   Back projection in 2D histograms is now used in more algorithms.
-   Don’t forget to have some idle fun – AsciiArt.vb
-   Histogram3D and back projection reviewed and improved.
-   Improved RedCloud and RedColor algorithms using back projection.
    -   C++ module FloodCell.cpp replaces 4 reduction algorithms.
-   Improvements to the display of side and top projections were added.
    -   Output images no longer clip the side and top view data.
    -   New global variables XRange and YRange control how projection spreads.

![A colorful squares and lines Description automatically generated with medium confidence](media/c4eed0d963820c627ec5b94291a36c4d.gif)

**RedCloud_Color:** *This image segmentation algorithm uses both the point cloud and color to identify cells. RedCloud algorithms typically reduce the point cloud resolution in X and Y to produce cells that describe regions in the image. This algorithm also uses the reduced point cloud but has added cells based on color for regions that have no depth. Because both color and the point cloud are used, the entire image is segmented instead of just that portion with depth. When a cell’s color is consistent, it has been matched to a cell in the previous frame.*

# 2023 August - Recent Changes

-   Over 1700 algorithms are included, averaging 30 lines of code per algorithm.
-   OpenCVB has moved to OpenCV 4.8 – see “PragmaLibs.h” for the update.
    -   Delete the “opencv” directory in OpenCVB’s home directory.
    -   Run OpenCVB’s “Update_All.bat” to update OpenCV.
    -   Rebuild OpenCVB. Post with any install problems. They are high priority.
-   Testing install showed that librealsense2.sln is now realsense2.sln
    -   The “Update_All.bat” file is now updated to reflect that change.
-   A new “toggle” variable in every algorithm switches on and off about once a second.
-   GIF support now includes capturing the dst0 and dst1 images independently.
    -   All 4 images and the OpenGL output may be captured individually.
-   FeatureMatch_Basics (shown below) now uses color images for left/right images.
    -   Kinect for Azure has only 1 camera, so it doesn’t produce any matches.
    -   All other cameras have left and right images and will work properly.

![A room with a computer and a desk Description automatically generated](media/6910830262336f25a37fd666645d9382.gif)

**FeatureMatch_Basics:** *OpenCV’s GoodFeatures function is called twice, once with the left image and then with the right image. Features with a high correlation coefficient between left and right images are considered matched and this GIF image confirms that the matches are largely correct. AddWeighted_Basics is used to combine the left and right images and the white line segments connect the points that are confirmed matches. The closer the feature is to the camera the longer the line segment. In this test, there are two line segments that look incorrect. Can you find them? Why is there any variability in the highlighted line segments? GoodFeatures is not precise and will often pick a point above or below the corresponding one in the other image. Points must be in the exact same row, or they are not considered candidates. BRISK features may also be selected for input to this algorithm.*

# 2023 August - Recent Changes

-   Almost 1700 algorithms are included, averaging 30 lines of code per algorithm.
-   Improvements to tracking RedCloud cells. Below is an example GIF.
    -   RedColor_Basics and RedCloud_Basics both track all cells.
    -   Reduced color =RedColor, Reduced point cloud = RedCloud.
    -   RedCC is an abbreviation for Reduced color and cloud.
-   FeatureMatch algorithms independently find features in left and right images.
    -   Both BRISK and GoodFeatures can be used in FeatureMatch algorithms.
    -   Feature points identify the same object in both left and right images.
    -   The output is a set of fixed points in 3D space.
-   Up and down arrows now work to switch between algorithms.
-   The gravity and the history cloud may now both be active.
    -   Global algorithm options allow toggling which are active.
    -   To demo, try toggling the following checked boxes:

![A screenshot of a computer program Description automatically generated](media/02c6ce848cd7de5f5fea7561677395ca.png)

-   OpenCV’s puttext settings were reworked to support the different resolutions.
    -   Histogram output is much more readable with different resolutions.
-   The GroupName combo box now uses blank lines to group the selections.
    -   It is easier to locate an OpenCV API or OpenCVB algorithm group.

![A room with colorful furniture Description automatically generated](media/505f9ca73f5ee142fb9e3879bdae651d.gif)

**RedColor_Basics:** *RedColor_Basics builds cells from a reduced color image (reduced color = RedColor.) All cells are tracked even with camera motion. Note that the highlighted floor cell is tracked despite changing size and shape. A cell will maintain its color (indicating the cell was tracked) if it was present in the previous frame. The black dot in the selected (white) cell represents the point with the maximum distance from surrounding cells and is defined for each cell for use with tracking.*

# 2023 July - Recent Changes

-   Almost 1700 algorithms are included, averaging 30 lines of code per algorithm.
-   A tutorial on the option “Synchronize Debug with Output” is now available.
    -   It outlines a more WYSIWYG approach to debugging algorithms.
-   The history cloud contains the point cloud averaged over X frames.
    -   It also removes pixels that were not present in all X frames.
    -   Use of the history cloud is toggled with a global algorithm option.
-   The GIF capture feature now can capture the OpenGL output from OpenCVB
    -   Examples below compare using the history and raw cloud images.
-   Algorithm option presentation was improved – fewer algorithm options present.
-   RedCloud contours now use ApproxNone – better floodfill results.

![A colorful cube with a laser Description automatically generated](media/1e1c632b14491a6493bfa9ba40589995.gif)

**OpenGL_RedCloud:** *The OpenGL_RedCloud algorithm displays the RedCloud output in OpenGL and colors each RedCloud cell. The output above uses the history cloud collected over 10 frames and masks outs pixels that were not present in all 10 frames.*

*![A colorful cube with a line Description automatically generated](media/cc1176c66b58e5a9a131347007d99453.gif)*

**OpenGL_RedCloud:** *The same algorithm as above running with the raw point cloud. The history cloud is different from the raw point cloud because it averages the point cloud over 10 frames and masks out pixels that were not present in all 10 frames.*

# 2023 July - Recent Changes

-   Almost 1700 algorithms are included, averaging 30 lines of code per algorithm.
-   Separators in the list of algorithms make it easier to find algorithms.
-   Tutorial for lane finding now includes a Gif representation of the results.
-   StereoLabs Zed 2/2i cameras have 3 new resolutions based on SVGA mode.
    -   Images (left, right, IMU data, and point cloud) arrive at 100 FPS.
    -   This is the fastest image retrieval for all supported cameras.
-   RedColor_Track will track the selected cell from frame to frame.
    -   Distance in 8 dimensions - BGR Color mean, stdev, and location.
    -   Must have a threshold of pixels for tracking to work.
-   OpenCVB now includes another way to detect motion – see below.
-   RedCloud algorithms are split into 3 groups – RedCloud, RedColor, and RedCommon.
    -   RedCloud algorithms use a reduced point cloud input.
    -   RedColor algorithms use a reduced color image,
    -   RedCommon algorithms can be by either RedCloud or RedColor.

        ![A screenshot of a computer screen AI-generated content may be incorrect.](media/6cba27c1ec97a5d241d60e43951855c1.gif)

**RedCloud_MotionBGSubtract:** *The GIF above shows another way to detect motion in an image. The lower right shows the conventional difference between images – a standard way to detect changes in pixel values by comparing images. The lower left image is the RedCloud_Basics output with cells for each reduced color segment. The upper right image shows the RedCloud cells that contain pixels that changed.*

# 2023 July - Recent Changes

-   StereoLabs camera support upgraded to Zed SDK 4.0.
    -   Camera calibration data moved to “camera_Configuration” structure.
    -   Zed SDK 4.0 appears to be more efficient than earlier releases.
-   The default group is now “\<All but Python\>” instead of “\<All\>”.
    -   All VB, C\#, and C++ algorithms are guaranteed to work.
    -   Python algorithms may have missing packages on a new installation.
-   JSON interface is helpful when any algorithm fails.
    -   Fix any OpenCVB failure to start by removing the settings.json file.
-   Almost 1700 algorithms are included, averaging 30 lines of code per algorithm.
-   All the RedCloud algorithms were reviewed and consolidated.
    -   Point cloud input algorithms are in the RedCloud.vb
    -   Color image input algorithms are in the RedColor.vb
    -   RedColor_Basics uses any of the 8 color inputs in the global options.
    -   Original version of RedCloud is still available – RedCloud_BasicsOriginal.
-   GIF animation in OpenCVB has been reworked and generalized.
    -   Create a GIF image for any algorithm in OpenCVB
    -   Click on the global algorithm checkbox “Create GIF of current algorithm”.
    -   There is an option to capture dst2, dst3, or the entire application window.
    -   Example below captures the entire application window.

![A collage of images of a person AI-generated content may be incorrect.](media/62ec1d7073fbf71e996e7ada7bec557b.gif)**RedCloud_BasicsColor:** *An example of using the GIF interface to capture an OpenCVB algorithm. The bottom left image is the RedCloud_BasicsColor output that uses both color and cloud data..*

# 2023 June - Recent Changes

-   StereoLab’s ZED 2 cameras are now required in the default installation.
    -   “Update_All.bat” asks to install StereoLabs ZED 2/2i camera support.
    -   StereoLabs requires NVIDIA and CUDA and won’t compile without them.
    -   If StereoLabs support is troublesome, turn support off in CameraDefines.hpp.
        -   Comment out CameraDefines.hpp “STEREOLAB_INSTALLED” \#define.
    -   Left and right views can now contain color images instead of grayscale.
-   Almost 1700 algorithms are included, averaging of 30 lines of code per algorithm.
-   New global option to assist in debugging algorithms.
    -   Screen output is exactly what was just stepped through in the debugger.
    -   100 millisecond delay allows screen update to happen before next iteration.
    -   In the “All Algorithm Options” look for “Synchronize output with Debug”.
-   Why are options now counted as algorithms?
    -   The algorithm group combo box selects all algorithms using those options.
    -   Select an option algorithm in the Group combo box (the second combo box.)
    -   Test all the algorithms that use the changed options with one click.
-   Support for 1920x1080 input is now an option for Kinect and ZED 2 cameras.
    -   See OpenCVB Global Options dialog box.
-   “Temp Slider” added to the “All Algorithm Options” for testing.
    -   Algorithms can test a slider before adding it as a new option.
    -   Similar to the “Fun Checkbox” that can toggle a feature.

# 2023 June - Recent Changes

-   Over 1680 algorithms are included with an average of 30 lines of code per algorithm
    -   Compile OpenCVB and all algorithms can be selected using a combo box.
    -   Algorithms contain just code for the algorithm – not infrastructure.
-   New global algorithm option to toggle multi-threading.
    -   Review it with HeatMap_Grid to see if multi-threading helps.
-   New global algorithm option to “Show All” options when opening Options.
    -   Some prefer the default to be show all available options for the algorithm stack.
-   All OpenCVB camera interfaces were reviewed and improved.
    -   Less data movement for every camera.
-   All the backprojection 2D algorithms were reviewed and new ones added.
    -   BackProject2D color classify algorithm added to global options.
-   All the HeatMap algorithms were reviewed and updated.
-   More json settings were added and tested.
-   A new set of KWhere algorithms were added.
    -   The image below shows the typical output
    -   There is a lot of new material and KWhere needed its own tutorial.
        -   <https://github.com/bobdavies2000/OpenCVB/tree/master/Tutorials>
        -   Select “(5) Finding K”
-   The jump in the count of algorithms is the result of adding Options.vb to the tally
    -   The options were not included in the algorithm count previously.
    -   Including options means all algorithms using an options are easily found.

![A screenshot of a computer Description automatically generated with medium confidence](media/39e5ccb9bf9f87faf1b5649e6008b3c5.png)

**KWhere_DoctoredBP** *– The algorithm finds the K objects present in the image and where they are. It is based on a “doctored backprojection” of the top-down view (upper left) and side view (upper right). The result identifies K objects in the lower left image with individual colors. For more information, see the tutorial on “Finding K”.*

# 2023 May - Recent Changes

-   Over 1550 algorithms are included with an average of 30 lines of code per algorithm
    -   Compile OpenCVB and all algorithms can be selected using a combo box.
    -   Algorithms contain only the code for the algorithm – separated from infrastructure.
-   What are the principal design features of OpenCVB?
    -   Each algorithm can visualize a reliable test case when run standalone.
    -   Overnight testing of all algorithms is kicked off with one click.
    -   OpenCVB reads its own code to find the names of all algorithms.
        -   User interface combo boxes are generated automatically on every run.
        -   Also generated: algorithm count, total lines of code, lines per algorithm.
    -   Keep algorithms small – 30 lines of code, easily understood, easily rewritten.
    -   Visualize both results *and* performance to easily verify and understand.
    -   Combine algorithms easily – standard connections, easily reconfigured.
    -   Keep infrastructure separate from the algorithm.
        -   The environment is abstracted to avoid dependency on Windows.
        -   Snippets are available to add algorithms, options, sliders, radio buttons, checkboxes.
        -   3 option groups: general OpenCVB, all algorithms, and algorithm specific.
    -   Make it simple to add more algorithms.
        -   Snippets and “Blue Plus” button *![](media/0dede74f225b8e19e8f4fd5a50ba9f28.png)* generate new algorithms easily.
-   A list of RedCloud neighbor cells was added for each cell.
-   A Principal Component Analysis (PCA) eigenvector is available for RedCloud cells
-   A plane equation has been added for each RedCloud cell found.
-   ChatGPT and Bard both translate VB.Net to C++, C\#, or Python.
    -   Translating is easier without infrastructure or user interface.
    -   It is more important than ever to keep algorithms short and direct – just the algorithm.
-   The Mynt D1000 camera installation and capture were reviewed and improved.
    -   Installation is now a simple .bat – AddMynt.bat
    -   Unfortunately, it looks like the Mynt cameras are no longer available.
-   Json has been introduced and parameters are being converted from registry to json.
    -   All parameters are vetted in jsonRead() and jsonWrite.
    -   No more registry entries for OpenCVB.
    -   If anything goes wrong, just delete the \<OpenCVB HomeDir\>/settings.json\>.
    -   Improved testing for a wider range of settings.

![A picture containing text, screenshot, multimedia software, art Description automatically generated](media/195f7fde1649a871e7cb3cab7ca8f4fe.png)**RedCloud_Planes** *– The data for each cell now contains the plane equation for the cell and a list of neighboring cells. The lower left shows the numbered cells with the selected cell shown in white. The selected cell and its neighbors are shown in the upper right image. The upper left image highlights the selected cell in the RGB image. In the lower right are the same cells colored with the direction of the principal axis of the plane equation – red cells are oriented along the Z-axis, blue for X-axis, and green along the Y-axis (floor and ceiling).*

# 2023 May - Recent Changes

\- Over 1520 algorithms are included with an average of 30 lines of code per algorithm

\- Compile OpenCVB and all algorithms can be selected using a combo box.

\- Algorithms contain only the code for the algorithm – separated from infrastructure.

\- More Accord algorithms were added to OpenCVB.

\- Accord website with documentation: \<http://accord-framework.net/\>

\- 25 Accord image filters added (see Filter_AccordSuite.)

\- Other Accord additions: wavelets, Self-Organizing Maps (SOM).

\- Motion_Rect algorithm finds and keeps the maximum extent of motion.

\- Minimizes the work to update the image.

\- Artifacts are present in the motion-updated image but still useful.

\- TreeView user interface is simplified – intermediate results one click away.

\- RedCloud reorganization underway – see RC_Basics.

\- Simpler interface, neighbors identified, RGB and depth merged.

# 2023 May - Recent Changes

-   Over 1520 algorithms are included with an average of 30 lines of code per algorithm
    -   Compile OpenCVB and all algorithms can be selected using a combo box.
    -   Algorithms contain only the code for the algorithm – separated from infrastructure.
-   More Accord algorithms were added to OpenCVB.
    -   Accord website with documentation: <http://accord-framework.net/>
    -   25 Accord image filters added (see Filter_AccordSuite.)
    -   Other Accord additions: wavelets, Self-Organizing Maps (SOM).
-   Motion_Rect algorithm finds and keeps the maximum extent of motion.
    -   Minimizes the work to update the image.
    -   Artifacts are present in the motion-updated image but still useful.
-   TreeView user interface is simplified – intermediate results one click away.
-   RedCloud reorganization underway – see RC_Basics.
    -   Simpler interface, neighbors identified, RGB and depth merged.

**![A screenshot of a computer screen Description automatically generated with low confidence](media/f230dfdcb1bde53dd59d720e7b8953d4.png)Motion_Rect** *– Motion in the image is isolated by the Motion_Rect algorithm. The lower left image shows the motion detail while the rectangle in the lower right shows the* **maximum** *extent of this motion. OpenCVB’s heartbeat (roughly once a second) updates the entire image. Artifacts may be produced when the color image is updated only with the data from the motion rectangle. One such artifact is highlighted in yellow in the upper left image. The question: how important is it to avoid artifacts? The motion rectangle is produced with every new image as the cost is low – note the frame rate in the top of the image is 90 fps at 320x240 resolution.*

# 2023 April - Recent Changes

-   Over 1510 algorithms are included with an average of 30 lines of code per algorithm
    -   Algorithms contain only the code for the algorithm – no infrastructure.
-   Accord algorithms are now available in OpenCVB.
    -   Accord website with documentation: <http://accord-framework.net/>
    -   Several classification algorithms are available in classify.vb and Accord.cs.
    -   Convert Mat structure to Accord bitmap with the ‘ToBitmap’ extension.
    -   Accord is a NuGet package so installation is invisible.
-   Classify_Basics surveys how to use 7 OpenCV ML classification algorithms
    -   Naïve Bayes, SVM, Decision Trees, Random Forest, ANN, Boosted Trees, KNN
-   Options for all algorithms is the default. Use ‘Show All’ to see all other options.
-   Excel support is now available – see CSV_Excel for example usage.
-   TreeView selections now working when multiple copies of an algorithm are present.
-   New global option to use color and depth to build a RedCloud cells.
-   New global option to update only RedCloud cells with motion.
-   K means is a useful classification tool if ‘K’ is known.
    -   The ‘K’ value is now found using valleys in the depth histogram.
    -   See below example to examine how depth histogram defines ‘K”.

![A collage of images of a room Description automatically generated with low confidence](media/92e62b4ec302c2d2ea93d597cc035cf6.png)

**Depth_Tiers2** *– The choice of K for K Means is critical. Here the depth ‘valleys’ provide a natural way to find K in the histogram of the depth. The white lines in the upper right indicate valley bottoms and provide K to the K Means algorithm. The K Means output of the depth data is depicted in the bottom images.*

# 2023 March - Recent Changes

-   Over 1460 algorithms are included with an average of 30 lines of code per algorithm
-   Missing depth data is now tracked over multiple frames.
    -   Use the global option frame history to control how many frames of missing depth are used.
    -   Removes most of the unstable depth data and blowback from depth edges.
    -   Use “Depth_Basics” algorithm and toggle the global algorithm option “Use Depth Shadow History”.
        -   Turn off the impact of depth history by moving the “Frame History” slider to 1.
    -   Camera motion further separates depth regions for better image segmentation.
-   Photo images from the Berkeley BSDS500 image segmentation database can be tested with OpenCVB.
    -   Image segmentation testing is easier and more reproducible with photos.
    -   Image.vb contains algorithms to load individual images or a series of images.
    -   Image.vb also contains tests for image segmentation with different OpenCVB algorithms.
-   The code to prepare images for RedCloud segmentation has been greatly simplified.
-   Any of the “Color_Classify” output images may be used as input to RedCloud – See “RedCloud_ColorStats”.
    -   A new global algorithm option labelled “Color Class” controls the input to RedCloud.
-   A new global algorithm option is an easy way to toggle algorithm behavior without adding any options.
    -   Use “Fun Checkbox” and test it using the variable “gOptions.FunChecked.checked”.
    -   It is intended to answer a question: could adding this option be valuable or necessary?
-   Post with any problems, especially install problems. They will receive the highest priority.
-   A monthly history of changes is included at the bottom of this document.

![Graphical user interface Description automatically generated with medium confidence](media/e064d9cdab50e85f484ed32f6c5f44a9.png)

**RedCloud_ColorAndCloud:** *This algorithm allows comparison of cells created using the reduced point cloud and cells created using reduced color images. The image in the lower left is segmented using the point cloud and cells don’t penetrate deeply into the scene. The image in the lower right uses color to segment the same scene and some cells will contain foreground and background items. However, there are cells where color segmentation is superior in joining cells that are separated in the point cloud segmentation. Edges in the color image assist with segmentation in the reduced point cloud and may be toggled on an off to see the benefit – see the RedCloud option labelled “Use color edges to better separate RedCloud cells”.*

# 2023 March - Recent Changes

-   Over 1450 algorithms are included with an average of 30 lines of code per algorithm
    -   Earlier versions of OpenCVB had an average of 31 lines on code.
    -   The reduction arose from moving algorithm options to Options.vb thus reducing total lines of code.
    -   The objective is to further reduce the algorithm’s environmental dependencies.
    -   Algorithm options are explicit or tied to the ‘options’ variable.
-   OpenCVB has almost 1000 options that fall into 3 categories:
    -   Global OpenCVB options – options for the OpenCVB application such as working resolution or camera.
    -   Global Algorithm options – options common to all tasks such as maximum depth or line width.
    -   Algorithm options – trackbars, check boxes, or radio buttons tailored just for that algorithm.
-   Options can be easily added to any algorithm using code snippets.
-   Low use options may be ‘sidelined’ – see the “Show All” menu command in the Options Container.
    -   All QT algorithms were removed because of the new ability to sideline an algorithm’s options.
-   The infrastructure for handling options is a major feature of the OpenCVB application.
-   Lines and Planes are detected using a simple depth test (see example below).
-   RedCloud now accepts 8-bit or 32-bit images on all RedCloud runs.
-   The 7 alternative RedCloud inputs are available through the Color_Classify algorithm.
-   RedCloud now optionally classifies image regions without any depth data (using color.)
-   All depth data algorithms can toggle the application of gravity with a global algorithm option.
-   Post with any problems, especially install problems. They will receive the highest priority.
-   A monthly history of changes is included at the bottom of this document.

![Website Description automatically generated](media/ca51a247fee268009638b097647ef791.png)

**OpenGL_PCLinesAll:** *Vertical and horizontal depth lines are detected in the scene and joined. The grid of lines in the lower left image shows lines and cross-hatching where there is likely to be a plane. The background of the lower left image confirms that the estimate for planes is correct. The lower right image is a snap shot from the OpenGL window with the resulting grid of points. The OpenGL output is normally in a separate window and manipulated with the mouse but can be optionally captured in an OpenCVB image.*

# 2023 February - Recent Changes

-   Over 1400 algorithms are included with an average of 31 lines of code per algorithm
-   Oak-D Pro and Oak-D S2 camera support is now installed by default.
    -   Oak-D Lite cameras will work but have no IMU (supported cameras typically have an IMU)
    -   Any performance or reliability improvements for the camera interface would be gratefully received.
    -   Oak-D camera point cloud is built in the host from intrinsics and depth data.
-   With the addition of the Oak-D camera support, the installation process was reviewed and simplified.
    -   One script file handles the support for the installation of all components.
    -   To refresh any of the components, delete the component’s directory and run “Update_All.bat”.
        -   Remove “\<OpenCVB Dir\>/OakD/Build” to update the Oak-D camera support
        -   Remove “\<OpenCVB Dir\>/librealsense” to update the Realsense camera support
        -   Remove “\<OpenCVB Dir\>/Azure-Kinect-Sensor-SDK” to update Microsoft Kinect for Azure support
        -   Remove “\<OpenCVB Dir\>/opencv” to update both OpenCV and OpenCV contributions.
-   Depth shadow is a challenging problem and in the example below is the beginning of a solution.
    -   The RedCloud_Simple algorithm assigns depth based on color and creates depth data for the entire image.
-   Color_Classify uses 6 methods to classify each pixel in the RGB image.
-   KMeans algorithms were reviewed and simplified with the KMeans_Basics converted to single channel input only.
-   Edge Drawing algorithms added – both line segments and edges are included.
-   A monthly history of changes is included at the bottom of this document.

![A picture containing text, indoor, display, different Description automatically generated](media/72003f6ab495cbb5a6f3f080567d5e97.png)

**RedCloud_Basics:** *Depth shadow is a significant problem – there is no depth data in the shadow of objects close to the camera because one camera cannot see what the other camera can. The depth shadow around the hand is black in the RGB representation of the depth data in the upper right. Note that the RedCloud output in the lower left has identified regions in the depth shadow of the hand. These regions are found with color – not depth. The next step is to …*

# 2023 February - Recent Changes

-   Over 1400 algorithms are included with an average of 31 lines of code per algorithm
-   Adding a new OpenCVB algorithm using the ‘Blue Plus’ button *![](media/0dede74f225b8e19e8f4fd5a50ba9f28.png)* is now expanded and easier.
-   Depth at the image edges for RealSense cameras have gaps that can be approximated.
    -   See the Guess_ImageEdge algorithm (RealSense only)
-   OpenCVB has been tested on Windows 11 without incident.
-   The current version of OpenCVB introduces heartbeats in 3 flavors:
    -   Once a second, twice per second, and “almost” heartbeat (just before a heartbeat)
    -   In addition, a new Grid_FPS allows any algorithm to specify a requested heartbeat frequency.
-   Backprojection algorithms were reviewed and 2D histogram backprojections now have a separate module.
-   Plane equations for RedCloud cells are now computed for use in OpenGL.
-   Post with any problems, especially install problems. They will receive the highest priority.
-   A monthly history of changes is included at the bottom of this document.

![A collage of images of a room AI-generated content may be incorrect.](media/cc5cb6bf2bee9d442fa760af12a3f764.png)

**Plane_Basics:** *Improvements in the RedCloud cells have made it easier to detect the plane for a cell. Selecting a cell will create a plane equation that can be used to describe the plane to OpenGL. Also included in the display is an estimate of the Root-Mean-Square error. The selected RedCloud cell is outlined in the RGB image in the upper left. In the lower left, the selected cell is highlighted in white.*

# 2023 January - Recent Changes

-   Over 1400 algorithms are included with an average of 31 lines of code per algorithm
-   Oak-D Pro camera support is now installed by default. Oak-D Lite cameras have no IMU but will work as well for most algorithms.
-   FPoly_LeftRight determines the camera motion in the left and right cameras at the same time.
-   Camera interfaces no longer need to provide the RGB Depth or Depth 16-bit buffers.
    -   The point cloud data contains the depth information in all cases but the Oak-D cameras
    -   Oak-D point cloud is built on each frame from the 16-bit depth data (and camera information)
-   A monthly history of changes is included at the bottom of this document

![Graphical user interface, website Description automatically generated](media/1090d294fefc86a41b0a05ac277d0b1f.png)

**Flood_LRMatchLargest:** *Using the Oak-D camera’s left and right images (bottom left and bottom right) the RedCloud cells can be identified in one image and matched in the other image. The approach is searching for a way to match objects in the left and right image to determine their distance. The distance will be a single number and won’t identify and variations across the cell.*

# 2023 January - Recent Changes

-   Almost 1400 algorithms are included with an average of 31 lines of code per algorithm
-   Python scripts were all moved to the end of the list of project files in Visual Studio’s Project Explorer
    -   All the Python scripts begin with “z_” to separate them from the VB.Net algorithms
    -   The documentation for using OpenCVB with Python scripts was updated (search below for “Python Interface”)
-   OpenCVB is evolving into a “layered” set of algorithms as more algorithms incorporate other algorithms.
    -   Use the TreeView button to breakdown the structure of all the contributing algorithms
    -   Click on the name of an algorithm in TreeView to see the output for that “layer” of the algorithm
    -   Read the section labelled “TreeView” below for the details and images.
-   RCR – RedCloud Recursion creates cells within a cell to help isolate surfaces.
-   A new algorithm group “\<Changed recently\>” for modules that have been recently modified.
-   A list of core “layered” algorithms is available under the heading “Cortico” (needed something unique)
-   WarpPerspective_Basics was replaced with a more targeted approach to warping an image
-   A monthly history of changes is included at the bottom of this document

![A picture containing text, indoor, screenshot, display Description automatically generated](media/20aa54d92af5420755e4f9ff108d0d09.png)

**Profile_Derivative:** *A new series of algorithms was added to work with the contour of RedCloud cells. In this example some key points on the contour of a cell are explored. The upper left image outlines in yellow the selected RedCloud cell in the RGB image. The upper right image shows the RedCloud_Basics output (click to select another cell.) The lower left image shows the derivative of the contour in depth with yellow highlighting where contour points are closest to the camera and blue shows where contour points are farther away from the camera. The information in the lower right image shows the point cloud coordinates of the rightmost, leftmost, highest, lowest, closest and farthest points (see the key in the lower right image for color definitions.)*

# 2022 December - Recent Changes

-   Over 1370 algorithms are included with an average of 31 lines of code per algorithm
-   C++ Translator: an OpenCVB-specific tool translates the VB.Net algorithms to C++
    -   The patterns used in the VB.Net code translate most of the algorithm to C++
    -   The translator is specific to the OpenCV API’s and OpenCVB’s structure
    -   The resulting C++ algorithm is similar in structure to existing VB.Net algorithms
        -   C++ “IncludeOnly” algorithms can be reused by other C++ algorithms and even VB.Net algorithms.
    -   OpenCVB’s “Algorithm Starter” tool generates the C++ template (See “Creating C++ ‘IncludeOnly’ Algorithms” section)
    -   There are now some 50 new C++ algorithms available in an “IncludeOnly” file
        -   An imgui example shows how to include all 50 algorithms in your C++ application with one include file
-   FeatureLess regions are mapped and tracked with the RedCloud image segmentation tool (example below)
-   FloodFill examples now use RedCloud_Basics for image segmentation and tracking (previously they used a similar algorithm.)
-   ColorMatch algorithms now use RedCloud_Basics as well for image segmentation and tracking.
-   A monthly history of changes is included at the bottom of this document
-   OpenCVB’s support for the Oak-D Lite and Oak-D Pro was brought up to date with the latest C++ interface (depthai-core)
    -   The Python interface is no longer needed as the C++ interface is much more direct.
    -   The Oak-D cameras are not installed by default – only Kinect for Azure and RealSense are required.
-   Oak-D testing showed that install problems were present and now resolved.
    -   Any reported problems with installation get the highest priority.
-   An excellent overview of the current 3D camera technology and 3D cameras:
    -   https://www.aivero.com/overview-of-depth-cameras/

![A picture containing text, electronics, screenshot Description automatically generated](media/ba224180471b27b6c66f1caaa140989f.png)

**CPP_RedColor_FeatureLess:** *The scene in the upper left is segmented into different featureless regions using only RGB. The image in the bottom right is the output of the edge-drawing C++ algorithm and is the input to a distance transform. A mask created by the distance transform is used to create the identified regions in the lower left image with RedCloud_Basics.*

# 2022 November - Recent Changes

-   Over 1300 algorithms are included with an average of 31 lines of code per algorithm
-   A new series of algorithms using the “feature polygon” is available for use in camera stabilization and tracking.
-   Another new series of algorithms allows C++ algorithms to be constructed and reused the same way as VB.Net algorithms.
    -   All the C++ algorithms are accessed from VB.Net through the CPP_Basics algorithm.
    -   All the C++ algorithm output is displayed in the VB.Net interface as is normally done for all algorithms.
    -   All the CPP_Basics algorithms were modeled on an equivalent VB.Net algorithm.
    -   All the CPP_Basics algorithms are available to C++ as an “include only” file – just drop it in.
    -   All the C++ algorithms can be stacked into more complex algorithms just like the VB.Net algorithms
-   The CPP_Basics algorithms are intended to export any OpenCVB algorithm to other environments.
    -   A new sample project shows how the “include only” code can be mainstreamed into an imgui C++ application.
    -   There is a new button in the interface to add algorithms conforming to the CPP_Basics style guide.
-   There were some improvements to the install process – it is no longer necessary to have MSBuild.exe in the path.
    -   OpenCVB’s install process assumes Visual Studio 2022 Community Edition is installed in the default location.
    -   For alternate Visual Studio editions, a change is needed to the “Update_\<package\>.bat” files.
-   There was no update to this ReadMe.md in October.
-   Install problems? Pull requests for install problems will get the highest priority.
    -   NOTE: the current CMake RC 3.25 will fail to install OpenCV. Use the latest release (3.24.3).
-   A complete history of changes is included at the bottom of this document

![A picture containing text, electronics, display, screenshot Description automatically generated](media/d36c247345b3c413bcfd0838c9c46d1a.png)

**FPoly_Basics:** *The FPoly (Feature Polygon) series of algorithms use the “good” features of the current image to create a vector describing the orientation of the camera. The white double bar line was captured in an earlier frame while the yellow double bar line is the current orientation of the same vector. A rotate and shift allows a rough comparison between frames separated by time. The values in the figure in the bottom left indicate how many generations (or frames) that the Delaunay region has been present in the image. The older the polygon, the more stability the feature polygon will exhibit. In the lower left image, the black region (highlighted with a yellow edge) shows the oldest of the regions.*

# 2022 September - Recent Changes

-   Over 1200 algorithms are included with an average of 31 lines of code per algorithm
-   BackProject_Full builds a histogram and backprojects it into a segmented color image
    -   A median filter removes most of the textured output
    -   At low resolution, the BackProject_Full has only a minimal impact on performance.
-   RedCloud_Hulls output can be input to OpenGL and provide a 3D model of objects in the scene
    -   The scene is rendered in OpenGL as 3D solids, not just a point cloud
-   All options were updated to simplify the search for sliders, checkboxes, and radio buttons.
    -   No more indexed references to any of the option controls
    -   The options for sliders can now access any number of trackbars using a scroll bar.
    -   Options are no longer counted in the lines of code or algorithm count
-   Floor and ceiling are automatically identified using the point cloud and rendered in OpenGL
    -   See “OpenGL_FlatFloor” and “OpenGL_FlatCeiling”
-   OpenGL Models include both quads and tiles
-   Additional improvements to the snippets files for adding Options
    -   The new “Add Algorithm” button in the main toolbar was also improved
-   A complete history of “Recent Changes” is included at the bottom of this document

![A screenshot of a computer AI-generated content may be incorrect.](media/2e2543648cac643698d4f2cf12b5886f.png)

**RedCloud_ColorAndCloud:** *The output of the “BackProject_Full” can be input into the RedCloud image segmentation and the results are in the lower left image. Previously, the reduced point cloud was the source of all the RedCloud input – it is shown on the lower right image. With the latest version of OpenCVB, both the color data and depth data can be used to segment the image.*

# 2022 August - Recent Changes

-   All the OpenGL algorithms were reviewed and updated with many new features and a simpler interface.
    -   More code reuse was main objective
    -   New OpenGL algorithms can be added automatically with the new toolbar button.
    -   The OpenGL app location is remembered across runs; the OpenGL window will open where it was last used.
    -   New OpenGL algorithms depict the 3D scene with color-coded triangles and quads.
    -   Beginner OpenGL algorithms for a colored cube and pyramid were added.
-   The interfaces for algorithm sliders, checkboxes, and radio buttons are now simpler and easier to use.
    -   Scrollbars now provide access to overflow options when there are too many to fit in the options form.
    -   Updated snippets reflect the changes to the algorithm options
-   RedCloud Hulls improve on cells by creating OpenCV hulls for each cell.
-   RedCloud cells now have more accurate min and max depth values.
    -   Depth min is never zero
    -   Depth max is trimmed using the standard deviation limits
-   OpenCVB was tested under Windows 11 – no changes required
-   A complete history of “Recent Changes” is included at the bottom of this document

![A screenshot of a computer AI-generated content may be incorrect.](media/0bd64f583dbd5f5c9cbad2fd15a3883e.png)

**OpenGL_QuadMinMax:** *The RedCloud image cells (lower left) are presented in OpenGL (lower right) as OpenGL quads – rectangles with min and max depth. The colors in the RedCloud cells are the same as those in the OpenGL presentation. The highlighted cell shown in white in the lower left is also shown as white in the OpenGL presentation in the lower right.*

# 2022 July - Recent Changes

-   Install tested with the latest Visual Studio 2022 (17.2.6) and OpenCV 4.6 release
    -   Keep Visual Studio 2019 around – only way to keep the .Net Framework 4.0 (required for librealsense)
    -   OpenCV needed to be cmake’d and built manually – problem in OpenCVUtils.cmake (?)
-   Over 1200 working algorithms in a single app with average length 31 lines of code.
-   4 tutorials describing how to build new algorithms. See the ‘Tutorials’ directory.
-   “QT” algorithms were introduced to help reduce the clutter in the options presentation. Some options don’t always need to be present.
    -   Full option versions of the algorithm are available with the same name – without the “QT” on the end.
    -   Want to see all the QT options? See Global Setting “Show Quiesced (QT) Options”.
-   Expanded IMU alternatives are available. A tutorial describes how to access and compare the choices.
-   New icon in the main toolbar –blue with a white plus sign. See the toolbar in the image below. What does it do?
    -   Clicking “plus” will inject the infrastructure for a new algorithm – better than a snippet.
    -   Open the dialog and then select the type of new algorithm: VB.Net, C++, OpenGL, C\#, or Python
    -   Restart OpenCVB and the new algorithm is ready to run!
-   OpenGL examples were reworked to make it easier to create new OpenGL algorithms and reuse more code.
-   RGB lines are classified as horizontal or vertical using the point cloud – see image below.

![A picture containing text, indoor, electronics, computer Description automatically generated](media/6b4cff83e9554da8dc0c5c02ca88d50c.png)

**OpenGL_3DLines:** *In the lower left, the horizontal lines are shown in yellow and the vertical lines in blue. In the lower right, the OpenGL output shows the vertical lines in 3D. The OpenGL point cloud is reoriented to gravity so the lines can be verified as vertical or horizontal.*

# 2022 June - Recent Changes

-   Over 1200 working algorithms. Average algorithm length is 31 lines of code.
-   A new tutorial was added describing how to find the longest vertical line:
    -   <https://github.com/bobdavies2000/OpenCVB/tree/master/Tutorials/(3)%20Longest%20Vertical>
    -   Several new algorithms were add including one using OpenGL to display the 3D results.
-   A new tutorial was added describing how to find vertical and horizontal lines in 3D
    -   https://github.com/bobdavies2000/OpenCVB/tree/master/Tutorials/(2)%20Lines
    -   Accompanying algorithms were also added to Feature.vb – Feature_Lines_Tutorial1, Feature_Lines_Tutorial2, and Feature_Lines
-   There is now a global option to use high Brightness/Contrast image input. See Entropy_Highest and toggle the global “Use RGB Filter” checkbox.
-   Similarly, there are several other global options to sharpen detail or adjust white balance or filter the RGB data.
    -   Any algorithm can be tested with the altered RGB input.
    -   Additional RGB filters may be added with a single line of code
    -   The default is to not use any RGB Filter
-   Double pendulum algorithm added using GitHub example code.
-   Highlight color is automatically switched to handle variable backgrounds.
-   Robust line-tracking is available in the Feature_Line algorithm
-   Robust point-tracking is available in Feature\_ MatchTemplate (see next image)

![A person sitting at a desk Description automatically generated with medium confidence](media/59cba4a31bb6db590f1854dd0930a298.png)

**Feature_MatchTemplate:** *In this example, the highlighted rectangles in the left image are tracked. The correlation coefficient for each rectangle is in the right image. When a correlation coefficient drops below a threshold value (see “Match_Options”), the tracked point is dropped. If more than a percentage of the tracked points are lost, tracked points are recomputed using OpenCV’s GoodFeatures.*

![A picture containing text Description automatically generated](media/8459e8185e6c564e6817463f2a28defd.png)

**Related_MouseClick:** *In this example, 4 different algorithms are featured. They are “Related” algorithms in the sense that all 4 use the mouse to perform various tricks – the upper left image uses the mouse to highlight an entry in the histogram for back projection, the upper right uses the mouse to slice through the projected side view, the lower left provides the back projection for the mouse selection from the histogram (look at the floor), and finally the lower right uses the mouse for both the x and y coordinates to use in a 2D histogram. The “Related” series of algorithms might be a good place to start looking when the output is remembered but not the name of the algorithm. The Related algorithms are new with the June release of OpenCVB.*

# 2022 May - Recent Changes

-   The first tutorial on OpenCVB is now available in the OpenCVB tree. See “Tutorial – Introduction”
-   EMax algorithms reviewed – now more general and useful with consistent colors. A new example is provided.
-   Related algorithms in different series can now be merged and presented simultaneously – see “Related.vb” examples.
-   Global options are now reset to default values before each algorithm is started.
-   Updated KNN_One_To_One – simpler to use
-   Lines in an image can now be identified and tracked
-   LaneFinder example identifies road lane markers (used for the new tutorial.)
-   Thread_Grid output now available during all algorithms.
-   To see previous editions of the “Recent Changes”, see Addendum 3 below
-   Install problems get high priority. Please notify of any problems with ‘UpdateAll.bat”
-   OpenCV’s “GoodFeatures” can now be traced as the camera moves – see below.

![A picture containing text, indoor Description automatically generated](media/d6e10c5e6c55c695cf34438913f1142c.png)

*In this example from “Features_GoodFeatureTrace”, the upper left image shows the good features to track in the current image. The lower right image shows a trace of those same points as the camera is rotated. The motion of the camera is more pronounced in the lower left image.*

# 2022 February - Recent Changes

-   Switched to Visual Studio 2022! The Community Edition is free and an easy install.
    -   Post with any problems transitioning to VS 2022. They will be high priority.
-   Further testing and improvements to the Oak-D interface
-   Most changes were focused on the RedCloud algorithms to build consistent cells in an image.
-   Point cloud filtering is available for each frame as an option
-   Added a heartbeat available to all algorithms for once-a-second activity.
-   Added a global motion test to update a motion threshold flag available to all algorithms. Redo an image if flag is set.

![A collage of pictures of a person playing a piano Description automatically generated with low confidence](media/e157d44e1f6726405eba1755fb3e652c.png)

*In this first version of the Match_TrackFeatures algorithm, OpenCV”s MatchTemplate (correlation coefficient calculator) is used to track the output of OpenCV’s GoodFeatures. The points are matched with the previous frame using KNN 1:1 matching. In the lower right image, the blue dots were matched to the previous frame while the yellow dots were not matched. In the lower left frame, the correlation coefficient of the region around the feature is shown using the previous and current frame.*

# 2022 April - Recent Changes

-   KNN examples now have higher dimensions – 2, 3, 4 and N dimensions.
-   KNN improvement provides a 1:1 matching of points in a series of images.
-   Options handling is a key feature of OpenCVB. All algorithms now have a single flag that indicates any options were updated.
-   OpenCV’s MatchTemplate can be used for tracking. See Match_TrackFeatures and image below.
-   SVM algorithms were reviewed and simplified with better presentation.
-   To see previous editions of the “Recent Changes”, see Addendum 3 below
-   Install problems get high priority. Please notify of any problems with ‘UpdateAll.bat”

# 2022 March - Recent Changes

-   Reviewed and reworked the RedCloud algorithms for speed and simplicity.
-   Convex Hull algorithm applied to the RedCloud output (see below)
-   New ML_Basics example
-   Simplified KNN interface and added working examples of 3- and 4-dimensional data.
-   Reviewed and reworked all the FloodFill algorithms. Code structure is now like RedCloud algorithms.
-   More TrueType text usage throughout.

![Graphical user interface, application Description automatically generated](media/5db655fc1b209508cc62adeeab3f7a2a.png)

*The image in the bottom left is the output of the RedCloud_Basics algorithm where each cell is consistently identified by applying reduction in X and Y to the point cloud image. The bottom right image shows the RedCloud output after processing with OpenCV’s ConvexHull API. The upper left and right images appear with every algorithm – RGB on the left and the point cloud on the right.*

![A picture containing text, indoor, computer, screenshot Description automatically generated](media/cfc5629359716924e7bb4139f6115a96.png)

*The images above were captured at the same time as the OpenGL image above. The upper left image is the RGB captured from the Oak-D Lite camera and the upper right is the point cloud (computed on the host using the calibration metrics provided by the camera.) The bottom left image is a representation of the depth data used to create the point cloud.*

# 2022 January - Recent Changes

-   Oak-D and Oak-D Lite support is now included.
-   All OpenCVB’s 1100+ algorithms are now accessible using the Oak-D and Oak-D Lite cameras
-   Oak-D installation automated with “Update_OakD.bat”
-   Oak-D point cloud is created on the host from the captured depth data
-   Oak-D cameras are supported through a separate Python process using a pipe to move images to OpenCVB
    -   Breakpoints do not interfere with camera image capture as it would if depthai-core provided the camera images
    -   RGB, depth, disparity, and rectified left and right images are provided on every iteration.
    -   Oak-D Lite has no IMU but IMU data is provided for the original Oak-D camera with every frame
    -   Calibration data for the RGB camera is available as well (used for the point cloud computation.)
-   OpenCVB’s “RGB depth” image (upper right) now represents the point cloud data – useful data (not just a pretty picture)
-   The Python interface for OpenCVB is now built with requirements for Python 3.9. See installation instructions in “Python Installation” below.

# OpenGL View of Oak-D Lite Point Cloud

![A picture containing text Description automatically generated](media/5eeb6559193188b7fd363812509ccf9a.png)

*An OpenGL screen capture shows the output of the point cloud data from an Oak-D Lite camera.*

# 2021 November - Recent Changes

-   Almost 1100 algorithms – almost all less than a page of code. Average algorithm is 32 lines of code
-   This version includes point cloud heat maps (see Highlight below)
-   All the Structured.vb algorithms were updated to use heat maps
-   TimeView algorithms were removed now that heat maps are available.
-   Updated to use the latest RealSense interface.

# Feature Highlight – Point Cloud Heat Maps

![A screenshot of a computer Description automatically generated with low confidence](media/19939738c000ae1e2b5ba1f422a0d0b1.png)

*The bottom left image is a heat map for the side view of the point cloud data while the bottom right image is the same but for the top down view.*

The heat map is a well-known method to display populations – blue is cool or low population while red is hot and high population. The plots are actually just histograms of the point cloud data projected into a plane at the side (bottom left) and top (bottom right) of the point cloud. The field of view of the camera is outlined in the markings and the distances are noted as well. The projection can be rotated to align with gravity. The short red lines emanating from the camera can show the field of view (FOV) after rotation. The snapshot was taken using the low-resolution Intel RealSense D435i.

# 2021 November - Recent Changes

-   Almost 1100 algorithms – almost all less than a page of code. Average algorithm is 31 lines of code
-   The reduced point cloud predictably divides an image for analysis.
-   “Reuse Rank” in algorithm groups shows all algorithms reused at least twice.

# Feature Highlight – OpenCVB Algorithm Rank

![Graphical user interface Description automatically generated](media/68cd4fbfec6491a47bd5299edef1a617.png)

**New to OpenCVB?***: start with the Algorithm Ranks*

With over a thousand algorithms in OpenCVB, it can be overwhelming for a new user to explore. To help, there are 2 kinds of rankings inside OpenCVB. The “Reuse Rank” shows how often algorithms reuse another algorithms. This is a useful measure of how general or useful the algorithm is. The “Value Rank”, on the other hand, is manually inserted in each algorithm. The snippet code automatically assigns a value of 1 to a new algorithm since this is the lowest ranking. There is no upper limit on the Value Rank.

What is the algorithm most often reused in OpenCVB? The “Thread_Grid” which is used to divide up images for use with multi-threading. To see all the algorithms using this algorithm, select “Thread_Grid” in the rightmost combo box. The leftmost combo box will show all the algorithms that use “Thread_Grid”. The second highest “Reuse Rank” has 2 entries – “Kalman_Basics” and “Reduction_Basics”. Both are often used throughout OpenCVB. Setting the rightmost combo box to “Kalman_Basics” will update the leftmost combo box with the list of all algorithms using “Kalman_Basics”. Similarly, setting the rightmost combo box to “Reduction_Basics” will update the leftmost combo box with the list of all the algorithms using “Reduction_Basics”.

The Value Rank is manually updated so there are some lag between an algorithm’s arrival and an update to its value rank.

# 2021 September - Recent Changes

-   Almost 1100 algorithms – almost all less than a page of code. Average algorithm is 31 lines of code
-   Improvements to the TreeView indicate how many cycles are available (see Highlight below.)
-   The reduced point cloud predictably divides an image for analysis.
-   “Reuse Rank” in algorithm groups shows all algorithms reused at least twice.
-   Quarter Resolution option finds bottlenecks without code change.
-   First example of using low resolution internally while displaying full resolution
-   RGB Depth can be displayed with numerous different palettes. You can create your own.

# 2021 July - Recent Changes

-   Over 1000 algorithms – almost all less than a page of code. Average algorithm is 31 lines of code
-   TreeView now shows algorithm cost in addition to algorithm components
-   Improved intermediate views – click anywhere in TreeView to see intermediate outputs
-   Depth Object algorithm identifies areas of interest 4 different ways with mask and enclosing rectangle
-   All algorithms can extend their output to all 4 images (only 2 were available before)
-   Upgraded to the latest versions of OpenCV, librealsense, and Kinect4Azure libraries
-   Framerate for all cameras upgraded to 60 fps

# New Feature Highlight – TreeView

![A screenshot of a computer Description automatically generated](media/80ad200f3aea31fd66d4a388697cded6.png)

The Tree View shows the cost of each component of the structure of the algorithm on the left side and the cost of each component on the right side. The active algorithm here was “RedCloud_BasicsColor” – also the top entry in the tree view at the left. The frame rate for the algorithm is shown at the top right.

The ‘waitingForInput’ entry is important to understanding performance. The percentage is often near zero and would indicate that the algorithm is processor-bound. If the value is significantly far from 0, it would indicate that the algorithm is I/O bound.

The Tree View provides a structure and performance analysis for every algorithm in OpenCVB – automatically.

# 2021 May - Recent Changes

-   980 algorithms – almost all less than a page of code
-   Global variables introduced – settings that apply to all algorithms, line width, max depth, font size.
    -   Global variables are remembered across runs and can be reset to known working values
-   Fewer lines of code. Code size dropped about 4000 lines with more algorithms. Average algorithm: 31 lines.
-   Algorithms are now ranked by usage (“Reuse Rank”) and “Value Rank”, a graded estimate of algorithm value.
    -   Rankings are entries in the Group ComboBox.
-   New Survey function to build images of all algorithm output to allow visual searches for desired algorithm.
-   Global setting for palette control
-   Improved regression testing – all algorithms are tested with each attached camera at all supported resolutions.
-   Navigation aids now available – back to previous algorithm, forward to next, and full history.
-   Image microscope works even when stream is paused, allowing more detailed image analysis.
-   Improved tree view to study how algorithm was constructed from other algorithms.
-   

# 2021 March - Recent Changes

-   Almost 940 algorithms – almost all less than a page of code
-   Stream-lined install: no environmental variable, library builds are automated.
-   Latest version of the OpenCV library - 4.5.2
-   Improved Python support – now using “requirements.txt”
-   An experimental Python interface to the LibRealSense2 cameras has been added
-   VTK support is being dropped – it is too big and cumbersome. Recommended: Python Pyglet
-   Oak-D camera Python interface is present but turned off pending IMU support from the vendor
-   “PyStream” support is now a 2-way pipeline. Output can appear in the OpenCVB interface
-   Tensorflow database downloads are automated with algorithm “Download_Database”
-   Emgu examples removed. LineDetector library removed – it was redundant
-   Version 1.0.0 defined and released

# 2021 February - Recent Changes

-   Over 900 algorithms – almost all less than a page of code
-   New pixel viewer to inspect image pixels in 8UC1, 8UC3, 32F, and 32FC3 formats
-   Versioning policy set - The Repository IS The Release - TRISTR
-   Improved threading support for switching between camera interfaces
-   Oak-D camera support is working – still missing IMU and point cloud support
-   VTK support was improved – still optional (it is a lot to install)
-   Upgraded to the latest RealSense2, OpenCVSharp, and Kinect4Azure software
-   Motion Filtered Data series of algorithms – an attempt at reducing data analysis at input

# 2021 January - Recent Changes

Over 870 algorithms – almost all less than a page of code.

-   The new “Best Of” module contains the best example of common techniques. Need an example of contours, look in the BestOf.vb first.
-   OpenCV’s new Oak-D camera has arrived. Some python scripts were added for users that have installed it.
    -   <https://docs.luxonis.com/en/latest/pages/api/> - to get the official support.
-   Motion detection is easier to use with an “AllRect” cv.rect that encompass all RGB changes.
-   Image segmentation is more stable and consistent from frame to frame. See ImageSeg.vb.
-   OptionsCommon.vb defines options common to all algorithms.
-   StructuredDepth shows promise as a path to exploiting structured light technology.
-   PythonDebug project is now integrated into the OpenCVB.sln. Python debugging is easier.

# 2020 December - Recent Changes

-   Over 800 algorithms – almost all less than a page of code.
-   Depth updates are guided by motion – produces more stable 3D images. See Depth_SmoothMin algorithm.
-   Recently used algorithms are listed in the menus.
-   More snippets to help adding options to existing algorithms.
-   Algorithm options are now collected in a single form – easier usage on laptops or smaller screens.
-   Intel Realsense cameras are supported in native 640x480 modes (as well as 1280x720.)

# 2020 September - Recent Changes

-   Dropped support for Intel T265 camera (no point cloud) and the Intel RealSense L515 (no IMU). All supported cameras should have a point cloud and IMU.
-   TreeView – some of the algorithms are a combination of several other algorithms. A TreeView was built to display the hierarchy.
-   There are now over 750 algorithms implemented.
