using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRewindableObject
{
    public void RecordState(int BufferIndex);
    public void ApplyState(int BufferIndex);
    void ClearAt(int bufferIndex);

    void ClearAll();
    bool IncludeWhenInactive { get; }
}
