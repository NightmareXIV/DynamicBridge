namespace DynamicBridge.Core;

public struct TimelineSegment
{
    public float Start;  // Starting time (0-1)
    public float End;    // Ending time (0-1)
    public int State;  // Segment state

    public TimelineSegment(float start, float end, int state)
    {
        Start = start;
        End = end;
        State = state;
    }
}