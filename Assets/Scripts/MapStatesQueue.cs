using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MapStatesQueue : Singleton<MapStatesQueue>
{
    [SerializeField] private List<MapChunk> mapChunks;
    [SerializeField] int maxActiveChunkCount;
    [SerializeField] int spacing;
    [SerializeField] Vector3 offset;
    MapChunk[] activeChunks;
    Vector3 slotsHalfLength;
    int currentChunk;
    private void Start()
    {
        slotsHalfLength = (maxActiveChunkCount - 1) * spacing * offset / 2f;
        activeChunks = new MapChunk[maxActiveChunkCount];
        for (int i = 0; i < activeChunks.Length; i++)
        {
            activeChunks[i] = mapChunks[i];
            activeChunks[i].transform.position = GetSlotPosition(i);
            activeChunks[i].RecordPosition();
        }
        currentChunk = activeChunks.Length;
    }

    public void Remove(MapChunk bubble)
    {
        if (bubble == null) return;
        for (int i = 0; i < activeChunks.Length; i++)
        {
            if (activeChunks[i] == bubble)
            {
                activeChunks[i] = null;
                if (currentChunk < mapChunks.Count)
                {
                    activeChunks[i] = mapChunks[currentChunk];
                    activeChunks[i].transform.position = GetSlotPosition(i);
                    activeChunks[i].RecordPosition();
                    activeChunks[i].transform.DOScale(activeChunks[i].transform.localScale, 0.1f).From(0);
                    currentChunk++;
                }
                break;
            }
        }
    }

    public Vector3 GetSlotPosition(int slotIndex)
    {
        return (transform.position + slotIndex * spacing * offset) - slotsHalfLength;
    }
}
