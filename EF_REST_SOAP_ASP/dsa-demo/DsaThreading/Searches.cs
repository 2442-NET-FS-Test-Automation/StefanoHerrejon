namespace DsaThreating;

public static class Searches
{
    //Linear search O(n) - walk through the array until we find what we want
    //Sorted or Unsorted doesnt really matter, unserted ok
    public static int LinearSearch(int[] data, int target)
    {
        //We could probably use a foreach but that itselt is an abstraction
        for(int i = 0; i < data.Length; i++)
        {
            if(data[i] == target)
                return i;
        }

        //If we dont find it return -1
        return -1; 
    }

    //Binary search - halve the search space each step
    //O(lon(n)) - but we must be sorted before we start
    public static int BinarySearch(int[] sorted, int target)
    {
        int ini = 0, fin = sorted.Length-1;
        while(ini < fin)
        {
            int half = ini + (fin-ini)/2;
            if(sorted[half] == target)
            {
                return half;
            }else if(sorted[half] > target)
            {
                fin = half-1;
            }
            else
            {
                ini = half+1;
            }
        }
        return -1;
    }
}