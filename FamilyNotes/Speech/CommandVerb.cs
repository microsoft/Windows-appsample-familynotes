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

namespace FamilyNotes
{
    /// <summary>
    /// Defines the intent of a speech command.
    /// </summary>
    public enum CommandVerb
    {
        /// <summary>
        /// No intent is defined or can be inferred.
        /// </summary>
        None = 0,

        /// <summary>
        /// Speech is dictated and is not a command.
        /// </summary>
        Dictation,

        /// <summary>
        /// Create a note.
        /// </summary>
        Create,

        /// <summary>
        /// Read a note back to the user, by using speech synthesis.
        /// </summary>
        Read,

        /// <summary>
        /// Edit an existing note by using dictation.
        /// </summary>
        Edit,

        /// <summary>
        /// Delete the note that has focus.
        /// </summary>
        Delete,

        /// <summary>
        /// Filter notes per person.
        /// </summary>
        Show,

        /// <summary>
        /// Get help for available speech commands.
        /// </summary>
        Help
    }
}
