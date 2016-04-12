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

namespace VirtualFridge
{
    /// <summary>
    /// Provides data for the <see cref="Note.InputModeChanged"/> event.
    /// </summary>
    public class InputModeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputModeChangedEventArgs"/> class. 
        /// </summary>
        /// <param name="sourceNote">The <see cref="Note"/> control that raised the event.</param>
        /// <param name="inputMode">The new input mode.</param>
        public InputModeChangedEventArgs(Note sourceNote, NoteInputMode inputMode)
        {
            this.SourceNote = sourceNote;
            this.NewInputMode = inputMode;
        }

        /// <summary>
        /// Gets the newly selected input mode.
        /// </summary>
        public NoteInputMode NewInputMode { get; private set; }

        /// <summary>
        /// Gets the <see cref="Note"/> control that raised the event.
        /// </summary>
        public Note SourceNote { get; private set; }
    }

    /// <summary>
    /// Defines the event handler for the <see cref="Note.InputModeChanged"/> event.
    /// </summary>
    /// <param name="sender">The <see cref="Note"/> control that raised the event.</param>
    /// <param name="e">The event data.</param>
    public delegate void InputModeChangedEventHandler(Note sender, InputModeChangedEventArgs e);
}
