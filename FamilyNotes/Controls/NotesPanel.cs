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
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FamilyNotes
{
    /// <summary>
    /// Provides a layout panel for a <see cref="NotesCanvas"/> and <see cref="Note"/> controls.
    /// </summary>
    public sealed class NotesPanel : ItemsControl
    {
        public NotesPanel()
        {
            DefaultStyleKey = typeof(NotesPanel);
            Loaded += NotesPanel_Loaded;
        }

        private void NotesPanel_Loaded(object sender, RoutedEventArgs e)
        {
            CanvasControl.NoteInputModeChanged += CanvasControl_NoteInputModeChanged;
        }

        /// <summary>
        /// Gets the <see cref="Note"/> control that has focus currently.
        /// </summary>
        public Note FocusedNote
        {
            get
            {
                return CanvasControl.FocusedNote;
            }
        }

        /// <summary>
        /// Gets a collection of <see cref="Note"/> controls that
        /// are contained in the <see cref="NotesCanvas"/> control.
        /// </summary>
        public List<Note> NoteControls
        {
            get
            {
                return CanvasControl.NoteControls;
            }
        }

        /// <summary>
        /// Arranges <see cref="Note"/> controls neatly.
        /// </summary>
        public void TidyNotes()
        {
            CanvasControl.TidyNotes();
        }

        /// <summary>
        /// Highlights the <see cref="Note"/> controls for the specified <see cref="Person"/>.
        /// </summary>
        /// <param name="person">The person to highlight notes for.</param>
        public void FilterNotes(Person person)
        {
            CanvasControl.FilterNotes(person);
        }

        public int CountNotes(Person person)
        {
           return CanvasControl.CountNotes(person);
        }

        /// <summary>
        /// Raised when the value of a <see cref="Note"/> control's 
        /// <see cref="InputMode"/> property changes. 
        /// </summary>
        public event EventHandler<InputModeChangedEventArgs> NoteInputModeChanged;
        private void OnNoteInputModeChanged(InputModeChangedEventArgs e)
        {
            NoteInputModeChanged?.Invoke(e.SourceNote, e);
        }

        private void CanvasControl_NoteInputModeChanged(object sender, InputModeChangedEventArgs e)
        {
            OnNoteInputModeChanged(e);
        }


        private NotesCanvas CanvasControl
        {
            get
            {
                return (NotesCanvas)ItemsPanelRoot;
            }
        }

    }
}
