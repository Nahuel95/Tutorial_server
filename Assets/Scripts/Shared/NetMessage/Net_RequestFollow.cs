[System.Serializable]
public class Net_RequestFollow : NetMsg
{

    public Net_RequestFollow()
    {
        OP = NetOP.RequestFollow;
    }
    public string Token { set; get; }
}