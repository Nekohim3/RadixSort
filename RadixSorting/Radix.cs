using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RadixSorting
{
    public static class Radix
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="selector"></param>
        /// <param name="toLongConverter">null или Convert.ToInt64 для целых чисел, BitConverter.DoubleToInt64Bits для double</param>
        /// <param name="comparer"></param>
        /// <typeparam name="TElement"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        private static void Sort<TElement, TKey>(IList<TElement> arr, Func<TElement, TKey> selector, Func<TKey, long> toLongConverter = null, Comparer<TKey> comparer = null)
        {
            if (toLongConverter == null)
                toLongConverter = x => Convert.ToInt64(x);

            var typeSize = Marshal.SizeOf(typeof(TKey));
            comparer = comparer ?? Comparer<TKey>.Default;
            var rankLen = 4; // длинна разряда в битах

            if (arr.Count > 128)
                rankLen = 8;
            if (arr.Count > 65535 && typeSize > 1)
                rankLen = 16;
            
            var rankCount      = typeSize * 8 / rankLen; // количество разрядов
            var maxNum      = 1 << rankLen;           // Максимальное число в разряде (маска)
            var count      = arr.Count;
            var buffer = new TElement[count]; // буфер для отсортированного массива по разряду

            for (var currentRank = 0; currentRank < rankCount; currentRank++) // цикл по разрядам
            {
                var indexesCounter = new int[maxNum];

                for (var i = 0; i < count; i++) // подсчет количества чисел по индексам
                    indexesCounter[toLongConverter(selector(arr[i])) >> (rankLen * currentRank) & (maxNum - 1)]++;
                
                for (var i = 1; i < maxNum; i++) // Подсчитывается количества элементов меньших или равных count - 1
                    indexesCounter[i] += indexesCounter[i - 1];

                for (var i = count - 1; i >= 0; i--)
                    buffer[--indexesCounter[toLongConverter(selector(arr[i])) >> (rankLen * currentRank) & (maxNum - 1)]] = arr[i];

                if (currentRank < rankCount - 1)
                    for (var i = 0; i < count; i++)
                        arr[i] = buffer[i];
                else //фикс отрицательных чисел
                {
                    var s = -1;

                    for (var i = 0; i < count && s == -1; i++) // поиск отрицательных чисел
                        if (comparer.Compare(selector(buffer[i]), default) < 0)
                            s = i;
                    
                    if (s != -1)
                    {
                        var x = 0;
                        if (comparer.Compare(selector(buffer[s]), selector(buffer[buffer.Length - 1])) > 0) // если первый отрицательный элемент больше последнего, это double
                            for (var i = count - 1; i >= s; i--, x++)
                                arr[x] = buffer[i];
                        else
                            for (var i = s; i < count; i++, x++)
                                arr[x] = buffer[i];

                        for (var i = 0; i < s; i++, x++)
                            arr[x] = buffer[i];
                    }
                    else
                        for (var i = 0; i < count; i++)
                            arr[i] = buffer[i];
                }
            }
        }
    }
}