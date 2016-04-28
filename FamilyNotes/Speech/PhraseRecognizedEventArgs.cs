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
using Windows.Media.SpeechRecognition;

namespace FamilyNotes
{
    /// <summary>
    /// Provides data for the <see cref="PhraseRecognized"/> event
    /// </summary>
    public class PhraseRecognizedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PhraseRecognizedEventArgs"/> class.
        /// </summary>
        /// <param name="person">The Person who the note is addressed to.</param>
        /// <param name="phrase">The phrase provided by the speech recognizer.</param>
        /// <param name="speechRecognitionArgs">Event data from the speech recognizer.</param>
        public PhraseRecognizedEventArgs(
            Person person,
            string phrase,
            CommandVerb verb,
            SpeechContinuousRecognitionResultGeneratedEventArgs speechRecognitionArgs)
        {
            PhraseTargetPerson = person;
            PhraseText = phrase;
            Verb = verb;
            IsDictation = speechRecognitionArgs.Result.Constraint == null ? false : Verb == CommandVerb.Dictation;
        }

        /// <summary>
        /// Gets the person who the note is addressed to.
        /// </summary>
        public Person PhraseTargetPerson { get; private set; }

        /// <summary>
        /// The phrase provided by the speech recognizer.
        /// </summary>
        public string PhraseText { get; private set; }

        /// <summary>
        /// Gets the intent of the phrase.
        /// </summary>
        public CommandVerb Verb { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the speech recognizer is listening
        /// for dictated speech instead of a command list.
        /// </summary>
        public bool IsDictation { get; private set;  }
    }
}
