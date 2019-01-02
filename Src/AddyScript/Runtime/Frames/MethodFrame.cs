using System;
using System.Collections.Generic;

using AddyScript.Runtime.NativeTypes;


namespace AddyScript.Runtime.Frames
{
    /// <summary>
    /// Represents a method/function related frame.
    /// </summary>
    public class MethodFrame : Frame
    {
        private readonly CallContext context;
        private readonly Stack<BlockFrame> blockFrames;
        private BlockFrame rootBlockFrame, currentBlockFrame;

        /// <summary>
        /// Initializes an instance of MethodFrame.
        /// </summary>
        /// <param name="context">The context under which the frame is created</param>
        /// <param name="initialItems">A set of initial frame's items</param>
        public MethodFrame(CallContext context, Dictionary<string, IFrameItem> initialItems)
        {
            this.context = context;
            blockFrames = new Stack<BlockFrame>();
            PushBlock(initialItems);
            rootBlockFrame = blockFrames.Peek();
        }

        /// <summary>
        /// Initializes an instance of MethodFrame.
        /// </summary>
        /// <param name="context">The context under which the frame is created</param>
        public MethodFrame(CallContext context)
        {
            this.context = context;
            blockFrames = new Stack<BlockFrame>();
            PushBlock();
            rootBlockFrame = blockFrames.Peek();
        }

        /// <summary>
        /// The context under which the frame is created.
        /// </summary>
        public CallContext Context
        {
            get { return context; }
        }

        /// <summary>
        /// Pushes a new <see cref="BlockFrame"/> on top of the internal block related frames stack.
        /// </summary>
        /// <param name="items">The set of items to be registered to the newly created block</param>
        public void PushBlock(Dictionary<string, IFrameItem> items)
        {
            blockFrames.Push(currentBlockFrame = new BlockFrame(items));
        }

        /// <summary>
        /// Pushes a new <see cref="BlockFrame"/> on top of the internal block related frames stack.
        /// </summary>
        public void PushBlock()
        {
            blockFrames.Push(currentBlockFrame = new BlockFrame());
        }

        /// <summary>
        /// Pops a <see cref="BlockFrame"/> from the internal block related frames stack.
        /// </summary>
        public void PopBlock()
        {
            blockFrames.Pop();
            currentBlockFrame = blockFrames.Peek();
        }

        #region Overrides

        public override IEnumerable<string> GetNames()
        {
            var names = new List<string>();

            foreach (BlockFrame blockFrame in blockFrames)
                names.AddRange(blockFrame.GetNames());

            return names;
        }

        public override IFrameItem GetItem(string name)
        {
            foreach (BlockFrame blockFrame in blockFrames)
            {
                IFrameItem item = blockFrame.GetItem(name);
                if (item != null) return item;
            }

            return null;
        }

        public override void PutItem(string name, IFrameItem item)
        {
            foreach (BlockFrame blockFrame in blockFrames)
                if (blockFrame.UpdateItem(name, item))
                    return;

            currentBlockFrame.PutItem(name, item);
        }

        public override bool UpdateItem(string name, IFrameItem item)
        {
            foreach (BlockFrame blockFrame in blockFrames)
                if (blockFrame.UpdateItem(name, item))
                    return true;

            return false;
        }

        #endregion

        /// <summary>
        /// Gets an item from the root block frame by its name.
        /// </summary>
        /// <param name="name">The name of the item to get</param>
        /// <returns>A <see cref="IFrameItem"/></returns>
        public IFrameItem GetRootItem(string name)
        {
            return rootBlockFrame.GetItem(name);
        }

        /// <summary>
        /// Registers an item into the root block frame.
        /// </summary>
        /// <param name="name">The name of the item to be registered</param>
        /// <param name="item">The item to be registered</param>
        public void PutRootItem(string name, IFrameItem item)
        {
            rootBlockFrame.PutItem(name, item);
        }

        /// <summary>
        /// Updates an item previously registered into the root block frame.
        /// </summary>
        /// <param name="name">The name of the item to be registered</param>
        /// <param name="item">The item to be registered</param>
        /// <returns><b>true</b> if the item has effectively been updated; <b>false</b> otherwise</returns>
        public bool UpdateRootItem(string name, IFrameItem item)
        {
            return rootBlockFrame.UpdateItem(name, item);
        }

        /// <summary>
        /// Copies the root items of another frame.
        /// </summary>
        /// <param name="otherFrame">The frame to copy</param>
        public void CopyRootItems(MethodFrame otherFrame)
        {
            rootBlockFrame.CopyItems(otherFrame.rootBlockFrame);
        }

        /// <summary>
        /// Synchronizes a <see cref="MethodFrame"/>'s items with a set of given items.
        /// </summary>
        /// <param name="syncItems">The set of frame's items to synchronize with</param>
        /// <param name="namesToSkip">A set of names to be ignored during synchronization</param>
        /// <remarks>
        /// This method is to be invoked after a call to a closure to ensure that items imported by the
        /// closure from its declaring context will reflect changes occured during the closure's lifetime
        /// </remarks>
        public void SyncItems(Dictionary<string, IFrameItem> syncItems, HashSet<string> namesToSkip)
        {
            foreach (string name in GetNames())
                if (!namesToSkip.Contains(name) &&
                    syncItems[name].Kind == FrameItemKind.Variable)
                    UpdateItem(name, syncItems[name]);
        }
    }
}
