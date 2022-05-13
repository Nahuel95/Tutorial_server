[System.Serializable]
public class Net_OnAddFollow : NetMsg
{

    public Net_OnAddFollow()
    {
        OP = NetOP.OnAddFollow;
    }
    public Account Follow { set; get; }

}