using System;
using System.Collections.Generic;
using System.Globalization;

namespace ReversibleSignatureAnalyzer.Model.Algorithm
{
// Provides the RLE codec for any integer data type.
// The data's type. Must be an integer type or an ArgumentException will be thrown
static class RLE<T> where T : struct, IConvertible
{
    // This is the marker that identifies a compressed run
    private static T rleMarker;
    // A run can be at most as long as the marker - 1
    private static ulong maxLength;
 
    static RLE()
    {
        GetMaxValues();
    }
 
    // RLE-Encodes a data set.
    public static IEnumerable<T> Encode(IEnumerable<T> data)
    {
        var enumerator = data.GetEnumerator();
 
        if (!enumerator.MoveNext())
            yield break;
 
        var firstRunValue = enumerator.Current;
        ulong runLength = 1;
        while(enumerator.MoveNext())
        {
            var currentValue = enumerator.Current;
            // If the current value is the value of the current run, don't yield anything, 
            // Just extend the run
            if (currentValue.Equals(firstRunValue))
                runLength++;
            else
            {
                // The current value is different from the current run
                // Yield what we have so far
                foreach (var item in MakeRun(firstRunValue, runLength))
                    yield return item;
                     
                // And reset the run
                firstRunValue = currentValue;
                runLength = 1;
            }
            // If there are very many identical values, don't exceed the max length
            if (runLength > maxLength)
            {
                foreach (var item in MakeRun(firstRunValue, maxLength))
                    yield return item;
                runLength -= maxLength;
            }
        }
        // Yield everything that has been buffered
        foreach (var item in MakeRun(firstRunValue, runLength))
            yield return item;
    }
 
    // Decodes RLE-encoded data
    public static IEnumerable<T> Decode(IEnumerable<T> data)
    {
        var enumerator = data.GetEnumerator();
        if (!enumerator.MoveNext())
            yield break;
 
        do
        {
            var value = enumerator.Current;
            if (!value.Equals(rleMarker))
            {
                // An ordinary value
                yield return value;
            }
            else
            {
                // Might be flag or escape
                // Examine the next value
                if (!enumerator.MoveNext())
                    throw new ArgumentException("The provided data is not properly encoded.");
                if (enumerator.Current.Equals(rleMarker))
                {
                    // Escaped value
                    yield return value;
                }
                else
                {
                    // Rle marker
                    var length = enumerator.Current.ToInt64(CultureInfo.InvariantCulture);
                    if (!enumerator.MoveNext())
                        throw new ArgumentException("The provided data is not properly encoded.");
                    var val = enumerator.Current;
                    for (var j = 0; j < length; ++j)
                        yield return val;
                }
            }
        }
        while (enumerator.MoveNext());
    }
 
    private static IEnumerable<T> MakeRun(T value, ulong length)
    {
        if ((length <= 3 && !value.Equals(rleMarker)) || length <= 1)
        {
            // Don't compress this run, it is just too small
            for (ulong i = 0; i < length; ++i)
            {
                if (value.Equals(rleMarker))
                    yield return rleMarker;
                yield return value;
            }
        }
        else
        {
            // Compressed run
            yield return rleMarker;
            yield return (T)(dynamic)length;
            yield return value;
        }
    }
 
    private static void GetMaxValues()
    {
        object maxValue = default(T);
        TypeCode typeCode = Type.GetTypeCode(typeof(T));
        switch (typeCode)
        {
            case TypeCode.Byte:
                {
                    byte limit = Byte.MaxValue;
                    rleMarker = __refvalue( __makeref(limit),T);
                    maxLength = (ulong)(limit - 1);
                    break;
                }
            case TypeCode.Char:
                {
                    var limit = char.MaxValue;
                    rleMarker = __refvalue( __makeref(limit),T);
                    maxLength = (ulong)(limit - 1);
                    break;
                }
            case TypeCode.Int16:
                {
                    var limit = short.MaxValue;
                    rleMarker = __refvalue( __makeref(limit),T);
                    maxLength = (ulong)(limit - 1);
                    break;
                }
            case TypeCode.Int32:
                {
                    var limit = int.MaxValue;
                    rleMarker = __refvalue( __makeref(limit),T);
                    maxLength = (ulong)(limit - 1);
                    break;
                }
            case TypeCode.Int64:
                {
                    var limit = long.MaxValue;
                    rleMarker = __refvalue( __makeref(limit),T);
                    maxLength = (ulong)(limit - 1);
                    break;
                }
            case TypeCode.SByte:
                {
                    var limit = sbyte.MaxValue;
                    rleMarker = __refvalue( __makeref(limit),T);
                    maxLength = (ulong)(limit - 1);
                    break;
                }
            case TypeCode.UInt16:
                {
                    var limit = ushort.MaxValue;
                    rleMarker = __refvalue( __makeref(limit),T);
                    maxLength = (ulong)(limit - 1);
                    break;
                }
            case TypeCode.UInt32:
                {
                    var limit = uint.MaxValue;
                    rleMarker = __refvalue( __makeref(limit),T);
                    maxLength = (ulong)(limit - 1);
                    break;
                }
            case TypeCode.UInt64:
                {
                    var limit = ulong.MaxValue;
                    rleMarker = __refvalue( __makeref(limit),T);
                    maxLength = (ulong)(limit - 1);
                    break;
                }
            default:
                throw new ArgumentException("The provided type parameter is not an integer type");
        }
    }
}
}
