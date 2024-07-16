using System;

[Serializable]
public class RavenMessagePool
{
    public RavenMessage[] list = new RavenMessage[50];
    public RavenMessage[] inUse = new RavenMessage[50];
    public int listCount;
    public int inUseCount;

    public RavenMessage Allocate()
    {
        lock (list)
        {
            if (listCount != 0)
            {
                var obj = list[0];
                RemoveFromList(obj);
                AddToInUse(obj);
                return obj;
            }
            else
            {
                var obj = new RavenMessage();
                AddToInUse(obj);
                return obj;
            }
        }
    }

    public static int AddToArray(RavenMessage add, ref RavenMessage[] array, ref int arrayCount)
    {
        if (arrayCount < 0) arrayCount = 0;
        var arrayToAdd = arrayCount;
        var desiredNewLength = arrayCount + 1;
        if (array.Length < desiredNewLength) Array.Resize(ref array, desiredNewLength + 50);

        arrayCount = desiredNewLength;
        array[arrayToAdd] = add;
        add.poolIndex = (arrayToAdd);

        return arrayToAdd;
    }

    public static void RemoveFromArray(RavenMessage remove, ref RavenMessage[] array, ref int arrayCount)
    {
        if (remove.poolIndex < 0) return;
        var replaceIndex = arrayCount - 1;
        
        var index = remove.poolIndex;
        if (replaceIndex != index && replaceIndex >= 0)
        {
            array[index] = array[replaceIndex];
            array[index].poolIndex = (index);
            array[replaceIndex] = null;
        }
        else array[index] = null;
        
        remove.poolIndex = -1;
        arrayCount -= 1;
    }

    public int AddToList(RavenMessage add) => AddToArray(add, ref list, ref listCount);
    public void RemoveFromList(RavenMessage remove) => RemoveFromArray(remove, ref list, ref listCount);

    public int AddToInUse(RavenMessage add)
    {
        add.isAllocated = true;
        return AddToArray(add, ref inUse, ref inUseCount);
    }

    public void RemoveFromInUse(RavenMessage remove) => RemoveFromArray(remove, ref inUse, ref inUseCount);

    public void Release(RavenMessage obj)
    {
        if (!obj.isAllocated) return;
        obj.isAllocated = false;
        lock (list)
        {
            RemoveFromInUse(obj);
            AddToList(obj);
        }
    }
}