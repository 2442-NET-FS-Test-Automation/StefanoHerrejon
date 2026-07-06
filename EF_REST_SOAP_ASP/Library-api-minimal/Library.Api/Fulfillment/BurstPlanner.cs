using Library.Data.Entities;
using Library.Data.Enums;
namespace Library.Api.Fulfillment;

public class BurstPlanner
{
    //Method to place fullfillment order
    public IReadOnlyList<int> OrderByPriority(IEnumerable<Order> orders)
    {
        //We could make our own custome implementation on this - we won't
        //We can use a PriorityQueue - allows for FIFO processing with priority taken into account
        //First int = OrderId, Second int = priority
        //We are going to use lower number = higher priority
        PriorityQueue<int,int> pq = new PriorityQueue<int, int>();

        foreach(Order o in orders)
        {
            //Enqueue each order, if it's Priority is expedited, give it a 0 value, if normal give it 1
            pq.Enqueue(o.Id, o.Priority == Priority.Expedited? 0 : 1);
        }

        //This list will hold everything we want to process already in order to pass to our other methods
        var orderedByPriority = new List<int>();

        //While PriorityQueue has stuff in it - loop and add those things in the order they exist
        //to our orderredByPriority list - uses out parameters
        while(pq.TryDequeue(out int id, out _))
        {
            orderedByPriority.Add(id);
        }

        return orderedByPriority; //Expedited ids should be first
    }
}