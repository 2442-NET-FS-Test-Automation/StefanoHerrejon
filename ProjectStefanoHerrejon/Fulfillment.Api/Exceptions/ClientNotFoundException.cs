using Fulfillment.Data.Entities;
using Fulfillment.Data.Enums;

namespace Fulfillment.Api.Exceptions;

public class ClientNotFoundException : Exception
{
    //OrderId, LineId, Message
    public int ClientId{get;}

    public ClientNotFoundException(int clientId) : 
    base($" Client with id: {clientId} not found")
    {
        ClientId = clientId;
    }
}