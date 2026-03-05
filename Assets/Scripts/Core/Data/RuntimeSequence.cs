using System.Collections.Generic;
using UnityEngine;

namespace SUNSET16.Core
{
    public class RuntimeSequence
    {
        [Tooltip("Unique ID used to identify and select this sequence (e.g. 'albert_day1_pill').")]
        public string sequenceId;

        [Tooltip("All dialogue lines in this tree, played in order unless a choice branches the index.")]
        public List<RuntimeLine> lines;
    }
}