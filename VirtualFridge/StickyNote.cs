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
using System.Runtime.Serialization;
using System.ComponentModel;
using Windows.UI.Input.Inking;

namespace VirtualFridge
{
    /// <summary>
    /// Provides the business object for the note data.
    /// </summary>
    [DataContract]
    public class StickyNote : BindableBase
    {
        
        /// <summary>
        /// Initializes a new instance of a <see cref="StickyNote"/> with 
        /// the specified tag string.
        /// </summary>
        /// <param name="nametag">Who the note is for.</param>
        public StickyNote(string nametag)
        {
            WhoNoteIsFor = nametag;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="StickyNote"/> with 
        /// the specified <see cref="Person"/> object.
        /// </summary>
        /// <param name="person">The person the note is for.</param>
        public StickyNote(Person person)
        {
            this.NoteIsFor = person;
        }


        /// <summary>
        /// Gets or sets the <see cref="Person"/> instance that's associated 
        /// with the current note.
        /// </summary>
        [DataMember]
        public Person NoteIsFor { get; set; }

        /// <summary>
        /// Gets or sets the text in the current note.
        /// </summary>
        [DataMember]
        public string NoteText
        {
            get
            {
                return _noteText;
            }

            set
            {
                SetProperty(ref _noteText, value);
            }
        }
        private string _noteText;

        /// <summary>
        /// Gets or sets the placeholder for the <see cref="Note"/> control
        /// that's associated with the current <see cref="StickyNote"/> object.
        /// </summary>
        public string NotePlaceholderText
        {
            get
            {
                return _notePlaceholderText;
            }

            set
            {
                SetProperty(ref _notePlaceholderText, value);
            }
        }
        private string _notePlaceholderText;

        /// <summary>
        /// Gets or sets the ink stroke data for the <see cref="Note"/> control
        /// that's associated with the current <see cref="StickyNote"/> object.
        /// </summary>
        [IgnoreDataMember] // can't serialize InkStrokeContainers
        public InkStrokeContainer Ink
        {
            get
            {
                return _ink;
            }

            set
            {
                SetProperty(ref _ink, value);
            }
        }
        [IgnoreDataMember]
        private InkStrokeContainer _ink = new InkStrokeContainer();

        /// <summary>
        /// Gets or sets the name of the <see cref="Person"/> that
        /// the current <see cref="StickyNote"/> is for.
        /// </summary>
        [DataMember]
        public string WhoNoteIsFor { get; set; }

        /// <summary>
        /// Gets or sets the input mode that the user created the
        /// note's content with.
        /// </summary>
        [DataMember]
        public NoteInputMode NoteMode
        {
            get
            {
                return _noteMode;
            }
            set
            {
                SetProperty(ref _noteMode, value);
            }
        }
        private NoteInputMode _noteMode;

    }
}