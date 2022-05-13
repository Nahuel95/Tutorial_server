[System.Serializable]
public class Net_AddFollow : NetMsg
{

    public Net_AddFollow()
    {
        OP = NetOP.AddFollow;
    }
    public string Token { set; get; }
    public string UsernameDiscriminatorOrEmail {set; get;}

}