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
using System.ComponentModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace FamilyNotes
{
    /// <summary>
    /// Provides a canvas that contains <see cref="Note"/> controls.
    /// </summary>
    public class NotesCanvas : Canvas
    {
        /// <summary>
        /// Gets the <see cref="Note"/> control that has focus currently.
        /// </summary>
        public Note FocusedNote { get; private set; }

        /// <summary>
        /// Gets a collection of <see cref="Note"/> controls that
        /// are contained in the current <see cref="NotesCanvas"/> control.
        /// </summary>
        public List<Note> NoteControls
        {
            get
            {
                if (_notesList == null)
                {
                    _notesList = GetNotesFromVisualTree();
                }

                return _notesList;
            }
        }

        /// <summary>
        /// Assigns focus to the specified <see cref="Note"/> control.
        /// </summary>
        /// <param name="note">The note to give focus to.</param>
        public void BringNoteToFront(Note note)
        {
            FocusedNote = note;

            NoteControls.ForEach(n =>
            {
                // The visual parent of the Note control is a ContentPresenter control.
                var parent = VisualTreeHelper.GetParent(n);
                if (n == note)
                {
                    parent.SetValue(Canvas.ZIndexProperty, _zOrderTop);
                    parent.SetValue(Canvas.OpacityProperty, _opacityTop);
                }
                else
                {
                    parent.SetValue(Canvas.ZIndexProperty, _zOrderMid);
                    parent.SetValue(Canvas.OpacityProperty, _opacityNearTop);
                }
            });
        }

        /// <summary>
        /// Arranges <see cref="Note"/> controls neatly.
        /// </summary>
        public void TidyNotes()
        {
            // Each child control is a ContentPresenter that's wrapping a Note control.
            var children = Children;

            // Start stacking up the notes from here.
            int x = _tidyStartCoordX;
            int y = _tidyStartCoordY;

            foreach (var noteContentPresenter in children)
            {
                Note n = GetVisualChild<Note>(noteContentPresenter);
                CompositeTransform ct = n.RenderTransform as CompositeTransform;
                ct.TranslateX = x;
                ct.TranslateY = y;
                x += _tidyOffsetX;
                y += _tidyOffsetY;
                BringNoteToFront(n);
            }
        }

        /// <summary>
        /// Highlights the <see cref="Note"/> controls for the specified <see cref="Person"/>.
        /// </summary>
        /// <param name="person">The person to highlight notes for.</param>
        public void FilterNotes(Person person)
        {
            // Each child control is a ContentPresenter that's wrapping a Note control.
            var children = Children;

            foreach (var noteContentPresenter in children)
            {
                Note n = GetVisualChild<Note>(noteContentPresenter);
                StickyNote sn = n.DataContext as StickyNote;
                CompositeTransform ct = n.RenderTransform as CompositeTransform;

                if (sn.NoteIsFor.FriendlyName == person.FriendlyName ||
                    person.FriendlyName == App.EVERYONE ||
                    sn.NoteIsFor.FriendlyName == App.EVERYONE)
                {
                    // Make note totally not transparent.
                    BringNoteToFront(n);
                    n.Opacity = _opacityTop;
                    ct.ScaleX = _noteScaleTopX;
                    ct.ScaleY = _noteScaleTopY;
                }
                else
                {
                    // Make note a little transparent, ideally push it back into the z-dimension.
                    n.Opacity = _opacityMid;
                    ct.ScaleX = _noteScaleMidX;
                    ct.ScaleY = _noteScaleMidY;
                }
            }
        }

        /// <summary>
        /// Raised when the value of a <see cref="Note"/> control's 
        /// <see cref="InputMode"/> property changes.
        /// </summary>
        //public event InputModeChangedEventHandler NoteInputModeChanged;
        public event EventHandler<InputModeChangedEventArgs> NoteInputModeChanged;
        protected void OnNoteInputModeChanged(InputModeChangedEventArgs e)
        {
            NoteInputModeChanged?.Invoke(e.SourceNote, e);
        }


        /// <summary>
        /// Provides the behavior for the "Arrange" pass of layout. 
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this object should use to arrange itself and its children.</param>
        /// <returns>The actual size that is used after the element is arranged in layout.</returns>
        /// <remarks><para>In FamilyNotes, overriding this method is a hack to enable querying the visual tree 
        /// for <see cref="Note"/> controls that have been added or deleted. This is a workaround to deal with 
        /// the fact that the Visual.OnVisualChildrenChanged method isn't implemented for UWP apps.</para>
        /// <para>By the time that the <see cref="ArrangeOverride"/> method is called, controls generated 
        /// from the DataTemplate are attached to the visual tree, so they can be queried by using the
        /// <see cref="VisualTreeHelper"/> class.
        /// </para></remarks>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Size size = base.ArrangeOverride(finalSize);

            // Unhook event handlers from all of the Note controls in the old list.
            // For greater efficiency, this method could manage events only on 
            // the control that was added or removed.
            if (_notesList != null)
            {
                _notesList.ForEach(n =>
               {
                   n.PropertyChanged -= Note_PropertyChanged;
               });
            }

            // Add event handlers to all of the Note controls in the new list.
            var newNotesList = GetNotesFromVisualTree();
            newNotesList.ForEach(n =>
           {
               n.PropertyChanged += Note_PropertyChanged;
           });

            _notesList = newNotesList;

            // Give focus to the last Note in the list. 
            // Also, this method could sort by timestamp.
            FocusedNote = (_notesList.Count > 0) ? _notesList.Last() : null;

            return size;
        }

        private void Note_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "InputMode")
            {
                Note note = (Note)sender;
                InputModeChangedEventArgs args = new InputModeChangedEventArgs(note, note.InputMode);
                OnNoteInputModeChanged(args);
            }
        }

        private void Note_InputModeChanged(Note sender, InputModeChangedEventArgs e)
        {
            OnNoteInputModeChanged(e);
        }

        // Adapted from http://stackoverflow.com/questions/4586106/wpf-how-to-access-control-from-datatemplate.
        private T GetVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                DependencyObject v = VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }

            return child;
        }

        private List<Note> GetNotesFromVisualTree()
        {
            var notes = Children.Select(cp => GetVisualChild<Note>(cp));
            return notes.ToList();
        }

        private List<Note> _notesList;
        private readonly int _tidyOffsetX = 75;
        private readonly int _tidyOffsetY = 85;
        private readonly int _tidyStartCoordX = 50;
        private readonly int _tidyStartCoordY = 50;
        private readonly int _zOrderTop = 90;
        private readonly int _zOrderMid = 50;
        private readonly double _opacityTop = 1.0;
        private readonly double _opacityNearTop = 0.9;
        private readonly double _opacityMid = 0.5;
        private readonly double _noteScaleTopX = 1.0;
        private readonly double _noteScaleTopY = 1.0;
        private readonly double _noteScaleMidX = 0.85;
        private readonly double _noteScaleMidY = 0.85;

    }
}
