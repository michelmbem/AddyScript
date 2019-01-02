using System.Collections.Generic;


namespace AddyScript.Runtime.Utilities
{
    /// <summary>
    /// Represents a node in a <see cref="NameTree"/>.
    /// </summary>
    public class NameTreeNode
    {
        /// <summary>
        /// Initializes an instance of <see cref="NameTreeNode"/>.
        /// </summary>
        /// <param name="name">The name given to the node</param>
        /// <param name="value">The attached value</param>
        public NameTreeNode(string name, object value)
        {
            Name = name;
            Value = value;
            Parent = null;
            Children = new List<NameTreeNode>();
        }

        /// <summary>
        /// The name given to the node.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The value attached to the node.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// A reference to the parent node.
        /// </summary>
        public NameTreeNode Parent { get; set; }

        /// <summary>
        /// The collection of children nodes.
        /// </summary>
        public List<NameTreeNode> Children { get; set; }

        /// <summary>
        /// Gets the fummy qualified name of this node
        /// </summary>
        public string FullName
        {
            get { return Parent == null ? Name : Parent.FullName + "::" + Name; }
        }

        ///<summary>
        ///Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
        ///</summary>
        ///
        ///<returns>
        ///A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
        ///</returns>
        ///<filterpriority>2</filterpriority>
        public override string ToString()
        {
            return FullName;
        }
    }
}
