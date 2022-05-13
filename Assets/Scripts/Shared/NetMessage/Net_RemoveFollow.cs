[System.Serializable]
public class Net_RemoveFollow : NetMsg
{

    public Net_RemoveFollow()
    {
        OP = NetOP.RemoveFollow;
    }
    public string Token { set; get; }
    public string UsernameDiscriminator { set; get; }
}