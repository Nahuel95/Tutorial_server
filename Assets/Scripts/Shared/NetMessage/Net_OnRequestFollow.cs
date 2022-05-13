using System.Collections.Generic;

[System.Serializable]
public class Net_OnRequestFollow : NetMsg
{

    public Net_OnRequestFollow()
    {
        OP = NetOP.OnRequestFollow;
    }
    public List<Account> Token { set; get; }
}