using Fulfillment.Data;
using Fulfillment.Data.Enums;
namespace Fulfillment.Api.Fulfillment;

public class BurstPlanner
{
    //Method to place fulfillment orders on priority queue
    public IReadOnlyList<int> OrderByPriority(IEnumerable<Order> orders)
    {
        //Priority queue to have more important orders
        PriorityQueue<int,int> pq = new PriorityQueue<int, int>();

        //Fill the priority queue based on Priority
        foreach(Order o in orders)
        {
            pq.Enqueue(o.Id, o.Priority == Priority.Expidited? 0:1); //0 fast, 1 slow
        }

        var orderByPriority = new List<int>(); //List to have orders arranged based on priority

        while(pq.TryDequeue(out int id, out _))//While there is something on the queue, deque and add to list
        {
            orderByPriority.Add(id);
        }

        return orderByPriority;//Return list of orders ordered based on priority
    }
}