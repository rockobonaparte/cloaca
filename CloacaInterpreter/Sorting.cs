namespace CloacaInterpreter
{
    public class Sorting
    {
        private static void Swap(int[] arr, int a, int b)
        {
            int temp = arr[a];
            arr[a] = arr[b];
            arr[b] = temp;
        }
        public static void Sort(int[] arr)
        {
            // Iterative merge sort
            for (int width = 1; width < arr.Length; width <<= 1)
            {
                for(int l_start = 0; l_start < arr.Length; l_start += 2 * width)
                {
                    int r_start = l_start + width;
                    int l = l_start, r = r_start;
                    for(int i = l_start; i < r_start && r < r_start + width; ++i)
                    {
                        if(arr[i] > arr[r])
                        {
                            Swap(arr, i, r);
                            r += 1;
                        }
                    }
                }
            }
        }
    }
}
