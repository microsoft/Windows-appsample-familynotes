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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace FamilyNotes
{
    /// <summary>
    /// Provides a control for viewing a <see cref="StickyNote"/> object.
    /// </summary>
    public partial class Note : UserControl, INotifyPropertyChanged
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Note"/> control.
        /// </summary>
        public Note()
        {
            InitializeComponent();

            // Set up manipulation events.
            ManipulationMode = Windows.UI.Xaml.Input.ManipulationModes.All;
            ManipulationDelta += Note_ManipulationDelta;
            ManipulationStarted += Note_ManipulationStarted;
            Tapped += Note_Tapped;
            DoubleTapped += Note_DoubleTapped;

            // Create the render transform.
            _compositeTransform = new CompositeTransform();
            RenderTransform = _compositeTransform;

            // Promote the Note control to the top of the z-order.
            SetValue(Canvas.ZIndexProperty, _zOrderTop);

            // Put the new note in a random position
            SetPosition(
                new Random().Next(_notePositionMin, _notePositionMax), 
                new Random().Next(_notePositionMin, _notePositionMax));

            // By default, make the note ready for text.
            // Should add code to test if it's created with a pen
            // and should then default to ink.
            containerForInk.InkPresenter.InputDeviceTypes =
                Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                Windows.UI.Core.CoreInputDeviceTypes.Pen;
        }


        /// <summary>
        /// Gets or sets the <see cref="StickyNote"/> instance that
        /// the current <see cref="Note"/> control is bound to.
        /// </summary>
        public StickyNote NoteBusinessObject
        {
            get { return (StickyNote)GetValue(NoteBusinessObjectProperty); }
            set { SetValue(NoteBusinessObjectProperty, value); }
        }

        /// <summary>
        /// Implements a dependency property for the <see cref="NoteBusinessObject"/> property.
        /// </summary>
        /// <remarks>When the <see cref="NoteBusinessObject"/> property value changes,
        /// the <see cref="Note.DisplayName"/> property is assigned.</remarks>
        public static readonly DependencyProperty NoteBusinessObjectProperty =
            DependencyProperty.Register(
                "NoteBusinessObject",
                typeof(StickyNote),
                typeof(Note),
                new PropertyMetadata(false, NoteBusinessObjectPropertyChanged));

        private static void NoteBusinessObjectPropertyChanged(DependencyObject dependencyObject,
                                           DependencyPropertyChangedEventArgs ea)
        {
            Note instance = dependencyObject as Note;
            StickyNote note = ea.NewValue as StickyNote;
            instance.DisplayName = $"For: {note.NoteIsFor.FriendlyName}";
        }

        /// <summary>
        /// Gets the kind of input that's active on the current <see cref="Note"/> control.
        /// </summary>
        public NoteInputMode InputMode
        {
            get
            {
                return this._inputMode;
            }

            private set
            {
                if (SetProperty(ref _inputMode, value))
                {
                    OnPropertyChanged("CanEnterInkMode");
                    OnPropertyChanged("CanEnterSpeechMode");
                    OnPropertyChanged("CanEnterTypingMode");

                    NoteBusinessObject.NoteMode = value;
                }
            }
        }

        private NoteInputMode _inputMode = NoteInputMode.Default;

        /// <summary>
        /// Gets a value indicating whether the current <see cref="Note"/> 
        /// control can change to accepting ink input.
        /// </summary>
        public bool CanEnterInkMode
        {
            get
            {
                return InputMode != NoteInputMode.Ink;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current <see cref="Note"/> 
        /// control can change to accepting speech input.
        /// </summary>
        public bool CanEnterSpeechMode
        {
            get
            {
                return InputMode != NoteInputMode.Dictation;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current <see cref="Note"/> 
        /// control can change to accepting keyboard input.
        /// </summary>
        public bool CanEnterTypingMode
        {
            get
            {
                return InputMode != NoteInputMode.Text;
            }
        }

        /// <summary>
        /// Gets the person's name to display on the <see cref="Note"/> control.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return _displayName;
            }

            private set
            {
                SetProperty(ref _displayName, value);
            }
        }

        private string _displayName;

        /// <summary>
        /// Removes the associated <see cref="StickyNote"/> object from the data model. 
        /// </summary>
        /// <remarks>The current <see cref="Note"/> control is deleted by the
        /// containing <see cref="NotesCanvas"/>.</remarks>
        public void OnDeleteNoteEvent()
        {
            App theApp = Application.Current as App;
            theApp.Model.DeleteNote(NoteBusinessObject);
        }

        /// <summary>
        /// Set the location of the note on the screen. Used when the 'tidy' feature is called.
        /// </summary>
        public void SetPosition(int x, int y)
        {
            _compositeTransform.TranslateX = x;
            _compositeTransform.TranslateY = y;
        }


        private void Note_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // When the note is double tapped, expand or collapse it to take up less space.
            if (TheNote.Height == _noteHeightCollapsed)
            {
                TheNote.Height = _noteHeight;
                NoteBottomSection.Visibility = Visibility.Visible;
                DropShadow.Height = _dropShadowHeight;
            }
            else
            {
                TheNote.Height = _noteHeightCollapsed;
                DropShadow.Height = _dropShadowHeightCollapsed;
                NoteBottomSection.Visibility = Visibility.Collapsed;
            }
        }

        private void BringCurrentNoteToFront()
        {
            NotesCanvas parent = CanvasControl;
            parent.BringNoteToFront(this);
        }

        /// <summary>
        /// Gets the parent canvas of the current Note in the visual tree.
        /// </summary>
        /// <remarks>Having a reference to the parent canvas is useful when 
        /// the Note control is created from a DataTemplate. In this case, the 
        /// Note control is encapsulated by a ContentPresenter control in
        /// the visual tree. The parent of the ContentPresenter is the canvas.
        /// </remarks>
        private NotesCanvas CanvasControl
        {
            get
            {
                var parent = VisualTreeHelper.GetParent(this); // ContentPresenter
                var canvas = VisualTreeHelper.GetParent(parent);
                return (NotesCanvas)canvas;
            }
        }

        private void Note_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NotesCanvas parent = CanvasControl;
            parent.BringNoteToFront(this);
        }

        private void Note_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            BringCurrentNoteToFront();
        }

        private void Note_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Move the note around the screen
            _compositeTransform.TranslateX += e.Delta.Translation.X;
            _compositeTransform.TranslateY += e.Delta.Translation.Y;

            if (_compositeTransform.TranslateX < 0) _compositeTransform.TranslateX = 0;
            if (_compositeTransform.TranslateY < 0) _compositeTransform.TranslateY = 0;

            var noteWidth = ActualWidth;
            var noteHeight = ActualHeight;

            if (_compositeTransform.TranslateX > (CanvasControl.ActualWidth - noteWidth))
            {
                _compositeTransform.TranslateX = CanvasControl.ActualWidth - noteWidth;
            }

            if (_compositeTransform.TranslateY > (CanvasControl.ActualHeight - noteHeight))
            {
                _compositeTransform.TranslateY = CanvasControl.ActualHeight - noteHeight;
            }
        }

        private void NoteTypeText_Click(object sender, RoutedEventArgs e)
        {
            InputMode = NoteInputMode.Text;
        }

        private void NoteTypeInk_Click(object sender, RoutedEventArgs e)
        {
            InputMode = NoteInputMode.Ink;
        }

        private void NoteTypeVoice_Click(object sender, RoutedEventArgs e)
        {
            containerForText.PlaceholderText = "Dictate your note!";
            InputMode = NoteInputMode.Dictation;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            OnDeleteNoteEvent();
        }

        private void ContainerForText_GotFocus(object sender, RoutedEventArgs e)
        {
            // The text container was tapped. 
            // Ensure that the note is brought to foreground.
            BringCurrentNoteToFront();
        }

        private void ContainerForText_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            NoteBusinessObject.NoteText = textbox.Text;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            BringCurrentNoteToFront();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(
            ref T storage, 
            T value,
            [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }


        // Tracks the location of the note control.
        private CompositeTransform _compositeTransform;

        private readonly int _noteHeight = 300;
        private readonly int _noteHeightCollapsed = 58;
        private readonly int _dropShadowHeight = 300;
        private readonly int _dropShadowHeightCollapsed = 58;
        private readonly int _zOrderTop = 100;
        private readonly int _notePositionMin = 50;
        private readonly int _notePositionMax = 300;
    }
}
