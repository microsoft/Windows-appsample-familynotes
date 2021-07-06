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
using System;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Cortana using directives
using Windows.Storage;
using Windows.Media.SpeechRecognition;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Json;
using Windows.UI.Input.Inking;
using Windows.Storage.Streams;
using System.Collections.Generic;

namespace FamilyNotes
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// The data model for the Family Notes
        /// </summary>
        public Model Model
        {
            get;
            private set;
        }

        /// <summary>
        ///  Settings such as the Azure Face service key, and the background setting
        /// </summary>
        public Settings AppSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;
                
                if (args.PreviousExecutionState != ApplicationExecutionState.Running &&
                    args.PreviousExecutionState != ApplicationExecutionState.Suspended)
                {
                    await LoadModelAndSettingsAsync();
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), args.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();

            // Cortana voice commands.
            // Install Voice Command Definition (VCD) file.
            try
            {
                // Install the main VCD on launch to ensure 
                // most recent version is installed.
                StorageFile vcdStorageFile =
                    await Package.Current.InstalledLocation.GetFileAsync(
                        @"VoiceCommandObjects\VoiceCommands.xml");

                await Windows.ApplicationModel.
                    VoiceCommands.VoiceCommandDefinitionManager.
                    InstallCommandDefinitionsFromStorageFileAsync(
                    vcdStorageFile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Installing Voice Commands Failed: " + ex.ToString());
            }
        }

        /// <summary>
        /// Entry point for an application activated by some means other than normal launching. 
        /// This includes voice commands, URI, share target from another app, and so on. 
        /// 
        /// NOTE:
        /// A previous version of the VCD file might remain in place 
        /// if you modify it and update the app through the store. 
        /// Activations might include commands from older versions of your VCD. 
        /// Try to handle these commands gracefully.
        /// </summary>
        /// <param name="args">Details about the activation method.</param>
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);

            Type navigationToPageType;

            VoiceCommandObjects.VoiceCommand navCommand = null;

            // Voice command activation.
            if (args.Kind == Windows.ApplicationModel.Activation.ActivationKind.VoiceCommand)
            {
                // Event args can represent many different activation types. 
                // Cast it so we can get the parameters we care about out.
                var commandArgs = args as VoiceCommandActivatedEventArgs;

                Windows.Media.SpeechRecognition.SpeechRecognitionResult 
                    speechRecognitionResult = commandArgs.Result;

                // Get the name of the voice command and the text spoken. 
                // See VoiceCommands.xml for supported voice commands.
                string voiceCommand = speechRecognitionResult.RulePath[0];
                string textSpoken = speechRecognitionResult.Text;

                // commandMode indicates whether the command was entered using speech or text.
                // Apps should respect text mode by providing silent (text) feedback.
                string commandMode = this.SemanticInterpretation("commandMode", speechRecognitionResult);

                switch (voiceCommand)
                {
                    case "addNewNote":

                        // Create a navigation command object to pass to the page. 
                        navCommand = new VoiceCommandObjects.VoiceCommand();
                        navCommand.CommandMode = commandMode;
                        navCommand.VoiceCommandName = voiceCommand;
                        navCommand.TextSpoken = textSpoken;

                        // Set the page to navigate to for this voice command.
                        // App is a single page app at this time.
                        navigationToPageType = typeof(MainPage);
                        break;
                    case "addNewNoteForPerson":

                        // Create a navigation command object to pass to the page. 
                        // Access the value of the {person} phrase in the voice command
                        string noteOwner = this.SemanticInterpretation("person", speechRecognitionResult);
                        navCommand = new VoiceCommandObjects.VoiceCommand();
                        navCommand.CommandMode = commandMode;
                        navCommand.VoiceCommandName = voiceCommand;
                        navCommand.TextSpoken = textSpoken;
                        navCommand.NoteOwner = noteOwner;

                        // Set the page to navigate to for this voice command.
                        // App is a single page app at this time.
                        navigationToPageType = typeof(MainPage);
                        break;
                    default:
                        // If we can't determine what page to launch, go to the default entry point.
                        navigationToPageType = typeof(MainPage);
                        break;
                }
            }
            // Protocol activation occurs when a card is clicked within Cortana (using a background task).
            else if (args.Kind == ActivationKind.Protocol)
            {
                // No background service at this time.
                navigationToPageType = typeof(MainPage);
            }
            else
            {
                // If we were launched via any other mechanism, fall back to the main page view.
                // Otherwise, we'll hang at a splash screen.
                navigationToPageType = typeof(MainPage);
            }

            // Repeat the same basic initialization as OnLaunched() above, 
            // taking into account whether or not the app is already active.
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (args.PreviousExecutionState != ApplicationExecutionState.Running &&
                    args.PreviousExecutionState != ApplicationExecutionState.Suspended)
                {
                    await LoadModelAndSettingsAsync();
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            // Since we're expecting to always show the home page, navigate even if 
            // a content frame is in place (unlike OnLaunched).
            rootFrame.Navigate(navigationToPageType, navCommand);

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Returns the semantic interpretation of a speech result. 
        /// Returns null if there is no interpretation for that key.
        /// </summary>
        /// <param name="interpretationKey">The interpretation key.</param>
        /// <param name="speechRecognitionResult">The speech recognition result to get the semantic interpretation from.</param>
        /// <returns></returns>
        private string SemanticInterpretation(string interpretationKey, SpeechRecognitionResult speechRecognitionResult)
        {
            return speechRecognitionResult.SemanticInterpretation.Properties[interpretationKey].FirstOrDefault();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            AppSettings.SaveSettings();
            await SaveModelAsync();

            deferral.Complete();
        }

        /// <summary>
        /// Save the model (family and their notes) to the app's isolated storage
        /// </summary>
        /// <remarks>
        /// The data format for notes data:
        /// 
        ///     Serialized model data (the people and the sticky notes)
        ///     For each sticky note
        ///     {
        ///         int32 number of inkstrokes for the note
        ///     }
        ///     All ink stroke data (for all notes) combined into one container
        /// </remarks>
        /// <returns></returns>
        public async Task SaveModelAsync()
        {
            // Persist the model
            StorageFile notesDataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(NOTES_MODEL_FILE, CreationCollisionOption.ReplaceExisting);
            using (Stream notesDataStream = await notesDataFile.OpenStreamForWriteAsync())
            {
                // Serialize the model which contains the people and the stickyNote collection
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Model));
                serializer.WriteObject(notesDataStream, Model);
            }

            /* For each sticky note, save the number of inkstrokes it contains.
               The function on the InkStrokeContainer that persists its contents is not designed
               to save persist containers to the one stream. We also don't want to manage one
               backing file per note. So combine the ink strokes into one container and persist that.
               We'll seperate out the ink strokes to the right ink control by keeping track of how
               many ink strokes belongs to each note */

            InkStrokeContainer CombinedStrokes = new InkStrokeContainer();
            StorageFile inkFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(NOTES_INK_FILE, CreationCollisionOption.ReplaceExisting);
            using (var randomAccessStream = await inkFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (IOutputStream inkStream = randomAccessStream.GetOutputStreamAt(0)) // DataWriter requires an IOutputStream
                {
                    bool combinedStrokesHasContent = false; // whether we had any ink to save across all of the notes
                    DataWriter writer = new DataWriter(inkStream);
                    foreach (StickyNote Note in Model.StickyNotes)
                    {
                        // Save # strokes for this note
                        if (Note.Ink != null && Note.Ink.GetStrokes().Count > 0)
                        {
                            IReadOnlyList<InkStroke> InkStrokesInNote = Note.Ink.GetStrokes();
                            writer.WriteInt32(InkStrokesInNote.Count);
                            // capture the ink strokes into the combined container which will be saved at the end of the notes data file
                            foreach (InkStroke s in InkStrokesInNote)
                            {
                                CombinedStrokes.AddStroke(s.Clone());
                            }
                            combinedStrokesHasContent = true;
                        }
                        else
                        {
                            writer.WriteInt32(0); // not all notes have ink
                        }
                    }
                    await writer.StoreAsync(); // flush the data in the writer to the inkStream

                    // Persist the ink data
                    if (combinedStrokesHasContent ) 
                    {
                        await CombinedStrokes.SaveAsync(inkStream);
                    }
                }
            }
        }

        /// <summary>
        /// Load the family and their notes from local storage
        /// </summary>
        /// <returns>Null if there was no model to load, otherwise, the deserialized model</returns>
        private async Task<Model> LoadModelAsync()
        {
            Model model = null;

            InkStrokeContainer combinedStrokes = new InkStrokeContainer(); // To avoid managing individual files for every InkCanvas, we will combine all ink stroke information into one container
            List<int> InkStrokesPerCanvas = new List<int>();

            try
            {
                StorageFile modelDataFile = await ApplicationData.Current.LocalFolder.GetFileAsync(NOTES_MODEL_FILE);
                using (IRandomAccessStream randomAccessStream = await modelDataFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    // Load the model which contains the people and the note collection
                    try
                    {
                        DataContractJsonSerializer modelSerializer = new DataContractJsonSerializer(typeof(Model));
                        model = (Model)modelSerializer.ReadObject(randomAccessStream.AsStream());
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {
                        System.Diagnostics.Debug.Assert(false, "Failed to load serialized model");
                        return null;
                    }
                }

                // For each sticky note, load the number of inkstrokes it contains
                StorageFile inkDataFile = await ApplicationData.Current.LocalFolder.GetFileAsync(NOTES_INK_FILE);
                using (IInputStream inkStream = await inkDataFile.OpenSequentialReadAsync())
                {
                    bool combinedStrokesExist = false;
                    DataReader reader = new DataReader(inkStream);
                    foreach (StickyNote n in model.StickyNotes)
                    {
                        await reader.LoadAsync(sizeof(int)); // You need to buffer the data before you can read from a DataReader.
                        int numberOfInkStrokes = reader.ReadInt32();
                        InkStrokesPerCanvas.Add(numberOfInkStrokes);
                        combinedStrokesExist |= numberOfInkStrokes > 0;
                    }

                    // Load the ink data
                    if (combinedStrokesExist)
                    {
                        await combinedStrokes.LoadAsync(inkStream);
                    }
                } // using inkStream
            } // try
            catch (FileNotFoundException)
            {
                // No data to load. We'll start with a fresh model
                return null;
            }

            // Factor out the inkstrokes from the big container into each note
            int allStrokesIndex = 0, noteIndex = 0;
            IReadOnlyList<InkStroke> allStrokes = combinedStrokes.GetStrokes();
            foreach (StickyNote n in model.StickyNotes)
            {
                // InkStrokeContainers can't be serialized using the default xml/json serialization.
                // So create a new one and fill it up from the data we restored
                n.Ink = new InkStrokeContainer();
                // pull out the ink strokes that belong to this note
                for (int i = 0; i < InkStrokesPerCanvas[noteIndex]; i++)
                {
                    n.Ink.AddStroke(allStrokes[allStrokesIndex++].Clone());
                }
                ++noteIndex;
            }

            return model;
        }

        /// <summary>
        /// Load the settings and the model
        /// This function is factored out so that we can call it from OnLaunched()
        /// as well as OnActivated()--which is how the app is started from Cortana.
        /// </summary>
        /// <returns>An awaitable Task</returns>
        private async Task LoadModelAndSettingsAsync()
        {
            AppSettings = new Settings();
            AppSettings.LoadSettings();

            Model = await LoadModelAsync();
            if (Model == null)
            {
                Model = new Model();
                Model.CreateDefaultFamily();
            }
        }

        public const string EVERYONE = "Everyone";
        public const string NOTES_INK_FILE = "Ink.isf";
        public const string NOTES_MODEL_FILE = "NotesData.json";
    }
}
