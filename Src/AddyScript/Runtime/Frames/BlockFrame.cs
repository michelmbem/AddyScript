﻿using AddyScript.Compilers;
using System.Collections.Generic;


namespace AddyScript.Runtime.Frames
{
    /// <summary>
    /// Represents a block related frame.
    /// </summary>
    public class BlockFrame : Frame
    {
        private readonly Dictionary<string, IFrameItem> items;

        /// <summary>
        /// Initializes an instance of BlockFrame.
        /// </summary>
        /// <param name="items">The items to be registered to this frame</param>
        public BlockFrame(Dictionary<string, IFrameItem> items)
        {
            this.items = items;
        }

        /// <summary>
        /// Initializes an instance of BlockFrame.
        /// </summary>
        public BlockFrame() => items = new Dictionary<string, IFrameItem>();

        #region Overrides

        public override IEnumerable<string> GetNames()
        {
            return items.Keys;
        }

        public override IFrameItem GetItem(string name)
        {
            items.TryGetValue(name, out var item);
            return item;
        }

        public override void PutItem(string name, IFrameItem item)
        {
            items[name] = item;
        }

        public override bool UpdateItem(string name, IFrameItem item)
        {
            if (!items.ContainsKey(name)) return false;
            items[name] = item;
            return true;
        }

        #endregion

        /// <summary>
        /// Copies the items of another frame.
        /// </summary>
        /// <param name="otherFrame">The frame to copy</param>
        public void CopyItems(BlockFrame otherFrame)
        {
            foreach (var pair in otherFrame.items)
                if (pair.Key != Interpreter.MODULE_NAME_CONSTANT)
                    this.items[pair.Key] = pair.Value;
        }
    }
}
