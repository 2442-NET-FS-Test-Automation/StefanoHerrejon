namespace DsaThreating;

public static class Sorts
{
    //Bubble sort O(n^2) - we just swap adjacent pairs until the largest ones bubble to the end
    public static int[] BubbleSort(int[] input)
    {
        for(int i = 0; i < input.Length; i++)
        {
            for(int x = i+1; x < input.Length; x++)
            {
                if(input[i] > input[x])
                {
                    int tmp = input[i];
                    input[i] = input[x];
                    input[x] = tmp;
                }
            }
        }

        return input;
    }

    //Insertion sort O(n^2): Building the sorted array one element at the time
    //Start with a new empty array, and then as we insert compare and continue
    /*
    Algorithm
    For every element after the first:

        Save the current element.
        Compare it with the previous elements.
        Shift larger elements one position to the right.
        Insert the saved element into its correct position.
    */
    public static int[] InsertionSort(int[] input)
    {
        int length = input.Length;

        //We need a for loop, and we will start from the second element
        for(int i = 1; i < length; i++)
        {
            int key = input[i];
            int j = i - 1;

            //Shift elements of input that are greater than the key one position ahead
            //of what they are now

            while(j >= 0 && input[j] > key)
            {
                input[j+1] = input[j];
                j--;
            }
            //Insert the key into its sorted position
        }

        return input;
    }

    //Find the smalles element from the unsorted part if the array, and swap it
    //with the first unsorted element
    public static int[] Selection(int[] input)
    {
        for(int i = 0; i < input.Length; i++)
        {
            int minIndex = i;
            for(int x = i+1; x < input.Length; x++)
            {
                if(input[minIndex] > input[x])
                    minIndex = x;//Update the index if we find a smaller element
            }
            //Move the minimum element to their correct position via index
            int tmp = input[i];
            input[i] = input[minIndex];
            input[minIndex] = tmp;
        }

        return input;
    }

    //Merge Sort - sort each half recursively then merge them in order
    public static int[] Merge(int[] input)
    {
        //Base case, if its an array of 1
        if(input.Length == 1) return input;

        int mid = input.Length / 2;

        //We split the array into 2 halves
        int[] left = Merge(input[..mid]);
        int[] right = Merge(input[mid..]);

        //We combine them
        return MergeTwo(left, right);
    }

    public static int[] MergeTwo(int[] left, int[] right)
    {
        //Empty array of the total length of right + left

        int[] sorted = new int[left.Length + right.Length];

        int l = 0, r = 0, n = 0;

        //While we dont go over one of the arrays we traverse them and compare them to put the smaller to the sorted arr
        while(l < left.Length && r < right.Length)
        {
            sorted[n++] = left[l] < right[r] ? left[l++] : right[r++];
        }

        for(int i = r; i < right.Length; i++)
            sorted[n++] = right[i];
        

        while(l < left.Length) sorted[n++] = left[l++];

        return sorted;
    }
}