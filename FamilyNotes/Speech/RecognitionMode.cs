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
    /// Defines the recognition modes for the <see cref="SpeechManager"/>.
    /// </summary>
    public enum SpeechRecognitionMode
    {
        /// <summary>
        /// The default recognition mode, which is a policy defined elsewhere, 
        /// currently in the SpeechManager class.
        /// </summary>
        Default = 0,

        /// <summary>
        /// The speech recognizer listens for a List of commands.
        /// </summary>
        CommandPhrases,

        /// <summary>
        /// The speech recognizer listens for dictation.
        /// </summary>
        Dictation,

        /// <summary>
        /// The speech recognizer isn't listening for speech.
        /// </summary>
        Paused
    }
}
