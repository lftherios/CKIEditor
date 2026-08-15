using System;
using System.Collections.Generic;
using UnityEngine;

namespace CKIEditor.Model
{
    public static class NoteStringHelper
    {
        private const int OCTAVE_INTERVAL = 12;
        
        public static Dictionary<int, string> Notes = new Dictionary<int, string>()
                                                      {
                                                          {0,"C "},
                                                          {1,"C#"},
                                                          {2,"D "},
                                                          {3,"D#"},
                                                          {4,"E "},
                                                          {5,"F "},
                                                          {6,"F#"},
                                                          {7,"G "},
                                                          {8,"G#"},
                                                          {9,"A "},
                                                          {10,"A#"},
                                                          {11,"B "},
                                                      };


        public static int GetNoteId(string noteString)
        {
            return GetNoteId(GetNoteIndex(noteString), GetOctaveIndex(noteString));
        }
        
        public static int GetNoteId(int noteIndex, int octaveIndex)
        {
            return noteIndex + octaveIndex * OCTAVE_INTERVAL;
        }
        
        public static int GetNoteIndex(string noteString)
        {
            var noteOnly = noteString.Substring(0, 2);
            
            foreach (var keyValuePair in Notes)
            {
                if (keyValuePair.Value == noteOnly)
                    return keyValuePair.Key;
            }

            return 0;
        }
        
        public static int GetNoteIndex(int noteId)
        {
            return noteId % OCTAVE_INTERVAL;
        }

        public static int GetOctaveIndex(string noteString)
        {
            var octaveNumber = noteString.Substring(2);
            //Cirklon displays octave 10 as "X"
            if (octaveNumber == "X")
                return 10;

            int.TryParse(octaveNumber, out int octaveIndex);
            return octaveIndex;
        }
        
        public static int GetOctaveIndex(int noteId)
        {
            return (int)Math.Floor((double)noteId / OCTAVE_INTERVAL);
        }
        
        public static string GetNoteName(int noteId)
        {
            return GetNoteName(GetNoteIndex(noteId), GetOctaveIndex(noteId));
        }
        
        public static string GetNoteName(int noteIndex, int octaveIndex)
        {
            //Cirklon displays octave 10 as "X" to keep note names 3 characters wide
            var octaveString = octaveIndex == 10 ? "X" : octaveIndex.ToString();
            return $"{Notes[noteIndex]}{octaveString}";
        }
    }
}