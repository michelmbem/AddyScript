using System.Collections.Generic;
using System.Linq;

namespace AddyScript.Runtime.Frames;


/// <summary>
/// Represents a method/function related <see cref="Frame"/>.
/// </summary>
public class MethodFrame : Frame
{
    private readonly Stack<BlockFrame> blockFrames = [];
    private BlockFrame currentBlock;

    /// <summary>
    /// Initializes an instance of <see cref="MethodFrame"/>.
    /// </summary>
    /// <param name="context">The context under which the frame is created</param>
    /// <param name="initialItems">A set of initial frame's items</param>
    public MethodFrame(InvocationContext context, Dictionary<string, IFrameItem> initialItems)
    {
        Context = context;
        PushBlock(initialItems);
        RootBlock = blockFrames.Peek();
    }

    /// <summary>
    /// Initializes an instance of <see cref="MethodFrame"/>.
    /// </summary>
    /// <param name="context">The context under which the frame is created</param>
    public MethodFrame(InvocationContext context)
    {
        Context = context;
        PushBlock();
        RootBlock = blockFrames.Peek();
    }

    /// <summary>
    /// The context under which the frame is created.
    /// </summary>
    public InvocationContext Context { get; }

    /// <summary>
    /// Gets the root <see cref="BlockFrame"/> of this <see cref="MethodFrame"/>
    /// </summary>
    public BlockFrame RootBlock { get; }

    /// <summary>
    /// Pushes a new <see cref="BlockFrame"/> on top of the internal block related frames stack.
    /// </summary>
    /// <param name="items">The set of items to be registered to the newly created block</param>
    public void PushBlock(Dictionary<string, IFrameItem> items)
    {
        blockFrames.Push(currentBlock = new BlockFrame(items));
    }

    /// <summary>
    /// Pushes a new <see cref="BlockFrame"/> on top of the internal block related frames stack.
    /// </summary>
    public void PushBlock()
    {
        blockFrames.Push(currentBlock = new BlockFrame());
    }

    /// <summary>
    /// Pops a <see cref="BlockFrame"/> from the internal block related frames stack.
    /// </summary>
    public void PopBlock()
    {
        blockFrames.Pop();
        currentBlock = blockFrames.Peek();
    }

    #region Overrides

    public override IEnumerable<string> GetNames()
    {
        List<string> names = [];

        foreach (BlockFrame blockFrame in blockFrames)
            names.AddRange(blockFrame.GetNames());

        return names;
    }

    public override IFrameItem GetItem(string name) =>
        blockFrames.Select(frame => frame.GetItem(name))
                   .FirstOrDefault(item => item != null);

    public override void PutItem(string name, IFrameItem item)
    {
        if (blockFrames.Any(frame => frame.UpdateItem(name, item)))
            return;

        currentBlock.PutItem(name, item);
    }

    public override bool UpdateItem(string name, IFrameItem item) =>
        blockFrames.Any(frame => frame.UpdateItem(name, item));

    #endregion

    /// <summary>
    /// Synchronizes a <see cref="MethodFrame"/>'s items with a set of given items.
    /// </summary>
    /// <param name="itemsToSync">The set of frame's items to synchronize with</param>
    /// <param name="namesToSkip">A set of names to be ignored during synchronization</param>
    /// <remarks>
    /// This method is to be invoked after a call to a closure to ensure that the items captured
    /// by the closure will reflect the changes occured during the closure's lifetime
    /// </remarks>
    public void SyncItems(Dictionary<string, IFrameItem> itemsToSync, HashSet<string> namesToSkip)
    {
        foreach (string name in GetNames())
        {
            if (namesToSkip.Contains(name)) continue;

            IFrameItem item = itemsToSync[name];

            if (item.Kind == FrameItemKind.Variable)
                UpdateItem(name, item);
        }
    }
}
