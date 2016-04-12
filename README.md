# VirtualFridge sample

**VirtualFridge** is a Universal Windows Platform (UWP) app sample that explores different input modalities and scenarios of user awareness. The VirtualFridge sample is essentially a bulletin board app that allows family members to leave notes for each other on a common PC/tablet just like they would a fridge. Using text, speech, ink, or pictures, a user is able to create a note and tag it for another user. Later when that other user approaches the PC/Tablet, the app uses imaging APIs and the Microsoft Cognitive Services (Face API) to detect their presence and display the notes that have been left for them, effectively filtering based on facial recognition. While the app is open, it can be naturally interacted with using speech (“Add note for Bob”). If the app isn’t open, it can easily be launched and interacted with using Cortana.

 Be aware that the image understanding capabilities of the **VirtualFridge** app use Microsoft Cognitive Services. Microsoft will receive the images and other data that you upload (via this app) for service improvement purposes. To report abuse of the Microsoft Face APIs to Microsoft, please visit the Microsoft Cognitive Services website at www.microsoft.com/cognitive-services, and use the “Report Abuse” link at the bottom of the page to contact Microsoft. For more information about Microsoft privacy policies please see the privacy statement here: http://go.microsoft.com/fwlink/?LinkId=521839.



![VirtualFridge MainPage](Screenshots/VirtualFridge.PNG)


## Features

The VirtualFridge app demonstrates:

* Speech recognition and speech synthesis by using the [SpeechRecognizer](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.speechrecognizer.aspx) and [SpeechSynthesizer](https://msdn.microsoft.com/library/windows/apps/windows.media.speechsynthesis.speechsynthesizer.aspx) classes.  
* User detection using the [MediaCapture](https://msdn.microsoft.com/library/windows/apps/windows.media.capture.mediacapture.aspx) and [FaceDetectionEffect](https://msdn.microsoft.com/library/windows/apps/windows.media.core.facedetectioneffect.aspx) classes.  
* User facial recognition using the [Microsoft Face API](http://www.microsoft.com/cognitive-services/en-us/face-api).
* Activation through Cortana voice commands, defined in VoiceCommands.xml (a [VCD](https://msdn.microsoft.com/library/windows/apps/dn706593) file), using [VoiceCommands](https://msdn.microsoft.com/library/windows/apps/Windows.ApplicationModel.VoiceCommands.aspx) and [Activation](https://msdn.microsoft.com/en-us/library/windows/apps/windows.applicationmodel.activation.aspx) classes.
* Pen input using the [InkCanvas API](https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.inkcanvas.aspx)
* JSON serialization using the [DataContractJsonSerializer](https://msdn.microsoft.com/en-us/library/system.runtime.serialization.json.datacontractjsonserializer.aspx) class.
* Setting the fridge wallpaper using the [Bing image of the day task snippet](https://github.com/Microsoft/Windows-task-snippets/blob/master/tasks/Bing-image-of-the-day-URI.md).

## Universal Windows Platform development

This sample requires Visual Studio 2015 and the Windows Software Development Kit (SDK) for Windows 10.

[Get a free copy of Visual Studio 2015 Community Edition with support for building Universal Windows apps](http://go.microsoft.com/fwlink/?LinkID=280676)

Additionally, to be informed of the latest updates to Windows and the development tools, join the [Windows Insider Program](https://insider.windows.com/ "Become a Windows Insider").

## Running the sample

The default project is VirtualFridge and you can Start Debugging (F5) or Start Without Debugging (Ctrl+F5) to try it out. The app will run in the emulator or on physical devices, though functionality related to speech and face recognition is dependent on hardware support and the app has not yet been designed for a phone layout. Specific requirements for full functionality are:
### User filtering by facial recognition
* A front-facing camera or USB webcam.
* A subscription key for the Microsoft Face API. For information about getting a free trial key, see the [Microsoft Cognitive Services site](http://www.microsoft.com/cognitive-services/en-us/sign-up).
* A user created with a profile picture for your phase, or an user you want to be recognized.

**Note**: The Microsoft Face API subscription key must be entered in the Settings menu of the app before facial recognition can be used. The settings menu is opened by clicking the gear button on the apps command bar.
### Speech recognition
* A microphone and the appropriate settings enabled on the local machine.

###  Cortana
* The app must be launched once to register the Cortana voice commands for subsequent activation through Cortana.
* The Cortana voice command phrase list is updated dynamically whenever a family member is added or removed.

## Articles

The VirtualFridge app illustrates a number of platform features. For more detailed articles about those features and their use within the app, see the following articles.
* [Data bind an InkCanvas control](DatabindInkCanvas.md)
* [Serializing the model and InkCanvas data](Serialization.md)
* [Using speech for note taking](Speech.md)
* [Using the camera, imaging, and the Microsoft Cognitive Services (Face API) for facial recognition](CameraImagingRecognition.md)

## Code at a glance

If you are interested in code snippets and don’t want to browse or run the full sample, check out the following files for examples of some highlighted features:

* [Settings.cs](VirtualFridge/Settings.cs) : Downloads the Bing image of the day and allows for app config such as storing the developer key for the Microsoft Face API.
* [BindableInkCanvas.cs](VirtualFridge/Controls/BindableInkCanvas.cs) : An `InkCanvas` control with a bindable `InkStrokeContainer`.
* [Utils.cs](VirtualFridge/Utils.cs) : Delete a directory and its contents.
* [App.xaml.cs](VirtualFridge/App.xaml.cs) : Saves/loads the people and their notes. Demonstrates serialization and how to handle saving multiple`InkStrokeContainers`to a stream.
* [AddPersonContentDialog.xaml.cs](VirtualFridge/AppDialogs/AddPersonContentDialog.xaml.cs) : Contains the add person dialog, which has an option to take a snapshot for a user when adding him or her. This picture is taken using the [CameraCaptureUI](https://msdn.microsoft.com/en-us/library/windows/apps/windows.media.capture.cameracaptureui.aspx).
* [UserPresence.cs](VirtualFridge/UserDetection/UserPresence.cs) : Contains the code that is responsible for taking pictures in the background. These pictures are then used for user identification.
* [FacialSimilarity.cs](VirtualFridge/UserDetection/FacialSimilarity.cs) : Contains the code used to interact with the Microsoft Face APIs for the purpose of comparing a dynamically captured user image against a list of known users to obtain the most likely user present.

## See also
[Microsoft Cognitive Services](http://www.microsoft.com/cognitive-services)

[Cortana interactions](https://msdn.microsoft.com/en-us/windows/uwp/input-and-devices/cortana-interactions)  

[Cortana voice command sample](http://go.microsoft.com/fwlink/p/?LinkId=619899)

[Pen and stylus interactions](https://msdn.microsoft.com/en-us/windows/uwp/input-and-devices/pen-and-stylus-interactions)  

[Simple ink sample](http://go.microsoft.com/fwlink/p/?LinkID=620312)

[Complex ink sample](http://go.microsoft.com/fwlink/p/?LinkID=620314)

[Speech recognition and synthesis sample](https://github.com/Microsoft/Windows-universal-samples/tree/master/Samples/SpeechRecognitionAndSynthesis)
