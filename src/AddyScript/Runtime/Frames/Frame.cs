using System.Collections.Generic;


namespace AddyScript.Runtime.Frames;


/// <summary>
/// Represents an isolated storage for function/method or block related objects.
/// </summary>
public abstract class Frame
{
    /// <summary>
    /// Gets the names of all the symbols registered in the frame.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{String}"/> of strings</returns>
    public abstract IEnumerable<string> GetNames();

    /// <summary>
    /// Gets an item by its name.
    /// </summary>
    /// <param name="name">The name of the item to get</param>
    /// <returns>A <see cref="IFrameItem"/></returns>
    public abstract IFrameItem GetItem(string name);

    /// <summary>
    /// Registers an item to the frame.
    /// </summary>
    /// <param name="name">The name of the item to be registered</param>
    /// <param name="item">The item to be registered</param>
    public abstract void PutItem(string name, IFrameItem item);

    /// <summary>
    /// Updates an item previously registered to the frame.
    /// </summary>
    /// <param name="name">The name of the item to be registered</param>
    /// <param name="item">The item to be registered</param>
    /// <returns><b>true</b> if the item has effectively been updated; <b>false</b> otherwise</returns>
    public abstract bool UpdateItem(string name, IFrameItem item);
}
