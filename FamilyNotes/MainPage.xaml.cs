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

using FamilyNotes.AppDialogs;
using FamilyNotes.UserDetection;
using System;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace FamilyNotes
{
    /// <summary>
    /// The main FamilyNotes page and controller class that the user interacts with.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // Initialize the data model for the application, consisting of a list of people (the family),
            // a list of notes (which are assigned to a person), and the app's settings.
            App app = (App)Application.Current;
            FamilyModel = app.Model;
            AppSettings = app.AppSettings;

            // Initially assume we are unfiltered - all notes are shown.
            CurrentlyFiltered = false;

            // Update greeting that appears at the top of the screen e.g. "Good morning"
            UpdateGreeting(string.Empty);

            // Create default notes if first launch
            if (AppSettings.LaunchedPreviously != true)
            {
                CreateDefaultNotes();
                ShowTeachingTips();
            }

            // Set up custom title bar.
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            coreTitleBar.IsVisibleChanged += CoreTitleBar_IsVisibleChanged;
            // Set XAML element as a draggable region.
            Window.Current.SetTitleBar(AppTitleBar);

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = (Resources["ApplicationForegroundThemeBrush"] as SolidColorBrush).Color;
            AppTitle.Text = Windows.ApplicationModel.Package.Current.DisplayName;
        }


        /// <summary>
        /// This method is called when the app first appears.
        /// </summary>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            _presence = new UserPresence(_dispatcher, _unfilteredName);


            // For launching via Cortana.
            _pageParameters = e.Parameter as VoiceCommandObjects.VoiceCommand;
            if (_pageParameters != null)
            {
                switch (_pageParameters.VoiceCommandName)
                {
                    case "addNewNote":
                        _activeNote = CreateNote(App.EVERYONE);
                        break;
                    case "addNewNoteForPerson":
                        _activeNote = CreateNote(_pageParameters.NoteOwner);
                        break;
                    default:
                        break;
                }
            }

            // Perform initialization for face recognition.
            if (AppSettings.FaceApiKey != "")
            {
                await FacialSimilarity.TrainDetectionAsync();
            }

            // Perform initialization for speech recognition.
            _speechManager = new SpeechManager(FamilyModel);
            _speechManager.PhraseRecognized += SpeechManager_PhraseRecognized;
            _speechManager.StateChanged += SpeechManager_StateChanged;
            _speechManager.ModeChanged += SpeechManager_ModeChanged;
        }

        private void ShowTeachingTips()
        {
            AddPersonTip.IsOpen = true;

            AddPersonTip.ActionButtonClick += (sender, e) =>
            {
                AddPersonTip.IsOpen = false;
                NewNoteTip.IsOpen = true;
            };

            NewNoteTip.ActionButtonClick += (sender, e) =>
            {
                NewNoteTip.IsOpen = false;
                VoiceCommandTip.IsOpen = true;
            };

            VoiceCommandTip.ActionButtonClick += (sender, e) =>
            {
                VoiceCommandTip.IsOpen = false;
                FamilyFilterTip.IsOpen = true;
            };
        }

        private void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
        {
            AppTitleBar.Visibility = sender.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Public properties

        public Note FocusedNote
        {
            get
            {
                return taskPanel.FocusedNote;
            }
        }

        #endregion

        #region Public methods

        public StickyNote CreateNote(Person person)
        {
            // Create the new note.
            StickyNote newNote = new StickyNote(person);
            FamilyModel.StickyNotes.Add(newNote);
            return newNote;
        }

        public StickyNote CreateNote(string nameTag)
        {
            // Create the new note.
            StickyNote newNote = new StickyNote(nameTag);
            newNote.NoteIsFor = FamilyModel.PersonFromName(nameTag);
            FamilyModel.StickyNotes.Add(newNote);
            return newNote;
        }

        public void Public_ShowNotesForPerson(string nameTag)
        {
            Person selectedPerson = FamilyModel.PersonFromName(nameTag);
            taskPanel.FilterNotes(selectedPerson);

            // Determine whether or not we are currently filtering.
            if (selectedPerson.FriendlyName == _unfilteredName)
            {
                CurrentlyFiltered = false;
            }
            else
            {
                CurrentlyFiltered = true;
            }
        }
        #endregion

        #region Implementation

        private Model FamilyModel { get; set; }

        private Settings AppSettings { get; set; }

        private bool CurrentlyFiltered
        {
            get
            {
                return _currentlyFiltered;
            }
            set
            {
                _currentlyFiltered = value;
            }
        }

        /// <summary>
        /// Display a friendly greeting at the top of the screen.
        /// </summary>
        /// <param name="name">Name of user for the salutation</param>
        private async void UpdateGreeting(string name)
        {
            var now = DateTime.Now;
            var greeting =
                now.Hour < 12 ? "Good morning" :
                now.Hour < 18 ? "Good afternoon" :
                /* otherwise */ "Good night";
            var person = (string.IsNullOrEmpty(name) || name == App.EVERYONE) ? "!" : $", {name}!";
            TextGreeting.Text = $"{greeting}{person}";

            if (!string.IsNullOrEmpty(name) && (name != App.EVERYONE))
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {

                    var SpeakGreeting = $"{greeting} {name}";

                    var notes = taskPanel.CountNotes(FamilyModel.PersonFromName(name));

                    if (notes > 0)
                    {
                        if (notes == 1)
                            SpeakGreeting += ",there is a note for you.";
                        else
                            SpeakGreeting += $",there are {notes} notes for you.";
                    }

                    await this._speechManager.SpeakAsync(
                        SpeakGreeting,
                         this._media);
                });
            }
        }

        private async Task<bool> FocusedNoteAssigned()
        {
            bool focusedNote = false;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                focusedNote = FocusedNote != null ? true : false;
            });

            return focusedNote;
        }

        private void UserFilterFromDetection(object sender, UserPresence.UserIdentifiedEventArgs e)
        {
            Public_ShowNotesForPerson(e.User);
            UpdateGreeting(e.User);
        }

        #endregion

        #region User Interactions

        /// <summary>
        /// The people picker appears after tapping New Note, and this is the event
        /// that is given the person selected by the user. It then creates a note for that person. 
        /// </summary>
        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Person person = e.ClickedItem as Person;
            _activeNote = CreateNote(person);
            _activeNote.NotePlaceholderText = "Type your note here.";
            NewNoteButton.Flyout.Hide();
        }

        private async void AddNewPersonDialog(Person currentPerson)
        {
            var dialog = new AddPersonContentDialog();

            dialog.ProvideExistingPerson(currentPerson);
            await dialog.ShowAsync();
            Person newPerson = dialog.AddedPerson;

            // If there is a valid person to add, add them.
            if (newPerson != null)
            {
                // Get or create a directory for the user (we do this regardless of whether or not there is a profile picture).
                StorageFolder userFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(("Users\\" + newPerson.FriendlyName), CreationCollisionOption.OpenIfExists);

                // See if we have a profile photo.
                if (dialog.TemporaryFile != null)
                {
                    // Save off the profile photo and delete the temporary file.
                    await dialog.TemporaryFile.CopyAsync(userFolder, "ProfilePhoto.jpg", NameCollisionOption.ReplaceExisting);
                    await dialog.TemporaryFile.DeleteAsync();

                    // Update the profile picture for the person.
                    newPerson.IsProfileImage = true;
                    newPerson.ImageFileName = userFolder.Path + "\\ProfilePhoto.jpg";

                    if (AppSettings.FaceApiKey != "")
                    {
                        await FacialSimilarity.AddTrainingImageAsync(newPerson.FriendlyName, new Uri($"ms-appdata:///local/Users/{newPerson.FriendlyName}/ProfilePhoto.jpg"));
                    }
                }
                // Add the user if it is new now that changes have been made.
                if (currentPerson == null)
                {
                    await FamilyModel.AddPersonAsync(newPerson);
                }
                // Otherwise we had a user, so update the current one.
                else
                {
                    //await FamilyModel.UpdatePersonImageAsync(newPerson);
                    Person personToUpdate = FamilyModel.PersonFromName(currentPerson.FriendlyName);
                    if (personToUpdate != null)
                    {
                        personToUpdate.IsProfileImage = true;
                        personToUpdate.ImageFileName = userFolder.Path + "\\ProfilePhoto.jpg";
                    }
                }
            }
        }

        private void AddPerson()
        {
            // Add null if there is no existing person.
            AddNewPersonDialog(null);
        }

        private async Task DisableFaceDetection()
        {
            await _presence.DisableFaceDetection();
            ImageWarningBar.IsOpen = false;
            (FaceDetectionButton.Icon as SymbolIcon).Symbol = Symbol.WebCam;
            SetCameraButton.IsEnabled = true;
        }

        private async Task EnableFaceDetection()
        {
            // Inform the user if we do not have a Azure Face service key and then exit without doing anything.
            if (AppSettings.FaceApiKey == "")
            {
                ContentDialog noFaceKeyDialog = new ContentDialog
                {
                    Title = "No Azure Face key",
                    Content = "You need an Azure Face service key, which you define in settings, to use facial recognition.",
                    CloseButtonText = "Ok"
                };
                await noFaceKeyDialog.ShowAsync();
                FaceDetectionButton.IsChecked = false;
                return;
            }

            if (SetCameraButton.IsEnabled)
            {
                // Make sure the user accepts privacy implications.
                var dialog = new WarningDialog();
                await dialog.ShowAsync();
                if (dialog.WarningAccept == false)
                {
                    FaceDetectionButton.IsChecked = false;
                    return;
                }
            }

            bool result = await _presence.EnableFaceDetection();
            if (result)
            {
                _presence.FilterOnFace += UserFilterFromDetection;
            }
            else
            {
                _presence.FilterOnFace -= UserFilterFromDetection;
            }

            ImageWarningBar.IsOpen = result;
            // Update the face detection icon depending on whether the effect exists or not.
            (FaceDetectionButton.Icon as SymbolIcon).Symbol = (result) ? Symbol.View : Symbol.WebCam;
            SetCameraButton.IsEnabled = result != true;
        }

        /// <summary>
        /// Sets the camera object to use, in case you don't want to use the default.
        /// </summary>
        private async Task SetCameraDevice()
        {
            // Create a DevicePicker.
            var devicePicker = new DevicePicker();

            // Set that we are looking for video capture devices.
            devicePicker.Filter.SupportedDeviceClasses.Add(DeviceClass.VideoCapture);

            // Calculate the position to show the picker (right below the buttons).
            GeneralTransform transform = SetCameraButton.TransformToVisual(null);
            Point point = transform.TransformPoint(new Point());
            Rect rect = new Rect(point, new Point(point.X + SetCameraButton.ActualWidth, point.Y + SetCameraButton.ActualHeight));

            // Use the device picker to pick the device.
            DeviceInformation deviceInfo = await devicePicker.PickSingleDeviceAsync(rect);
            if (null != deviceInfo)
            {
                _presence.CameraDeviceId = deviceInfo.Id;
                _presence.IsDefaultCapture = false;

                DeviceText.Text = deviceInfo.Name;
            }
        }

        private void TidyNotes()
        {
            taskPanel.TidyNotes();
        }

        private async void NavigationViewControl_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                try
                {
                    // Open settings.
                    _ = await SettingsDialog.ShowAsync();
                }
                catch { };
            }
            else
            {
                // A person was selected from the list.
                // Link the XAML object tapped with the PERSON in the FAMILY collection.
                _selectedPerson = args.InvokedItem as Person;
                taskPanel.FilterNotes(_selectedPerson); // Show the notes that apply to this person.

                // Determine whether or not we are currently filtering.
                CurrentlyFiltered = _selectedPerson.FriendlyName != _unfilteredName;

                // Update greeting that appears at the top of the screen e.g. "Good morning".
                UpdateGreeting(_selectedPerson.FriendlyName);
            }
        }

        /// <summary>
        /// When the user right-clicks on a person, a flyout menu appears. The menu options allow
        /// for a note to be quickly created for that person, or the person can be deleted.
        /// </summary>
        private void FamilyList_RightTap(object sender, RightTappedRoutedEventArgs e)
        {
            FamilyListMethod(e.OriginalSource as FrameworkElement);
        }

        private void FamilyListMethod(FrameworkElement selectedItem)
        {
            // To find which person the user clicked, we look at the data context of the clicked item.
            Person selectedPerson = selectedItem.DataContext as Person;

            if (selectedPerson is null)
            {
                return;
            }

            var menu = new MenuFlyout();

            var option1 = new MenuFlyoutItem() { Text = "Create note for " + selectedPerson.FriendlyName };
            option1.Tag = selectedPerson;
            option1.Click += MenuFlyoutOptionCreateNote;
            menu.Items.Add(option1);

            if (selectedPerson.FriendlyName != App.EVERYONE)
            {
                var option2 = new MenuFlyoutItem() { Text = "Add/replace photo for " + selectedPerson.FriendlyName };
                option2.Tag = selectedPerson;
                option2.Click += MenuFlyoutOptionAddPhotoToPerson;
                menu.Items.Add(option2);
                
                var option3 = new MenuFlyoutItem() { Text = "Delete " + selectedPerson.FriendlyName };
                option3.Tag = selectedPerson;
                option3.Click += MenuFlyoutOptionDeletePerson;
                menu.Items.Add(option3);
            }

            menu.ShowAt(selectedItem, new Point(60, 0));
        }

        private void MenuFlyoutOptionCreateNote(object sender, RoutedEventArgs e)
        {
            Person selectedPerson = (Person)(sender as MenuFlyoutItem).Tag;
            CreateNote(selectedPerson);
        }

        private async void MenuFlyoutOptionDeletePerson(object sender, RoutedEventArgs e)
        {
            var dialog = new DeleteConfirmationDialog();
            await dialog.ShowAsync();
            if (dialog.DeleteData == false)
            {
                return;
            }

            Person selectedPerson = (Person)(sender as MenuFlyoutItem).Tag;
            await this.FamilyModel.DeletePersonAsync(selectedPerson.FriendlyName);
        }

        private void MenuFlyoutOptionAddPhotoToPerson(object sender, RoutedEventArgs e)
        {
            Person selectedPerson = (Person)(sender as MenuFlyoutItem).Tag;
            AddNewPersonDialog(selectedPerson);
        }

        /// <summary>
        /// Delete all app data, including users (execept 'Everyone')
        /// </summary>
        private async Task DeleteAll()
        {
            SettingsDialog.Hide();
            var dialog = new DeleteConfirmationDialog();
            await dialog.ShowAsync();
            if (dialog.DeleteData == false)
            {
                return;
            }

            // Delete FamilyNotesData and ink.isf.
            IsolatedStorageFile appLocalData = IsolatedStorageFile.GetUserStoreForApplication();
            if (appLocalData.FileExists(App.NOTES_MODEL_FILE))
            {
                appLocalData.DeleteFile(App.NOTES_MODEL_FILE);
            }

            if (appLocalData.FileExists(App.NOTES_INK_FILE))
            {
                appLocalData.DeleteFile(App.NOTES_INK_FILE);
            }

            foreach (string name in FamilyModel.Family.Select(person => person.FriendlyName).ToList())
            {
                bool success = await FamilyModel.DeletePersonAsync(name);
                Debug.Assert(success == true, "Unable to delete person");
            }
        }

        private void ToggleFullScreenMode()
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
                // The SizeChanged event will be raised when the exit from full-screen mode is complete.
            }
            else
            {
                view.TryEnterFullScreenMode();
                // The SizeChanged event will be raised when the entry to full-screen mode is complete.
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
                (FullScreenButton.Icon as SymbolIcon).Symbol = Symbol.BackToWindow;
            }
            else
            {
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
                (FullScreenButton.Icon as SymbolIcon).Symbol = Symbol.FullScreen;
            }

            // If the window has gotten smaller, make sure all the notes
            // are still visible on the canvas.
            if (e.NewSize.Height < e.PreviousSize.Height ||
                e.NewSize.Width < e.PreviousSize.Width)
            {
                taskPanel.ConstrainNotesToCanvas();
            }
        }

        private void SettingsDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            // When settings is closed, put the nav view back on the previously selected person.
            NavigationViewControl.SelectedItem = _selectedPerson;
        }
        #endregion

        #region Speech Handling

        private async void SpeechManager_ModeChanged(object sender, EventArgs e)
        {
            if (_speechManager.RecognitionMode == SpeechRecognitionMode.CommandPhrases)
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    MicrophoneWarningBar.IsOpen = true;
                    MicrophoneWarningBar.Message = "Your microphone is listening for voice commands.";
                });
            }
            else if (_speechManager.RecognitionMode == SpeechRecognitionMode.Dictation)
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    MicrophoneWarningBar.IsOpen = true;
                    MicrophoneWarningBar.Message = "Your microphone is listening for dictation.";
                });
            }
            else
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    MicrophoneWarningBar.IsOpen = false;
                });
            }
        }

        private async void SpeechManager_StateChanged(object sender, StateChangedEventArgs e)
        {
            if (e.IsSessionState && !e.SessionCompletedSuccessfully && e.SessionTimedOut)
            {
                // Dictation timed out, so reset recognition mode to what it was
                // before dictation started (voice commands or disabled).
                Debug.WriteLine("Timeout exceeded, resetting RecognitionMode");
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if ((bool)VoiceCommandButton.IsChecked)
                    {
                        EnableVoiceCommands();
                    }
                    else
                    {
                        DisableVoiceCommands();
                    }
                    FocusedNote.InputMode = NoteInputMode.Text;
                });
            }
        }

        /// <summary>
        /// Handles the <see cref="SpeechManager.PhraseRecognized"/> event.
        /// </summary>
        /// <param name="sender">the <see cref="SpeechManager"/> that raised the event.</param>
        /// <param name="e">The event data.</param>
        private async void SpeechManager_PhraseRecognized(object sender, PhraseRecognizedEventArgs e)
        {
            Person person = e.PhraseTargetPerson;
            string phrase = e.PhraseText;
            CommandVerb verb = e.Verb;

            string msg = String.Format("Heard phrase: {0}", phrase);
            Debug.WriteLine(msg);

            switch (verb)
            {
                case CommandVerb.Dictation:
                    {
                        // The phrase came from dictation. Now that dictation has ended, transition
                        // speech recognition to its previous mode (listen for command phrases or disable).
                        await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            FocusedNote.NoteBusinessObject.NoteText = phrase;
                            if ((bool)VoiceCommandButton.IsChecked)
                            {
                                EnableVoiceCommands();
                            }
                            else
                            {
                                DisableVoiceCommands();
                            }
                            FocusedNote.InputMode = NoteInputMode.Text;
                        });

                        break;
                    }
                case CommandVerb.Create:
                    {
                        // A command for creating a note was recognized.
                        await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            _activeNote = CreateNote(person);
                            _activeNote.NoteText = "Dictate your note here!";
                            if (await _speechManager.SetRecognitionMode(SpeechRecognitionMode.Dictation))
                            {
                                await _speechManager.SpeakAsync("Dictate your note", _media);
                                FocusedNote.InputMode = NoteInputMode.Dictation;
                            }
                            else
                            {
                                FocusedNote.InputMode = NoteInputMode.Text;
                            }
                        });

                        break;
                    }
                case CommandVerb.Read:
                    {
                        // The command for reading a note was recognized.
                        bool focusedNoteAssigned = await FocusedNoteAssigned();
                        if (focusedNoteAssigned)
                        {
                            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                            {
                                await _speechManager.SpeakAsync(
                                    FocusedNote.NoteBusinessObject.NoteText,
                                    _media);
                            });
                        }

                        break;
                    }
                case CommandVerb.Edit:
                    {
                        // The command for editing a note was recognized.
                        bool focusedNoteAssigned = await FocusedNoteAssigned();
                        if (focusedNoteAssigned)
                        {
                            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                            {
                                if (await _speechManager.SetRecognitionMode(SpeechRecognitionMode.Dictation))
                                {
                                    await _speechManager.SpeakAsync("Dictate your note", _media);
                                    FocusedNote.InputMode = NoteInputMode.Dictation;
                                }
                                else
                                {
                                    FocusedNote.InputMode = NoteInputMode.Text;
                                }
                            });
                        }

                        break;
                    }
                case CommandVerb.Delete:
                    {
                        // The command for deleting a note was recognized.
                        bool focusedNoteAssigned = await FocusedNoteAssigned();
                        if (focusedNoteAssigned)
                        {
                            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                            {
                                FocusedNote.OnDeleteNoteEvent();
                                await _speechManager.SpeakAsync("Note deleted", _media);
                            });
                        }

                        break;
                    }
                case CommandVerb.Show:
                    {
                        Debug.WriteLine("SpeechManager.PhraseRecognized handler: Show TBD");
                        break;
                    }
                case CommandVerb.Help:
                    {
                        // A command for spoken help was recognized.
                        await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            await _speechManager.SpeakAsync(_helpString, _media);
                        });

                        break;
                    }
                default:
                    {
                        Debug.WriteLine("SpeechManager.PhraseRecognized handler: Couldn't determine phrase intent");
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles transitions into and out of dictation mode 
        /// for the focused <see cref="Note"/> control.
        /// </summary>
        /// <param name="sender">The <see cref="Note"/> control.</param>
        /// <param name="e">The event data.</param>
        private async void TaskPanel_NoteInputModeChanged(object sender, InputModeChangedEventArgs e)
        {
            // Transition out of dictation mode. 
            if (_speechManager.RecognitionMode == SpeechRecognitionMode.Dictation && 
                e.NewInputMode != NoteInputMode.Dictation)
            {
                if ((bool)VoiceCommandButton.IsChecked)
                {
                    EnableVoiceCommands();
                }
                else
                {
                    DisableVoiceCommands();
                }
            }
            // Transition to dictation mode. 
            else if (_speechManager.RecognitionMode != SpeechRecognitionMode.Dictation &&
                e.NewInputMode == NoteInputMode.Dictation)
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (await _speechManager.SetRecognitionMode(SpeechRecognitionMode.Dictation))
                    {
                        await _speechManager.SpeakAsync("Dictate your note", _media);
                    }
                    else
                    {
                        e.SourceNote.InputMode = NoteInputMode.Default;
                    }
                });
            }
        }

        private async void EnableVoiceCommands()
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await _speechManager.SetRecognitionMode(SpeechRecognitionMode.CommandPhrases);
                if (!_speechManager.IsInRecognitionSession)
                {
                    VoiceCommandButton.IsChecked = false;
                }
            });
        }

        private async void DisableVoiceCommands()
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await _speechManager.SetRecognitionMode(SpeechRecognitionMode.Paused);
            });
        }
        #endregion


        public void CreateDefaultNotes()
        {
            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "6/6\n\nYou can also use voice commands, such as 'Add new note' Try 'What can I say?' for help.";

            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "5/6\n\nFilter notes by tapping the relevant user's button on the left, or by turning on the camera if using face recognition.";

            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "4/6\n\nIf you want to use the face recognition feature: obtain an Azure Face Service API Key and endpoint, add them to the app settings, and then enable the feature.";

            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "3/6\n\nNow you can add new notes by pressing the 'New note' button and selecting 'Everyone' or another user.";

            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "2/6\n\nTo get started, you should add some users by tapping on the 'New person' button at the bottom of the screen.";

            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "1/6\n\nWelcome to Family Notes!\nOnce you have read these sample notes, you can delete them by tapping on the 3 dots on the bottom right and choosing Delete.";
        }

        #region Private fields

        private StickyNote _activeNote;
        private static string _unfilteredName = App.EVERYONE;
        private UserPresence _presence;
        private const string _detectionString = "Detected faces : ";
        private CoreDispatcher _dispatcher;
        private SpeechManager _speechManager;
        private VoiceCommandObjects.VoiceCommand _pageParameters;
        private bool _currentlyFiltered;
        private Person _selectedPerson;
        private const string _helpString = "You can say: add note for person, or, create note to person, or, new note to person. For the active note, you can say, edit note, read note, and delete note.";

        #endregion

        private async void NavigationViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentDialog HelpDialog = new ContentDialog
            {
                Title = "Help",
                Content = "To use voice commands and dictation, you have to give Family Notes access to your camera and microphone, and turn on Online speech recognition in Settings.I agree \n\nTo create a note, you can say: \"add note for person\", \"create note to person\", \"new note to person\".\n\nFor the active note, you can say: \"edit note\", \"read note\", \"delete note\".",
                CloseButtonText = "Close",
                PrimaryButtonText = "Show teaching tips"
            };
            HelpDialog.PrimaryButtonClick += (s, args) =>
            {
                ShowTeachingTips();
            };
            await HelpDialog.ShowAsync();

        }
    }
}
