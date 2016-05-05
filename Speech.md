## Speech input

The FamilyNotes app shows how to use [speech in a UWP app](https://dev.windows.com/speech). Speech input is handled in the [SpeechManager](FamilyNotes/Speech/SpeechManager.cs) class, which uses the
[SpeechRecognizer](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.speechrecognizer.aspx) in the
[Windows.Media.SpeechRecognition](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.aspx) namespace to analyze the sound stream from the microphone.

**Note:** Don't confuse this implementation of speech recognition with the desktop speech recognizer in the [System.Speech.Recognition](https://msdn.microsoft.com/library/system.speech.recognition.aspx) namespace.  

The FamilyNotes app shows how to implement two kinds of speech input: *command phrases* and *dictation*. The user talks to the app with phrases like, "Add note to John", which creates
a new note addressed to the appropriate person. The user can say other commands, like "Edit note" and "Delete note".

When the user creates a new note by using speech input, the app enters dictation mode, which enables the user to dictate the content of the note. When the user has finished dictating,
the app automatically returns to listening for command phrases. Much of the state management in SpeechManager involves setting the input mode of the
[SpeechRecognizer](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.speechrecognizer.aspx) from commands to dictation and back.  

## Setting the speech input mode  
Control the speech input mode in the [SpeechManager](FamilyNotes/Speech/SpeechManager.cs) class by calling the [SetRecognitionMode](FamilyNotes/Speech/SpeechManager.cs#L118) method. Pass the desired [SpeechRecognitionMode](FamilyNotes/Speech/RecognitionMode.cs) value and await the call.
When it returns, the speech recognizer is in a new recognition session and is listening for the specified mode of speech input.
Currently, only two modes are supported: CommandPhrases and Dictation. The two speech modes are represented by two different sets of *constraints*,
and the speech recognizer compiles the constraints when you call the [CompileConstraintsAsync](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.speechrecognizer.compileconstraintsasync.aspx) method. When this method completes, you start the recognition session
by calling the StartAsync method on the speech recognizer's [ContinuousRecognitionSession](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.speechrecognizer.continuousrecognitionsession.aspx) object.

The `SpeechManager` provides state management around these operations. Before a new recognition session can start, any running session must end. The EndRecognitionSession method
calls the [StopAsync](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.speechcontinuousrecognitionsession.startasync.aspx) or [CancelAsync](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.speechcontinuousrecognitionsession.cancelasync.aspx) method, depending on the state of the speech recognizer: if it's Idle, which means that it's not processing audio input, the session is stopped;
otherwise, the session and any current processing are canceled. Calling `StopAsync` and `CancelAsync` when the speech recognizer is in an incompatible state raises an `InvalidOperationException`,
so one of main purposes of SpeechManager is to ensure that these methods are called correctly, depending on the session state and the speech recognizer state.
``` csharp
private async Task EndRecognitionSession()
{
    // Detach event handlers.
    SpeechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
    SpeechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;

    // Stop the recognition session, if it's in progress.
    if (IsInRecognitionSession)
    {
        try
        {
            if (SpeechRecognizer.State != SpeechRecognizerState.Idle)
            {
                await SpeechRecognizer.ContinuousRecognitionSession.CancelAsync();
            }
            else
            {
                await SpeechRecognizer.ContinuousRecognitionSession.StopAsync();
            }

            IsInRecognitionSession = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }
}
```

The [StartRecognitionSession](FamilyNotes/Speech/SpeechManager.cs#L127) method manages the session state and the speech recognizer state, and the `SetRecognitionMode` method calls
the `StartRecognitionSession` method to start a new session for the requested mode.
Compiling constraints for the new mode is potentially a high-latency operation, and it's easy for various threads to call this method concurrently,
so `SpeechManager` provides a [Mutex](FamilyNotes/Speech/SpeechManager.cs#L678) property, which is implemented by using the [SempahoreSlim](https://msdn.microsoft.com/library/windows/apps/system.threading.semaphoreslim.aspx) class,
to serialize access to this method. The mutex allows only one thread at a time to execute the `StartRecognitionSession` code path.

The following code example shows how the `StartContinuousRecognition` method manages state for the speech recognizer. For the full code listing, see [SpeechManager](FamilyNotes/Speech/SpeechManager.cs).

``` csharp
public async Task StartContinuousRecognition()
{
    await Mutex.WaitAsync();

    // End the previous speech recognition session.
    await EndRecognitionSession();

    try
    {
        // If no mic is available, do nothing.
        if (!await IsMicrophoneAvailable())
        {
            return;
        }

        // Compile the grammar, based on the value of the RecognitionMode property.
        await CompileGrammar();

        // You can attach these event handlers only after the grammar is compiled.
        SpeechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
        SpeechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;

        // Start the recognition session.
        await SpeechRecognizer.ContinuousRecognitionSession.StartAsync();

        // Keep track of the the recognition session's state.
        IsInRecognitionSession = true;
    }
    catch (Exception ex)
    {
        Debug.WriteLine("SpeechManager: Failed to start continuous recognition session.");

        var messageDialog = new Windows.UI.Popups.MessageDialog(
            ex.Message,
            "Failed to start continuous recognition session");
        await messageDialog.ShowAsync();
    }
    finally
    {
        Mutex.Release();
    }
}
```

## Speech recognition results

Harvest results from the speech recognizer by handling the session's [ResultGenerated](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.speechcontinuousrecognitionsession.resultgenerated.aspx) event. The `SpeechManager` class handles results in the [ContinuousRecognitionSession_ResultGenerated](FamilyNotes/Speech/SpeechManager.cs#L505) method.
Other events of interest:

- [StateChanged](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.speechrecognizer.statechanged.aspx): Occurs when a change occurs to the State property during audio capture.
- [HypothesisGenerated ](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.speechrecognizer.hypothesisgenerated.aspx) Occurs during dictation, when a recognition result fragment is returned by the speech recognizer. Useful for showing that speech recognition is working during a lengthy dictation session.
- [Completed](https://msdn.microsoft.com/library/windows/apps/windows.media.speechrecognition.speechcontinuousrecognitionsession.completed.aspx) Occurs when a continuous recognition session ends.

## Speech prompts

The FamilyNotes app uses speech synthesis to provide spoken prompts to the user. For example, when the user creates a new note by saying, "Add note for John", the app prompts
by saying, "Dictate your note". If the user says, "Delete note", the app deletes the active note and says, "Note deleted". Also, the user can say "Read note",
and the app reads back the note text to the user.

The [SpeechSynthesizer](https://msdn.microsoft.com/library/windows/apps/windows.media.speechsynthesis.speechsynthesizer.aspx) class provides
the [SynthesizeTextToStreamAsync](https://msdn.microsoft.com/library/windows/apps/windows.media.speechsynthesis.speechsynthesizer.synthesizetexttostreamasync.aspx) method
to create a playable sound stream from the specified text. The [SpeechManager.SpeakAsync](FamilyNotes/Speech/SpeechManager.cs#L198) method passes this stream to a [MediaElement](https://msdn.microsoft.com/library/windows/apps/windows.ui.xaml.controls.mediaelement.aspx)  
to play the stream.

The `SpeakAsync` method is awaitable, because in the case of a speech prompt, the speech recognizer can hear the prompt and may process it,
along with the user's speech. The FamilyNotes app avoids this bug by awaiting the call to the `SpeakAsync` method and setting [RecognitionMode](FamilyNotes/Speech/SpeechManager.cs#L104)
to `SpeechRecognitionMode.Dictation` after it completes. This way, the speech prompt ends before recognition begins.

To make the the `SpeakAsync` method avaitable, `SpeechManager` uses the [SemaphoreSlim](https://msdn.microsoft.com/library/windows/apps/system.threading.semaphoreslim.aspx) class to implement a signal
from the [MediaElement.MediaEnded](https://msdn.microsoft.com/library/windows/apps/windows.ui.xaml.controls.mediaelement.mediaended.aspx) event to the `SpeakAsync` method.

The following code example shows how the `SpeakAsync` method uses a semaphore to wait for the `MediaElement` to finish playing
a speech prompt. For the full code listing, see [SpeechManager](FamilyNotes/Speech/SpeechManager.cs).
``` csharp
public async Task SpeakAsync(string phrase, MediaElement media)
{
    if (!String.IsNullOrEmpty(phrase))
    {
        // Turn off speech recognition while speech synthesis is happening.
        await SetRecognitionMode(SpeechRecognitionMode.Paused);

        MediaPlayerElement = media;
        SpeechSynthesisStream synthesisStream = await SpeechSynth.SynthesizeTextToStreamAsync(phrase);

        // The Play call starts the sound stream playback and immediately returns,
        // so a semaphore is required to make the SpeakAsync method awaitable.
        media.AutoPlay = true;
        media.SetSource(synthesisStream, synthesisStream.ContentType);
        media.Play();

        // Wait until the MediaEnded event on MediaElement is raised.
        await Semaphore.WaitAsync();

		// Turn on speech recognition and listen for commands.
        await SetRecognitionMode(SpeechRecognitionMode.CommandPhrases);
    }
}
```
