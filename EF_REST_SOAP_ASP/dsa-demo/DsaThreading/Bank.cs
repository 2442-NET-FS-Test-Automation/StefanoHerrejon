namespace DsaThreating;

public class Bank
{
    public long Balance;//mutable state
    
    //First lets create a lock object
    private readonly object _gate = new();
    public void DepositUnSafe(long amount) => Balance += amount; //read-modify-write; NOT ATOMIC

    public void DepositSafe(long amount)
    {
        lock(_gate) //only onw thread can enter this code block at a time
        {
            Balance += amount;
        }
    }
}