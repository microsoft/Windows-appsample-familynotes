//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

//#define VERBOSE_DEBUG

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;
using Windows.Media.Capture;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace FamilyNotes
{
    /// <summary>
    /// Provides speech recognition and speech synthesis services 
    /// for the FamilyNotes app.
    /// </summary>
    public class SpeechManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechManager"/> class. 
        /// </summary>
        /// <param name="model">The app's data model, which has a collection 
        /// of <see cref="Person"/> instances.</param>
        public SpeechManager(Model model)
        {
            if (model != null)
            {
                Family = model.Family;
                Family.CollectionChanged += Family_CollectionChanged;
            }
            else
            {
                throw new ArgumentNullException("model", "Model can't be null");
            }
        }

        private void InitializeRecognizer()
        {
            try
            {
                // Initialize resource map to retrieve localized speech strings.
                Language speechLanguage = SpeechRecognizer.SystemSpeechLanguage;
                string langTag = speechLanguage.LanguageTag;
                _speechContext = ResourceContext.GetForCurrentView();
                _speechContext.Languages = new string[] { langTag };

                // Create the speech recognizer instance.
                _speechRecognizer = new SpeechRecognizer(SpeechRecognizer.SystemSpeechLanguage);

                // Be aware of state changes in the speech recognizer instance. 
                _speechRecognizer.StateChanged += SpeechRecognizer_StateChanged;
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == RecognizerNotFoundHResult)
                {
                    Debug.WriteLine("SpeechManager: The speech language pack for selected language isn't installed.");
                }
                else
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets the kind of speech that the SpeechRecognizer listens for.
        /// </summary>
        /// <remarks>
        /// <para>Currently supported modes are command-list and dictation.</para>
        /// <para>When the recognition mode changes, the grammar is re-compiled.</para>
        /// </remarks>
        public SpeechRecognitionMode RecognitionMode { get; private set; }

        /// <summary>
        /// Gets the <see cref="Person"/> collection from the FamilyNotes app's data model.
        /// </summary>
        public ObservableCollection<Person> Family { get; private set; }


        /// <summary>
        /// Assigns the kind of speech that the <see cref="SpeechManager"/> listens
        /// for: commands or dictation.
        /// </summary>
        /// <param name="mode">The recognition mode.</param>
        /// <returns>Void</returns>
        public async Task<bool> SetRecognitionMode(SpeechRecognitionMode mode)
        {
            bool success = true;
            if (mode != RecognitionMode)
            {
                RecognitionMode = mode;

                if (mode == SpeechRecognitionMode.Paused)
                {
                    await EndRecognitionSession();
                }
                else
                {
                    if (!await StartContinuousRecognition())
                    {
                        success = false;
                        mode = SpeechRecognitionMode.Default;
                    }
                }
                OnModeChanged(new EventArgs());
            }
            return success;
        }

        public async Task<bool> StartContinuousRecognition()
        {
            // Compiling a new grammar is potentially a high-latency operation,
            // and it's easy for various threads to call this method concurrently,
            // so use a sempahore to serialize access to this method. The semaphore
            // allows only one thread at a time to execute this code path.
            await Mutex.WaitAsync();

            if (IsInRecognitionSession)
            {
                // End the previous speech recognition session.
                await EndRecognitionSession();
            }
#if VERBOSE_DEBUG
            Debug.WriteLine( 
                "SpeechManager: Starting recognition session: {0}", 
                RecognitionMode );
#endif

            try
            {
                // If no mic is available, notify the user and reset mode to default.
                if (!await IsMicrophoneAvailable())
                {
                    if (RecognitionMode == SpeechRecognitionMode.CommandPhrases ||
                        RecognitionMode == SpeechRecognitionMode.Dictation)
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Microphone is not available.");

                        messageDialog.Commands.Add(new UICommand("Go to settings...", async (command) =>
                        {
                            bool result = await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-microphone"));

                        }));
                        messageDialog.Commands.Add(new UICommand("Close", (command) => { }));
                        await messageDialog.ShowAsync();
                    }

                    RecognitionMode = SpeechRecognitionMode.Default;
                    Mutex.Release();
                    return false;
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
#if VERBOSE_DEBUG
                Debug.WriteLine( "SpeechManager: Continuous recognition session started" );
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SpeechManager: Failed to start continuous recognition session.");

                var messageDialog = new Windows.UI.Popups.MessageDialog(
                    $"{ex.Message}",
                    "Failed to start continuous recognition session");

                messageDialog.Commands.Add(new UICommand("Go to settings...", async (command) =>
                {
                    bool result = await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-speech"));

                }));
                messageDialog.Commands.Add(new UICommand("Close", (command) => { }));
                await messageDialog.ShowAsync();

                Mutex.Release();
                return false;
            }
            finally
            {
                Mutex.Release();
            }
            return true;
        }

        /// <summary>
        /// Reads the specified phrase, with the default text-to-speech voice.
        /// </summary>
        /// <param name="phrase">The text to say.</param>
        /// <param name="media">The <see cref="MediaElement"/> that plays the speech.</param>
        /// <remarks><para>This method is awaitable, because in the case of a speech prompt,
        /// the speech recognizer can hear the prompt and may process it, along with 
        /// the user's speech. Avoid this bug by awaiting the call to the <see cref="SpeakAsync"/> method 
        /// and then setting <see cref="RecognitionMode"/> to <see cref="SpeechRecognitionMode.Dictation"/>
        /// after it completes. This way, the speech prompt ends before recognition begins.</para>
        /// <para>Also, the <see cref="SpeakAsync"/> method stops the current recognition session,
        /// so the user and any spoken prompts don't trigger speech commands.</para>
        /// <para>The <see cref="SpeakAsync"/> method uses the <see cref="SemaphoreSlim"/> class to implement
        /// a signal from the <see cref="MediaElement.MediaEnded"/> event handler to this method.
        /// </para>
        /// </remarks>
        public async Task SpeakAsync(string phrase, MediaElement media)
        {
            if (!String.IsNullOrEmpty(phrase))
            {
                var previousMode = RecognitionMode;
                // Turn off speech recognition while speech synthesis is happening.
                await SetRecognitionMode(SpeechRecognitionMode.Paused);

                MediaPlayerElement = media;
                SpeechSynthesisStream synthesisStream = await SpeechSynth.SynthesizeTextToStreamAsync(phrase);

                // The Play call starts the sound stream playback and immediately returns,
                // so a semaphore is required to make the SpeakAsync method awaitable.
                media.AutoPlay = true;
                media.SetSource(synthesisStream, synthesisStream.ContentType);
                media.Play();
                
                // Wait until the MediaEnded event on MediaElement is raised,
                // before turning on speech recognition again. The semaphore
                // is signaled in the mediaElement_MediaEnded event handler.
                await Semaphore.WaitAsync();

                // Turn on speech recognition and listen for commands.
                //await SetRecognitionMode(SpeechRecognitionMode.CommandPhrases);
                await SetRecognitionMode(previousMode);
            }
        }

        /// <summary>Raised when the recognition session produces a result.</summary>
        /// <remarks>The handler for the <see cref="SpeechRecognizer.ContinuousRecognitionSession.ResultGenerated"/> event 
        /// raises this event.
        /// </remarks>
        public event EventHandler<PhraseRecognizedEventArgs> PhraseRecognized;
        public delegate void PhraseRecognizedEventHandler(object sender, PhraseRecognizedEventArgs e);
        protected virtual void OnPhraseRecognized(PhraseRecognizedEventArgs e)
        {
            PhraseRecognized?.Invoke(this, e);
        }

        /// <summary>
        /// Raised when the state of the <see cref="Windows.Media.SpeechRecognition.SpeechRecognizer"/> changes.
        /// </summary>
        /// <remarks>The handler for the <see cref="SpeechRecognizer.StateChanged"/> event 
        /// raises this event.</remarks>
        public event EventHandler<StateChangedEventArgs> StateChanged;
        public delegate void StateChangedEventHandler(object sender, StateChangedEventArgs e);
        protected virtual void OnStateChanged(StateChangedEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Raised when the state of the <see cref="Windows.Media.SpeechRecognition.SpeechRecognizer"/> changes.
        /// </summary>
        /// <remarks>The handler for the <see cref="SpeechRecognizer.StateChanged"/> event 
        /// raises this event.</remarks>
        public event EventHandler<EventArgs> ModeChanged;
        public delegate void ModeChangedEventHandler(object sender, EventArgs e);
        protected virtual void OnModeChanged(EventArgs e)
        {
            ModeChanged?.Invoke(this, e);
        }

        #region Implementation for speech recognition

        public bool IsInRecognitionSession { get; set; }

        /// <summary>
        /// Queries a <see cref="MediaCapture"/> instance for an audio device controller. 
        /// </summary>
        /// <returns>True, if a microphone is found, otherwise false.</returns>
        /// <remarks>TBD: is this the best/only way to test for a mic?</remarks>
        private async Task<bool> IsMicrophoneAvailable()
        {
            bool isMicrophoneAvailable = false;

            try
            {
                var captureDevice = new MediaCapture();
                await captureDevice.InitializeAsync();

                // Throws if no device is available.
                var audioDevice = captureDevice.AudioDeviceController;
                if (audioDevice != null)
                {
#if VERBOSE_DEBUG
                    Debug.WriteLine( "SpeechManager: AudioDeviceController found" );
#endif
                    isMicrophoneAvailable = true;
                }
                else
                {
                    Debug.WriteLine("SpeechManager: No AudioDeviceController found");
                }
            }
            catch (COMException ex)
            {
                Debug.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return isMicrophoneAvailable;
        }

        private async void Family_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Re-compile the grammar for family members and restart the recognition
            // session, but only if voice recognition is already active.
            if (IsInRecognitionSession)
            {
                await StartContinuousRecognition();
            }
        }

        private SpeechRecognizer SpeechRecognizer
        {
            get
            {
                if (_speechRecognizer == null)
                {
                    InitializeRecognizer();
                }

                return _speechRecognizer;
            }
        }

        private ResourceMap SpeechResourceMap
        {
            get
            {
                if (_speechResourceMap == null)
                {
                    _speechResourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("SpeechResources");
                }

                return _speechResourceMap;
            }
        }

        private List<string> AvailablePhrases { get; set; }

        private Dictionary<string, Person> PhraseToPersonDictionary
        {
            get
            {
                if (_phraseToPersonDictionary == null)
                {
                    _phraseToPersonDictionary = new Dictionary<string, Person>();
                }

                return _phraseToPersonDictionary;
            }
        }

        private void PopulatePhrases()
        {
            AvailablePhrases = new List<string>();

            if (Family != null && Family.Count > 0)
            {
                PhraseToPersonDictionary.Clear();

                var familyList = Family.ToList();
                familyList.ForEach(person =>
               {
                   var phrases = GetPhrasesForPerson(person);
                   AvailablePhrases = AvailablePhrases.Concat(phrases).ToList();

                   phrases.ForEach(phrase =>
                   {
                       if (!PhraseToPersonDictionary.Keys.Contains(phrase))
                       {
                           PhraseToPersonDictionary.Add(phrase, person);
                       }
                   });
               });
            }

            AvailablePhrases.Add(GetGrammarResourceString("GrammarHelp"));
            AvailablePhrases.Add(GetGrammarResourceString("GrammarWhatCanISay"));
            AvailablePhrases.Add(GetGrammarResourceString("GrammarReadNote"));
            AvailablePhrases.Add(GetGrammarResourceString("GrammarDeleteNote"));
            AvailablePhrases.Add(GetGrammarResourceString("GrammarEditNote"));
            AvailablePhrases.Add(GetGrammarResourceString("GrammarShowAllNotes"));
            AvailablePhrases.Add(GetGrammarResourceString("GrammarShowMyNotes"));
            AvailablePhrases.Add(GetGrammarResourceString("GrammarShowAllNotesToMe"));
            AvailablePhrases.Add(GetGrammarResourceString("GrammarShowAllNotesForMe"));
            AvailablePhrases.Add(GetGrammarResourceString("GrammarShowNotesToMe"));
            AvailablePhrases.Add(GetGrammarResourceString("GrammarShowNotesForMe"));
        }

        private List<string> GetPhrasesForPerson(Person person)
        {
            List<string> phrases = new List<string>();

            phrases.Add(GetGrammarResourceStringAndAppendName("GrammarAddNoteTo", person.FriendlyName));
            phrases.Add(GetGrammarResourceStringAndAppendName("GrammarAddNoteFor", person.FriendlyName));
            phrases.Add(GetGrammarResourceStringAndAppendName("GrammarCreateNoteTo", person.FriendlyName));
            phrases.Add(GetGrammarResourceStringAndAppendName("GrammarCreateNoteFor", person.FriendlyName));
            phrases.Add(GetGrammarResourceStringAndAppendName("GrammarNewNoteTo", person.FriendlyName));
            phrases.Add(GetGrammarResourceStringAndAppendName("GrammarNewNoteFor", person.FriendlyName));
            phrases.Add(GetGrammarResourceStringAndAppendName("GrammarShowNotesTo", person.FriendlyName));
            phrases.Add(GetGrammarResourceStringAndAppendName("GrammarShowNotesFor", person.FriendlyName));

            // Handle "Show <user>'s notes" command.
            var showUsersTemplate = GetGrammarResourceString("GrammarShowUsersNotes");
            string showUsersString = String.Format(showUsersTemplate, person.FriendlyName);
            phrases.Add(showUsersString);

            return phrases;
        }

        private string GetGrammarResourceString(string resource)
        {
            return SpeechResourceMap.GetValue(resource, _speechContext).ValueAsString;
        }

        private string GetGrammarResourceStringAndAppendName(string resource, string personName)
        {
            string resourceString = GetGrammarResourceString(resource);
            string resourceStringWithName = $"{resourceString} {personName}";
            return resourceStringWithName;
        }

        private async Task CompileGrammar()
        {
            if (RecognitionMode == SpeechRecognitionMode.Dictation)
            {
                await CompileDictationConstraint();
            }
            else
            {
                await CompilePhraseConstraints();
            }
        }

        private async Task CompilePhrases()
        {

#if VERBOSE_DEBUG
            Debug.WriteLine( "SpeechManager: Compiling command phrase constraints" );
#endif

            try
            {
                SpeechRecognizer.Constraints.Clear();

                AvailablePhrases.ForEach(p =>
                {
                    string phraseNoSpaces = p.Replace(" ", String.Empty);
                    SpeechRecognizer.Constraints.Add(
                        new SpeechRecognitionListConstraint(
                            new List<string>() { p },
                            phraseNoSpaces));
                });

                var result = await SpeechRecognizer.CompileConstraintsAsync();
                if (result.Status != SpeechRecognitionResultStatus.Success)
                {
                    Debug.WriteLine("SpeechManager: CompileConstraintsAsync failed for phrases");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async Task CompileDictationConstraint()
        {

#if VERBOSE_DEBUG
            Debug.WriteLine( "SpeechManager: Compiling dictation constraint" );
#endif

            SpeechRecognizer.Constraints.Clear();

            // Apply the dictation topic constraint to optimize for dictated freeform speech.
            var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
            SpeechRecognizer.Constraints.Add(dictationConstraint);
            var result = await SpeechRecognizer.CompileConstraintsAsync();
            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                Debug.WriteLine("SpeechRecognizer.CompileConstraintsAsync failed for dictation");
            }
        }

        private async Task CompilePhraseConstraints()
        {
            try
            {
                PopulatePhrases();
                await CompilePhrases();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private void ContinuousRecognitionSession_Completed(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionCompletedEventArgs args)
        {
            IsInRecognitionSession = false;

            StateChangedEventArgs e = new StateChangedEventArgs(args);
            OnStateChanged(e);
        }

        /// <summary>
        /// Handle events fired when a result is generated. This may include a garbage rule that fires when general room noise
        /// or side-talk is captured (this will have a confidence of Rejected typically, but may occasionally match a rule with
        /// low confidence).
        /// </summary>
        /// <param name="sender">The Recognition session that generated this result</param>
        /// <param name="args">Details about the recognized speech</param>
        /// <remarks>
        /// <para> This method raises the PhraseRecognized event. Keep in mind that the 
        /// ContinuousRecognitionSession.ResultGenerated event is raised on an arbitrary thread 
        /// from the thread pool. If a <see cref="SpeechManager"/> client has thread affinity, 
        /// like in a XAML-based UI, you need to marshal the call to the client's thread.
        /// </para>
        /// <para>In a UWP app, use the <see cref="CoreDispatcher"/> to execute the call 
        /// on the main UI thread.</para>
        /// </remarks>
        private void ContinuousRecognitionSession_ResultGenerated(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            if (args.Result.Status != SpeechRecognitionResultStatus.Success)
            {
#if VERBOSE_DEBUG
                Debug.WriteLine( "SpeechManager: ResultGenerated: {0}", args.Result.Status );
#endif
                return;
            }

            // Unpack event arg data.
            bool hasConstraint = args.Result.Constraint != null;
            var confidence = args.Result.Confidence;
            string phrase = args.Result.Text;

            // The garbage rule doesn't have a tag associated with it, and 
            // the other rules return a string matching the tag provided
            // when the grammar was compiled.
            string tag = hasConstraint ? args.Result.Constraint.Tag : "unknown";
            if (tag == "unknown")
            {
#if VERBOSE_DEBUG
                Debug.WriteLine( "SpeechManager: ResultGenerated: garbage rule hit" );
#endif
                return;
            }
            else
            {
#if VERBOSE_DEBUG

                string msg = String.Format( "SpeechManager: ResultGenerated: {0}", phrase );
                Debug.WriteLine( msg );
#endif
            }

            if (hasConstraint && args.Result.Constraint.Type == SpeechRecognitionConstraintType.List)
            {
                // The List constraint type represents speech from 
                // a compiled grammar of commands.
                CommandVerb verb = GetPhraseIntent(phrase);

                // You may decide to use per-phrase confidence levels in order to 
                // tune the behavior of your grammar based on testing.
                if (confidence == SpeechRecognitionConfidence.Medium ||
                    confidence == SpeechRecognitionConfidence.High)
                {
                    Person person = null;
                    if (PhraseToPersonDictionary.ContainsKey(phrase))
                    {
                        person = PhraseToPersonDictionary[phrase];
                    }

                    // Raise the PhraseRecognized event. Clients with thread affinity, 
                    // like in a XAML-based UI, need to marshal the call to the 
                    // client's thread.
                    PhraseRecognizedEventArgs eventArgs = new PhraseRecognizedEventArgs(
                        person,
                        phrase,
                        verb,
                        args);
                    OnPhraseRecognized(eventArgs);
                }
            }
            else if (hasConstraint && args.Result.Constraint.Type == SpeechRecognitionConstraintType.Topic)
            {
                // The Topic constraint type represents speech from dictation.

                // Raise the PhraseRecognized event. Clients with thread affinity, 
                // like in a XAML-based UI, need to marshal the call to the 
                // client's thread.
                PhraseRecognizedEventArgs eventArgs = new PhraseRecognizedEventArgs(
                    null,
                    phrase,
                    CommandVerb.Dictation,
                    args);
                OnPhraseRecognized(eventArgs);
            }
        }

        private CommandVerb GetPhraseIntent(string phrase)
        {
            CommandVerb verb = CommandVerb.None;

            if (phrase.StartsWith("Add") || phrase.StartsWith("Create") || phrase.StartsWith("New"))
            {
                verb = CommandVerb.Create;
            }
            else if (phrase.StartsWith("Read"))
            {
                verb = CommandVerb.Read;
            }
            else if (phrase.StartsWith("Edit"))
            {
                verb = CommandVerb.Edit;
            }
            else if (phrase.StartsWith("Delete"))
            {
                verb = CommandVerb.Delete;
            }
            else if (phrase.StartsWith("Help") || phrase.StartsWith("What can I say"))
            {
                verb = CommandVerb.Help;
            }
            else if (phrase.StartsWith("Show"))
            {
                verb = CommandVerb.Show;
            }
            else
            {
                Debug.WriteLine("Phrase intent not recognized: {0}", phrase);
            }

            return verb;
        }

        /// <summary>
        /// Provides feedback to client code based on whether the recognizer is receiving speech input.
        /// </summary>
        /// <param name="sender">The recognizer that is currently running.</param>
        /// <param name="args">The current state of the recognizer.</param>
        private void SpeechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            StateChangedEventArgs e = new StateChangedEventArgs(args);
            OnStateChanged(e);
        }

        /// <summary>
        /// Stop the current speech recognition session.
        /// </summary>
        /// <returns>Void</returns>
        /// <remarks>Ensure that calls to this method are protected by
        /// a mutex or other thread-access object. Currently, the
        /// <see cref="StartContinuousRecognition"/> method is the only
        /// caller of this method, and the call is protected by the
        /// <see cref="SpeechManager.Mutex"/> property.</remarks>
        private async Task EndRecognitionSession()
        {
            // Detach event handlers.
            SpeechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
            SpeechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;

            // Stop the recognition session, if it's in progress.
            if (IsInRecognitionSession)
            {
#if VERBOSE_DEBUG
                Debug.WriteLine( "SpeechManager: Ending continuous recognition session" );
#endif
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

#if VERBOSE_DEBUG
                    Debug.WriteLine( "SpeechManager: Continuous recognition session ended" );
#endif
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }

        private SemaphoreSlim Mutex
        {
            get
            {
                if (_mutex == null)
                {
                    // Initialize the semaphore to allow execution
                    // by one thread thread at a time.
                    _mutex = new SemaphoreSlim(1);
                }

                return _mutex;
            }
        }

        #endregion

        #region Implementation for speech synthesis


        private SpeechSynthesizer SpeechSynth
        {
            get
            {
                if (_speechSynthesizer == null)
                {
                    _speechSynthesizer = new SpeechSynthesizer();
                }

                return _speechSynthesizer;
            }
        }

        private MediaElement MediaPlayerElement
        {
            get
            {
                return _mediaElement;
            }
            set
            {
                if (_mediaElement != value)
                {
                    if (_mediaElement != null)
                    {
                        _mediaElement.MediaEnded -= mediaElement_MediaEnded;
                    }

                    _mediaElement = value;
                    _mediaElement.MediaEnded += mediaElement_MediaEnded;
                }
            }
        }

        private void mediaElement_MediaEnded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Signal the SpeakAsync method.
            Semaphore.Release();
        }

        private SemaphoreSlim Semaphore
        {
            get
            {
                if (_semaphore == null)
                {
                    _semaphore = new SemaphoreSlim(0,1);
                }

                return _semaphore;
            }
        }

        private WaitHandle WaitHandle { get; set; }

        #endregion

        #region Private fields for speech recognition

        private SpeechRecognizer _speechRecognizer;
        private ResourceContext _speechContext;
        private ResourceMap _speechResourceMap;
        private Dictionary<string, Person> _phraseToPersonDictionary;
        private static uint RecognizerNotFoundHResult = 0x8004503a;

        // Synchronizes access to the StartContinuousRecognition method.
        private SemaphoreSlim _mutex;

        #endregion

        #region Private fields for speech synthesis

        // Creates speech for prompts and for reading notes to the user.
        private SpeechSynthesizer _speechSynthesizer;

        // Plays synthesized speech.
        private MediaElement _mediaElement;

        // Used to make the SpeakAsync method awaitable. 
        private SemaphoreSlim _semaphore;

        #endregion
    }
}
