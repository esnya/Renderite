using System;
using System.Collections.Generic;
using System.Text;


namespace Renderite.Shared
{
    public class KeyboardState : IMemoryPackable
    {
        public string? typeDelta;
        public HashSet<Key> heldKeys = new HashSet<Key>();
        public bool compositionActive;
        public string? compositionText;
        public int compositionSelectionStart;
        public int compositionSelectionLength;
        public List<string> compositionCandidates = new List<string>();
        public int compositionCandidateIndex = -1;

        public void Pack(ref MemoryPacker packer)
        {
            packer.Write(typeDelta);
            packer.WriteValueList(heldKeys);
            packer.Write(compositionActive);
            packer.Write(compositionText);
            packer.Write(compositionSelectionStart);
            packer.Write(compositionSelectionLength);
            packer.Write(compositionCandidateIndex);

            var candidateCount = compositionCandidates.Count;
            packer.Write(candidateCount);
            if (candidateCount <= 0)
            {
                return;
            }

            for (var index = 0; index < candidateCount; index++)
            {
                packer.Write(compositionCandidates[index]);
            }
        }

        public void Unpack(ref MemoryUnpacker packer)
        {
            packer.Read(ref typeDelta);
            packer.ReadValueList(ref heldKeys);
            packer.Read(ref compositionActive);
            packer.Read(ref compositionText);
            packer.Read(ref compositionSelectionStart);
            packer.Read(ref compositionSelectionLength);
            packer.Read(ref compositionCandidateIndex);

            var candidateCount = 0;
            packer.Read(ref candidateCount);
            compositionCandidates ??= new List<string>(candidateCount);
            compositionCandidates.Clear();
            for (var index = 0; index < candidateCount; index++)
            {
                string? candidate = null;
                packer.Read(ref candidate);
                compositionCandidates.Add(candidate ?? string.Empty);
            }
        }
    }
}
