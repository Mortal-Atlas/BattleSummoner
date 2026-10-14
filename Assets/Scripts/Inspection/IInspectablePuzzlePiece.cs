using UnityEngine;

public interface IInspectablePuzzlePiece
{
    void OnPuzzleInteracted();
    void OnPuzzleTapped();
    void OnPuzzleSlide(Vector2 screenDelta);
    void OnPuzzleSlideEnd();
}
